#!/usr/bin/env python3
from __future__ import annotations
"""
scan_assets.py - Validate Unity assets without running Unity.

Reads the binary-ish YAML files that Unity serializes (.prefab, .mat, .unity,
.asset, .meta) and checks for common integrity problems that would otherwise
only surface at import time or — worse — at runtime.

Checks performed:
  ASSET-001  Addressable asset groups present and non-empty
  ASSET-002  Prefab integrity (no missing/broken script references)
  ASSET-003  Material shader references (no null shaders)
  ASSET-004  Missing .meta files (GUID consistency)
  ASSET-005  Scene count and main-scene validation

Usage:
    python scan_assets.py --repo-dir "C:/repos/apps.studioa3"
    python scan_assets.py --repo-dir "C:/repos/apps.studioa3" --config configs/studio-a3.json
    python scan_assets.py --repo-dir "C:/repos/apps.studioa3" --output-dir ./my-reports

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import glob
import json
import logging
import os
import re
import sys

from qa_common import check, skip, build_scanner_argparser, load_config, resolve_output_dir, save_results, print_summary, setup_logging

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# ASSET-001: Addressable asset groups
# ---------------------------------------------------------------------------

def check_addressable_groups(repo_dir):
    """
    Count .asset files under Assets/AddressableAssetsData/AssetGroups/.
    Each file represents one Addressable group.  Verify every file is non-empty.
    """
    groups_dir = os.path.join(
        repo_dir, "Assets", "AddressableAssetsData", "AssetGroups"
    )

    if not os.path.isdir(groups_dir):
        # The project may not use Addressables at all.
        return skip(
            "ASSET-001",
            "Addressable asset groups",
            "Asset Integrity",
            "Medium",
            "No AddressableAssetsData/AssetGroups/ directory found — "
            "project may not use Addressables",
        )

    asset_files = glob.glob(os.path.join(groups_dir, "*.asset"))
    if not asset_files:
        return check(
            "ASSET-001",
            "Addressable asset groups (0 .asset files)",
            "Asset Integrity",
            "Medium",
            False,
            "AssetGroups/ directory exists but contains no .asset files",
        )

    empty_files = [
        os.path.relpath(f, repo_dir)
        for f in asset_files
        if os.path.getsize(f) == 0
    ]

    if empty_files:
        return check(
            "ASSET-001",
            f"Addressable asset groups ({len(asset_files)} files, "
            f"{len(empty_files)} empty)",
            "Asset Integrity",
            "Medium",
            False,
            f"Empty .asset files (likely corrupt): {empty_files}",
        )

    return check(
        "ASSET-001",
        f"Addressable asset groups ({len(asset_files)} groups, all non-empty)",
        "Asset Integrity",
        "Medium",
        True,
    )


# ---------------------------------------------------------------------------
# ASSET-002: Prefab integrity — missing script references
# ---------------------------------------------------------------------------

# Unity serializes a missing MonoBehaviour script reference as:
#   m_Script: {fileID: 0}
# A *valid* reference always has a non-zero fileID plus a guid.
_BROKEN_SCRIPT_RE = re.compile(r"m_Script:\s*\{fileID:\s*0\}")


def check_prefab_integrity(repo_dir):
    """
    Scan every .prefab under Assets/ for broken script references.
    """
    prefabs = glob.glob(
        os.path.join(repo_dir, "Assets", "**", "*.prefab"), recursive=True
    )

    if not prefabs:
        return skip(
            "ASSET-002",
            "Prefab integrity",
            "Asset Integrity",
            "High",
            "No .prefab files found under Assets/",
        )

    broken_refs_total = 0
    broken_files = []  # (relative_path, count)

    for prefab_path in prefabs:
        try:
            with open(prefab_path, "r", encoding="utf-8", errors="ignore") as fh:
                content = fh.read()
        except OSError:
            continue

        hits = len(_BROKEN_SCRIPT_RE.findall(content))
        if hits > 0:
            broken_refs_total += hits
            broken_files.append(
                (os.path.relpath(prefab_path, repo_dir), hits)
            )

    if broken_refs_total == 0:
        return check(
            "ASSET-002",
            f"Prefab integrity ({len(prefabs)} prefabs, 0 broken script refs)",
            "Asset Integrity",
            "High",
            True,
        )

    # Build a readable note — show the worst offenders first.
    broken_files.sort(key=lambda x: x[1], reverse=True)
    top = broken_files[:10]
    detail_lines = [f"  {path} ({n} broken)" for path, n in top]
    if len(broken_files) > 10:
        detail_lines.append(f"  ... and {len(broken_files) - 10} more files")
    detail = "\n".join(detail_lines)

    return check(
        "ASSET-002",
        f"Prefab integrity ({broken_refs_total} broken script refs in "
        f"{len(broken_files)} prefabs)",
        "Asset Integrity",
        "High",
        False,
        f"{broken_refs_total} missing script references found "
        f"(m_Script: {{fileID: 0}}). These prefabs will show "
        f"'Missing (Mono Script)' warnings in Unity.\n{detail}",
    )


# ---------------------------------------------------------------------------
# ASSET-003: Material shader references — null shader
# ---------------------------------------------------------------------------

_NULL_SHADER_RE = re.compile(r"m_Shader:\s*\{fileID:\s*0\}")


def check_material_shaders(repo_dir):
    """
    Scan every .mat file for null shader references.
    """
    mat_files = glob.glob(
        os.path.join(repo_dir, "Assets", "**", "*.mat"), recursive=True
    )

    if not mat_files:
        return skip(
            "ASSET-003",
            "Material shader references",
            "Asset Integrity",
            "Medium",
            "No .mat files found under Assets/",
        )

    null_shader_total = 0
    bad_mats = []

    for mat_path in mat_files:
        try:
            with open(mat_path, "r", encoding="utf-8", errors="ignore") as fh:
                content = fh.read()
        except OSError:
            continue

        hits = len(_NULL_SHADER_RE.findall(content))
        if hits > 0:
            null_shader_total += hits
            bad_mats.append(os.path.relpath(mat_path, repo_dir))

    if null_shader_total == 0:
        return check(
            "ASSET-003",
            f"Material shader references ({len(mat_files)} materials, "
            f"0 null shaders)",
            "Asset Integrity",
            "Medium",
            True,
        )

    top = bad_mats[:10]
    detail = "\n".join(f"  {m}" for m in top)
    if len(bad_mats) > 10:
        detail += f"\n  ... and {len(bad_mats) - 10} more"

    return check(
        "ASSET-003",
        f"Material shader references ({null_shader_total} null shaders in "
        f"{len(bad_mats)} materials)",
        "Asset Integrity",
        "Medium",
        False,
        f"{null_shader_total} materials reference a null shader "
        f"(m_Shader: {{fileID: 0}}). They will render pink in Unity.\n{detail}",
    )


# ---------------------------------------------------------------------------
# ASSET-004: Missing .meta files
# ---------------------------------------------------------------------------

# Files and directories that Unity never expects a .meta for.
_META_IGNORE = {".git", ".svn", ".vs", ".idea", "Library", "Temp", "obj",
                "Logs", "UserSettings", "MemoryCaptures", ".DS_Store"}


def check_missing_meta_files(repo_dir):
    """
    Every file and folder inside Assets/ should have a sibling .meta file.
    Missing .meta files cause GUID regeneration which breaks cross-references.
    """
    assets_dir = os.path.join(repo_dir, "Assets")
    if not os.path.isdir(assets_dir):
        return check(
            "ASSET-004",
            "Missing .meta files",
            "Asset Integrity",
            "High",
            False,
            "Assets/ directory does not exist",
        )

    missing = []

    for dirpath, dirnames, filenames in os.walk(assets_dir):
        # Prune directories we should never check.
        dirnames[:] = [
            d for d in dirnames
            if d not in _META_IGNORE and not d.startswith(".")
        ]

        # Check each sub-directory has a .meta sibling in its parent.
        for d in dirnames:
            meta_path = os.path.join(dirpath, d + ".meta")
            if not os.path.isfile(meta_path):
                missing.append(
                    os.path.relpath(os.path.join(dirpath, d), repo_dir) + "/"
                )

        # Check each file (skip .meta files themselves).
        for fname in filenames:
            if fname.endswith(".meta"):
                continue
            if fname in _META_IGNORE or fname.startswith("."):
                continue
            meta_path = os.path.join(dirpath, fname + ".meta")
            if not os.path.isfile(meta_path):
                missing.append(
                    os.path.relpath(os.path.join(dirpath, fname), repo_dir)
                )

    if not missing:
        return check(
            "ASSET-004",
            "All assets have .meta files",
            "Asset Integrity",
            "High",
            True,
        )

    top = missing[:15]
    detail = "\n".join(f"  {m}" for m in top)
    if len(missing) > 15:
        detail += f"\n  ... and {len(missing) - 15} more"

    return check(
        "ASSET-004",
        f"Missing .meta files ({len(missing)} assets without .meta)",
        "Asset Integrity",
        "High",
        False,
        f"{len(missing)} files/folders under Assets/ have no .meta file. "
        f"This will cause Unity to regenerate GUIDs, breaking references "
        f"for other team members.\n{detail}",
    )


# ---------------------------------------------------------------------------
# ASSET-005: Scene count and main scene validation
# ---------------------------------------------------------------------------

def check_scenes(repo_dir, config):
    """
    Count .unity scene files and verify the configured main scene exists.
    """
    scene_files = glob.glob(
        os.path.join(repo_dir, "Assets", "**", "*.unity"), recursive=True
    )
    scene_count = len(scene_files)

    # Normalise scene paths for comparison (forward slashes, relative).
    scene_rel = set()
    for s in scene_files:
        rel = os.path.relpath(s, repo_dir).replace("\\", "/")
        scene_rel.add(rel)

    # Check if a main scene is specified in config.
    main_scene = config.get("main_scene", "")

    if scene_count == 0:
        return check(
            "ASSET-005",
            "Scene files (0 found)",
            "Asset Integrity",
            "Critical",
            False,
            "No .unity scene files found under Assets/. "
            "The application has nothing to load.",
        )

    if not main_scene:
        # No main scene configured — just report scene count.
        return check(
            "ASSET-005",
            f"Scene files ({scene_count} scenes, no main scene configured)",
            "Asset Integrity",
            "Critical",
            True,
        )

    # Normalize the configured path for comparison.
    main_scene_norm = main_scene.replace("\\", "/")
    found = main_scene_norm in scene_rel

    if found:
        return check(
            "ASSET-005",
            f"Scene files ({scene_count} scenes, main scene verified)",
            "Asset Integrity",
            "Critical",
            True,
        )

    return check(
        "ASSET-005",
        f"Scene files ({scene_count} scenes, main scene MISSING)",
        "Asset Integrity",
        "Critical",
        False,
        f"Main scene '{main_scene}' specified in config was not found. "
        f"Available scenes:\n"
        + "\n".join(f"  {s}" for s in sorted(scene_rel)[:20]),
    )


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = build_scanner_argparser(
        "Validate Unity assets without running Unity.",
        require_config=False,
    )
    args = parser.parse_args()

    repo_dir = os.path.abspath(args.repo_dir)
    if not os.path.isdir(os.path.join(repo_dir, "Assets")):
        logger.error("No Assets/ folder in %s", repo_dir)
        sys.exit(2)

    config = load_config(args.config)
    app_name = config.get("app_name", os.path.basename(repo_dir))
    output_dir = resolve_output_dir(args.output_dir)

    setup_logging(verbose=getattr(args, "verbose", False),
                  quiet=getattr(args, "quiet", False))

    logger.info("=== Unity Asset Validator ===")
    logger.info("  App:  %s", app_name)
    logger.info("  Repo: %s", repo_dir)
    logger.info("")

    results = []
    logger.info("  [1/5] Checking addressable asset groups...")
    results.append(check_addressable_groups(repo_dir))
    logger.info("  [2/5] Checking prefab integrity...")
    results.append(check_prefab_integrity(repo_dir))
    logger.info("  [3/5] Checking material shader references...")
    results.append(check_material_shaders(repo_dir))
    logger.info("  [4/5] Checking for missing .meta files...")
    results.append(check_missing_meta_files(repo_dir))
    logger.info("  [5/5] Checking scenes...")
    results.append(check_scenes(repo_dir, config))

    results_path, _ = save_results(
        results, app_name, config, output_dir,
        scanner_name="asset_scan",
        tester="Automated Asset Scan",
        extra_env={"scan_type": "Asset validation (no Unity runtime)",
                    "repo_path": repo_dir},
    )
    fail_count = print_summary(results, app_name, results_path, args.config)
    sys.exit(1 if fail_count > 0 else 0)


if __name__ == "__main__":
    main()

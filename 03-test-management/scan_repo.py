#!/usr/bin/env python3
from __future__ import annotations
"""
scan_repo.py - Scan a zSpace Unity repo and generate real test results.

This script examines the actual source code, project settings, build scripts,
and configuration of a Unity repo to produce a test results JSON file that
can be fed into coverage_report.py.

It checks everything that CAN be verified without building or running the app:
  - Project structure and required files
  - Unity packages and dependencies
  - Build pipeline completeness
  - Content and asset integrity
  - Code quality indicators

Usage:
    python scan_repo.py --repo-dir "C:/repos/apps.studioa3" --config configs/studio-a3.json
    python scan_repo.py --repo-dir "C:/repos/apps.franklins-lab-a3" --config configs/franklins-lab-a3.json

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import glob
import json
import logging
import os
import re
import sys
from datetime import datetime

from qa_common import check, skip, build_scanner_argparser, load_config, app_slug, resolve_output_dir, save_results, print_summary, setup_logging

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Thresholds
# ---------------------------------------------------------------------------
MAX_CODE_MARKERS_PASS = 20  # CODE-001: max TODO/HACK/FIXME markers before failing


def scan_project_structure(repo_dir, config):
    """Check that the Unity project has the expected folder structure."""
    results = []

    # STRUCT-001: Assets folder exists
    assets_dir = os.path.join(repo_dir, "Assets")
    results.append(check(
        "STRUCT-001", "Assets folder exists", "Project Structure", "Critical",
        os.path.isdir(assets_dir),
        f"Missing: {assets_dir}"
    ))

    # STRUCT-002: ProjectSettings folder exists
    ps_dir = os.path.join(repo_dir, "ProjectSettings")
    results.append(check(
        "STRUCT-002", "ProjectSettings folder exists", "Project Structure", "Critical",
        os.path.isdir(ps_dir),
        f"Missing: {ps_dir}"
    ))

    # STRUCT-003: Packages/manifest.json exists
    manifest = os.path.join(repo_dir, "Packages", "manifest.json")
    results.append(check(
        "STRUCT-003", "Packages/manifest.json exists", "Project Structure", "Critical",
        os.path.isfile(manifest),
        f"Missing: {manifest}"
    ))

    # STRUCT-004: Main app scripts folder exists
    app_name = config.get("exe_name", "")
    # Try common patterns for the app scripts folder
    script_dirs = [
        os.path.join(repo_dir, "Assets", app_name),
        os.path.join(repo_dir, "Assets", app_name, "Scripts"),
        os.path.join(repo_dir, "Assets", "Scripts"),
    ]
    found_scripts = any(os.path.isdir(d) for d in script_dirs)
    results.append(check(
        "STRUCT-004", "Application scripts folder exists", "Project Structure", "High",
        found_scripts,
        f"No script directory found. Checked: {script_dirs}"
    ))

    # STRUCT-005: At least 1 C# script file exists
    cs_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*.cs"), recursive=True)
    results.append(check(
        "STRUCT-005", f"C# scripts present ({len(cs_files)} found)", "Project Structure", "High",
        len(cs_files) > 0,
        "No .cs files found in Assets/"
    ))

    # STRUCT-006: Scene files exist
    scene_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*.unity"), recursive=True)
    results.append(check(
        "STRUCT-006", f"Unity scene files present ({len(scene_files)} found)", "Project Structure", "High",
        len(scene_files) > 0,
        "No .unity scene files found in Assets/"
    ))

    return results


def scan_packages(repo_dir, config):
    """Check Unity package dependencies."""
    results = []
    manifest_path = os.path.join(repo_dir, "Packages", "manifest.json")

    if not os.path.isfile(manifest_path):
        results.append(check("PKG-001", "Package manifest readable", "Dependencies", "Critical", False, "manifest.json not found"))
        return results

    with open(manifest_path, "r", encoding="utf-8") as f:
        manifest = json.load(f)

    deps = manifest.get("dependencies", {})

    # PKG-001: Unity Test Framework installed
    has_test_fw = "com.unity.test-framework" in deps
    test_ver = deps.get("com.unity.test-framework", "not installed")
    results.append(check(
        "PKG-001", f"Unity Test Framework installed (v{test_ver})", "Dependencies", "High",
        has_test_fw,
        "com.unity.test-framework not in manifest.json. Add it to enable unit testing."
    ))

    # PKG-002: Addressables package (if used)
    has_addr = "com.unity.addressables" in deps
    addr_ver = deps.get("com.unity.addressables", "not installed")
    if has_addr:
        results.append(check(
            "PKG-002", f"Addressables package installed (v{addr_ver})", "Dependencies", "Medium",
            True, ""
        ))
    else:
        results.append(skip(
            "PKG-002", "Addressables package installed", "Dependencies", "Medium",
            "App does not use Addressables"
        ))

    # PKG-003: TextMeshPro
    has_tmp = "com.unity.textmeshpro" in deps
    results.append(check(
        "PKG-003", "TextMeshPro package installed", "Dependencies", "Medium",
        has_tmp,
        "TextMeshPro not found — UI text may not render correctly"
    ))

    # PKG-004: No deprecated packages
    deprecated = ["com.unity.ads", "com.unity.analytics"]
    found_deprecated = [d for d in deprecated if d in deps]
    results.append(check(
        "PKG-004", "No deprecated packages in manifest", "Dependencies", "Low",
        len(found_deprecated) == 0,
        f"Deprecated packages found: {found_deprecated}"
    ))

    return results


def scan_build_pipeline(repo_dir, config):
    """Check that the build pipeline is complete and functional."""
    results = []

    # BUILD-001: Build scripts exist
    build_patterns = ["build/*.bat", "build/**/*.bat", "*.bat", ".build/**/*.bat"]
    bat_files = []
    for pattern in build_patterns:
        bat_files.extend(glob.glob(os.path.join(repo_dir, pattern), recursive=True))
    results.append(check(
        "BUILD-001", f"Build scripts present ({len(bat_files)} .bat files)", "Build Pipeline", "Critical",
        len(bat_files) > 0,
        "No build .bat files found"
    ))

    # BUILD-002: Win64 build script exists
    win64_scripts = [f for f in bat_files if "win64" in os.path.basename(f).lower()]
    results.append(check(
        "BUILD-002", "Windows 64-bit build script exists", "Build Pipeline", "Critical",
        len(win64_scripts) > 0,
        "No buildWin64.bat found"
    ))

    # BUILD-003: WebGL build script exists
    webgl_scripts = [f for f in bat_files if "webgl" in os.path.basename(f).lower()]
    results.append(check(
        "BUILD-003", "WebGL build script exists", "Build Pipeline", "High",
        len(webgl_scripts) > 0,
        "No buildWebGL.bat found"
    ))

    # BUILD-004: Jenkins pipeline exists
    jenkins_patterns = ["**/Jenkinsfile", "**/Jenkinsfile*", "build/pipeline/Jenkinsfile"]
    jenkins_files = []
    for pattern in jenkins_patterns:
        jenkins_files.extend(glob.glob(os.path.join(repo_dir, pattern), recursive=True))
    results.append(check(
        "BUILD-004", "Jenkins pipeline configuration exists", "Build Pipeline", "High",
        len(jenkins_files) > 0,
        "No Jenkinsfile found"
    ))

    # BUILD-005: BuildScript.cs exists (Unity build automation)
    build_cs = glob.glob(os.path.join(repo_dir, "Assets", "**", "BuildScript*.cs"), recursive=True)
    results.append(check(
        "BUILD-005", "Unity BuildScript.cs exists", "Build Pipeline", "High",
        len(build_cs) > 0,
        "No BuildScript.cs found — builds may require manual Unity menu clicks"
    ))

    return results


def scan_content(repo_dir, config):
    """Check content/activity files."""
    results = []
    expected_count = config.get("expected_content_count", 0)

    # CONTENT-001: Activity files match expected count
    fla3_files = glob.glob(os.path.join(repo_dir, "**", "*.fla3"), recursive=True)
    if expected_count == 0 and len(fla3_files) == 0:
        results.append(check(
            "CONTENT-001", "Activity pack files (none expected for this app)", "Content", "Medium",
            True, ""
        ))
    elif expected_count > 0:
        results.append(check(
            "CONTENT-001", f"Activity pack files ({len(fla3_files)} of {expected_count} expected)", "Content", "High",
            len(fla3_files) >= expected_count,
            f"Found {len(fla3_files)}, expected at least {expected_count}"
        ))
    else:
        results.append(check(
            "CONTENT-001", f"Activity pack files ({len(fla3_files)} found, 0 expected)", "Content", "Medium",
            True, ""
        ))

    # CONTENT-002: Addressable asset groups configured
    addr_groups = glob.glob(os.path.join(repo_dir, "Assets", "AddressableAssetsData", "**", "*.asset"), recursive=True)
    if addr_groups:
        results.append(check(
            "CONTENT-002", f"Addressable asset groups configured ({len(addr_groups)} groups)", "Content", "Medium",
            True, ""
        ))
    else:
        results.append(skip(
            "CONTENT-002", "Addressable asset groups configured", "Content", "Medium",
            "No AddressableAssetsData found"
        ))

    # CONTENT-003: Scenes in the project
    scene_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*.unity"), recursive=True)
    results.append(check(
        "CONTENT-003", f"Scene files present ({len(scene_files)} scenes)", "Content", "High",
        len(scene_files) > 0,
        "No .unity scene files found"
    ))

    return results


def scan_code_quality(repo_dir, config):
    """Basic static analysis checks on the codebase."""
    results = []
    skipped_files = 0

    cs_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*.cs"), recursive=True)

    # CODE-001: No TODO/HACK/FIXME in critical paths
    critical_markers = 0
    files_with_markers = []
    for cs_file in cs_files:
        try:
            with open(cs_file, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
            markers = len(re.findall(r'//\s*(TODO|HACK|FIXME|BUG|XXX)', content, re.IGNORECASE))
            if markers > 0:
                critical_markers += markers
                files_with_markers.append(os.path.relpath(cs_file, repo_dir))
        except (IOError, OSError, UnicodeDecodeError):
            skipped_files += 1

    results.append(check(
        "CODE-001", f"Code markers (TODO/HACK/FIXME): {critical_markers} found in {len(files_with_markers)} files",
        "Code Quality", "Low",
        critical_markers < MAX_CODE_MARKERS_PASS,
        f"{critical_markers} markers across {len(files_with_markers)} files. Top files: {files_with_markers[:5]}"
    ))

    # CODE-002: No empty catch blocks
    empty_catches = 0
    for cs_file in cs_files:
        try:
            with open(cs_file, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
            empty_catches += len(re.findall(r'catch\s*\([^)]*\)\s*\{\s*\}', content))
        except (IOError, OSError, UnicodeDecodeError):
            skipped_files += 1

    results.append(check(
        "CODE-002", f"Empty catch blocks: {empty_catches} found",
        "Code Quality", "Medium",
        empty_catches == 0,
        f"{empty_catches} empty catch blocks — exceptions may be silently swallowed"
    ))

    # CODE-003: Editor scripts exist (build automation)
    editor_scripts = glob.glob(os.path.join(repo_dir, "Assets", "**/Editor", "*.cs"), recursive=True)
    results.append(check(
        "CODE-003", f"Editor scripts present ({len(editor_scripts)} files)", "Code Quality", "Medium",
        len(editor_scripts) > 0,
        "No Editor/ scripts found — custom build steps may be missing"
    ))

    # CODE-004: zSpace SDK integration present
    zspace_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*zSpace*"), recursive=True)
    zspace_files += glob.glob(os.path.join(repo_dir, "Assets", "**", "*zspace*"), recursive=True)
    results.append(check(
        "CODE-004", f"zSpace SDK integration ({len(zspace_files)} related files)", "Code Quality", "Critical",
        len(zspace_files) > 0,
        "No zSpace SDK files found — this may not be a zSpace app"
    ))

    if skipped_files > 0:
        logger.warning("  Warning: %d .cs files could not be read (encoding/permission issues)", skipped_files)

    return results


def scan_licensing_readiness(repo_dir, config):
    """Check that licensing infrastructure is present."""
    results = []

    # LIC-001: Licensing module or references exist
    cs_files = glob.glob(os.path.join(repo_dir, "Assets", "**", "*.cs"), recursive=True)
    licensing_refs = 0
    for cs_file in cs_files:
        try:
            with open(cs_file, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
            if re.search(r'[Ll]icens', content):
                licensing_refs += 1
        except (IOError, OSError, UnicodeDecodeError):
            skipped_files += 1

    results.append(check(
        "LIC-001", f"Licensing code references ({licensing_refs} files)", "Licensing Readiness", "High",
        licensing_refs > 0,
        "No licensing references found in source code"
    ))

    # LIC-002: Installation scripts present
    install_scripts = glob.glob(os.path.join(repo_dir, "build", "InstallationScripts", "*"), recursive=False)
    if not install_scripts:
        install_scripts = glob.glob(os.path.join(repo_dir, ".build", "InstallationScripts", "*"), recursive=False)
    results.append(check(
        "LIC-002", f"Installation scripts present ({len(install_scripts)} files)", "Licensing Readiness", "Medium",
        len(install_scripts) > 0,
        "No InstallationScripts folder found"
    ))

    return results


def scan_hardware_tests(repo_dir, config):
    """Mark hardware-dependent tests as skipped (can't verify without device)."""
    results = []

    results.append(skip("HW-001", "Stereo rendering on zSpace display", "Hardware (requires device)", "Critical",
                        "Requires physical zSpace hardware — cannot verify from source"))
    results.append(skip("HW-002", "Head tracking responsiveness", "Hardware (requires device)", "Critical",
                        "Requires physical zSpace hardware"))
    results.append(skip("HW-003", "Stylus input accuracy", "Hardware (requires device)", "Critical",
                        "Requires physical zSpace hardware"))
    results.append(skip("HW-004", "zView presenter mode", "Hardware (requires device)", "High",
                        "Requires zSpace hardware + secondary display"))
    results.append(skip("HW-005", "Multi-display configuration", "Hardware (requires device)", "Medium",
                        "Requires multiple displays"))

    return results


def main():
    parser = build_scanner_argparser(
        "Scan a zSpace Unity repo and generate real test results."
    )
    args = parser.parse_args()

    repo_dir = os.path.abspath(args.repo_dir)
    if not os.path.isdir(repo_dir):
        logger.error("Repo directory not found: %s", repo_dir)
        sys.exit(1)

    config = load_config(args.config)
    app_name = config.get("app_name", "Unknown App")
    output_dir = resolve_output_dir(args.output_dir)

    setup_logging(verbose=getattr(args, "verbose", False),
                  quiet=getattr(args, "quiet", False))

    logger.info("=== zSpace Repo Scanner ===")
    logger.info("  App:  %s", app_name)
    logger.info("  Repo: %s", repo_dir)
    logger.info("")

    # Run all scans.
    all_results = []

    logger.info("Scanning project structure...")
    all_results.extend(scan_project_structure(repo_dir, config))

    logger.info("Scanning package dependencies...")
    all_results.extend(scan_packages(repo_dir, config))

    logger.info("Scanning build pipeline...")
    all_results.extend(scan_build_pipeline(repo_dir, config))

    logger.info("Scanning content and assets...")
    all_results.extend(scan_content(repo_dir, config))

    logger.info("Scanning code quality...")
    all_results.extend(scan_code_quality(repo_dir, config))

    logger.info("Scanning licensing readiness...")
    all_results.extend(scan_licensing_readiness(repo_dir, config))

    logger.info("Marking hardware-dependent tests...")
    all_results.extend(scan_hardware_tests(repo_dir, config))

    # Write results and print summary.
    results_path, _ = save_results(
        all_results, app_name, config, output_dir,
        scanner_name="scan_results",
        tester="Automated Repo Scan",
        extra_env={"repo_path": repo_dir,
                    "test_framework": config.get("test_framework_version", "unknown")},
    )
    fail_count = print_summary(all_results, app_name, results_path, args.config)
    sys.exit(1 if fail_count > 0 else 0)


if __name__ == "__main__":
    main()

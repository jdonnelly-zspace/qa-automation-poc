#!/usr/bin/env python3
from __future__ import annotations
"""
run_qa.py - One-command QA Automation for zSpace Unity apps.

Clones repos (if URLs), runs all scanners, merges results, and generates
an HTML dashboard.

Usage:
    python run_qa.py https://github.com/zspace/apps.studioa3
    python run_qa.py https://github.com/zspace/apps.studioa3 https://github.com/zspace/apps.franklinslab-a3
    python run_qa.py ./apps.studioa3
    python run_qa.py ./apps.studioa3 --config configs/studio-a3.json

Part of QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import glob
import json
import logging
import os
import re
import subprocess
import sys
from datetime import datetime
from pathlib import Path

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

PROJECT_ROOT = Path(__file__).resolve().parent
SCRIPTS_DIR = PROJECT_ROOT / "03-test-management"
CONFIGS_DIR = PROJECT_ROOT / "configs"
OUTPUT_DIR = PROJECT_ROOT / "output" / "reports"
EXTRA_TESTS_DIR = PROJECT_ROOT / "02-unity-unit-tests" / "EditModeTests"
CLONE_DIR = PROJECT_ROOT / "repos"



# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def run_script(script_name, args_list, label):
    """Run a Python script and return (success, output_path_if_any).

    Exit code semantics:
        0 = all checks passed
        1 = test failures found (normal — pipeline continues)
        2+ = script error (pipeline logs warning and continues)
    """
    cmd = [sys.executable, str(SCRIPTS_DIR / script_name)] + args_list
    logger.info("\n%s", "=" * 64)
    logger.info("  RUNNING: %s", label)
    logger.info("%s\n", "=" * 64)

    try:
        result = subprocess.run(
            cmd,
            cwd=str(PROJECT_ROOT),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=300,  # 5 minute timeout per scanner
        )
    except subprocess.TimeoutExpired:
        logger.error("  ERROR: %s timed out after 5 minutes", label)
        return False, None

    # Print output (safe for Windows cp1252 console)
    if result.stdout:
        logger.info(result.stdout.encode("ascii", errors="replace").decode("ascii"))
    if result.stderr:
        logger.error(result.stderr.encode("ascii", errors="replace").decode("ascii"))

    # Check exit code
    if result.returncode not in (0, 1):
        logger.warning("  WARNING: %s exited with code %d (possible error)", label, result.returncode)

    # Extract the results file path from output (scripts print it)
    output_path = None
    markers = ["Results saved to:", "Results JSON:", "Saved merged:", "Saved:"]
    for line in (result.stdout or "").split("\n"):
        for marker in markers:
            if marker in line:
                path_part = line.split(marker, 1)[-1].strip()
                if os.path.isfile(path_part):
                    output_path = path_part
                    break

    success = result.returncode in (0, 1)
    return success, output_path


def resolve_repo(repo_arg):
    """
    Resolve a repo argument to a local path.
    - If it's a URL, clone it (or use existing clone).
    - If it's a local path, use it directly.
    Returns (repo_dir, repo_name).
    """
    # GitHub URL?
    if repo_arg.startswith("http://") or repo_arg.startswith("https://") or repo_arg.startswith("git@"):
        # Extract repo name from URL
        repo_name = repo_arg.rstrip("/").split("/")[-1]
        if repo_name.endswith(".git"):
            repo_name = repo_name[:-4]

        clone_target = CLONE_DIR / repo_name
        if clone_target.exists() and (clone_target / "Assets").exists():
            logger.info("  Using existing clone: %s", clone_target)
            # Pull latest
            subprocess.run(
                ["git", "pull", "--ff-only"],
                cwd=str(clone_target),
                capture_output=True,
                timeout=120,
            )
        else:
            logger.info("  Cloning %s -> %s", repo_arg, clone_target)
            CLONE_DIR.mkdir(parents=True, exist_ok=True)
            try:
                result = subprocess.run(
                    ["git", "clone", "--depth", "1", repo_arg, str(clone_target)],
                    capture_output=True,
                    text=True,
                    timeout=300,
                )
            except subprocess.TimeoutExpired:
                logger.error("  ERROR: Clone timed out after 5 minutes")
                return None, repo_name
            if result.returncode != 0:
                logger.error("  ERROR: Clone failed: %s", result.stderr)
                return None, repo_name

            # Init submodules
            subprocess.run(
                ["git", "submodule", "update", "--init", "--recursive"],
                cwd=str(clone_target),
                capture_output=True,
                timeout=300,
            )

        return str(clone_target), repo_name

    # Local path
    repo_path = Path(repo_arg).resolve()
    if not repo_path.exists():
        # Try relative to project root parent (where repos typically live)
        repo_path = PROJECT_ROOT.parent / repo_arg
    if not repo_path.exists():
        logger.error("  ERROR: Path not found: %s", repo_arg)
        return None, Path(repo_arg).name

    repo_name = repo_path.name
    return str(repo_path), repo_name


def find_config(repo_name, explicit_config=None):
    """Find the config file for a repo. Returns path or None.

    Auto-detection scans all JSON files in configs/ and matches
    the app_name field against the repo folder name. No hardcoded
    mapping — just drop a new config file in configs/ to add an app.
    """
    if explicit_config:
        cfg = Path(explicit_config)
        if not cfg.is_absolute():
            cfg = PROJECT_ROOT / cfg
        if cfg.exists():
            return str(cfg)
        logger.warning("  Warning: Config not found: %s", explicit_config)

    # Auto-detect by scanning configs/ directory
    repo_key = repo_name.lower().replace("-", "").replace("_", "").replace(".", "")
    for cfg_file in sorted(CONFIGS_DIR.glob("*.json")):
        try:
            with open(cfg_file, "r", encoding="utf-8") as f:
                cfg_data = json.load(f)
            app_name = cfg_data.get("app_name", "")
            app_key = app_name.lower().replace("-", "").replace("_", "").replace(".", "").replace("'", "").replace(" ", "")
            # Match if repo name contains the app key or vice versa
            if app_key in repo_key or repo_key in app_key:
                logger.info("  Auto-detected config: %s", cfg_file)
                return str(cfg_file)
        except (json.JSONDecodeError, IOError):
            continue

    logger.warning("  Warning: No config found for '%s'. Scanners will use defaults.", repo_name)
    return None


def merge_json_results(json_files, app_name="Unknown"):
    """Merge multiple JSON result files into one combined file."""
    all_results = []
    base_data = None

    for path in json_files:
        if not os.path.isfile(path):
            continue
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
        if base_data is None:
            base_data = data
        all_results.extend(data.get("results", []))

    if base_data is None:
        return None

    base_data["results"] = all_results
    base_data["tester"] = "Automated Full Scan (repo + localization + assets + coverage)"

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    app_slug = re.sub(r"[^a-z0-9]+", "-", app_name.lower()).strip("-")
    merged_path = OUTPUT_DIR / f"full_scan_{app_slug}_{timestamp}.json"

    with open(merged_path, "w", encoding="utf-8") as f:
        json.dump(base_data, f, indent=2)

    return str(merged_path)


# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

def run_pipeline(repo_dir, repo_name, config_path):
    """Run the full QA automation pipeline for one repo."""
    # Load app name from config
    app_name = repo_name
    if config_path:
        with open(config_path, "r", encoding="utf-8") as f:
            cfg = json.load(f)
        app_name = cfg.get("app_name", repo_name)

    app_slug = re.sub(r"[^a-z0-9]+", "-", app_name.lower()).strip("-")

    logger.info("\n%s", "#" * 64)
    logger.info("  QA AUTOMATION: %s", app_name)
    logger.info("  Repo: %s", repo_dir)
    logger.info("  Config: %s", config_path or "(none)")
    logger.info("#" * 64)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    result_files = []

    # -- Common args -----------------------------------------------------------
    base_args = ["--repo-dir", repo_dir]
    if config_path:
        base_args += ["--config", config_path]

    # -- 1. Source code scan ---------------------------------------------------
    ok, path = run_script("scan_repo.py", base_args, "Source Code Scan")
    if ok and path:
        result_files.append(path)

    # -- 2. Localization scan --------------------------------------------------
    ok, path = run_script("scan_localization.py", base_args, "Localization Scan")
    if ok and path:
        result_files.append(path)

    # -- 3. Asset scan ---------------------------------------------------------
    ok, path = run_script("scan_assets.py", base_args, "Asset Integrity Scan")
    if ok and path:
        result_files.append(path)

    # -- 4. Test coverage scan (with extra-tests) ------------------------------
    coverage_args = list(base_args)
    if EXTRA_TESTS_DIR.exists():
        coverage_args += ["--extra-tests", str(EXTRA_TESTS_DIR)]
    ok, path = run_script("scan_test_coverage.py", coverage_args, "Test Coverage Scan")
    if ok and path:
        result_files.append(path)

    # -- 5. Merge all results --------------------------------------------------
    if not result_files:
        logger.error("\n  ERROR: No scan results were produced.")
        return None

    logger.info("\n%s", "=" * 64)
    logger.info("  MERGING %d scan results", len(result_files))
    logger.info("=" * 64)

    merged_path = merge_json_results(result_files, app_name)
    if not merged_path:
        logger.error("  ERROR: Failed to merge results.")
        return None
    logger.info("  Merged: %s", merged_path)

    # -- 6. Check for Unity test results XML -----------------------------------
    unity_xml = OUTPUT_DIR / f"unity_test_results_{app_slug}.xml"
    if unity_xml.exists():
        logger.info("\n  Found Unity test results: %s", unity_xml)
        unity_args = [
            "--xml-file", str(unity_xml),
            "--app-name", app_name,
            "--merge-with", merged_path,
        ]
        if config_path:
            with open(config_path, "r", encoding="utf-8") as f:
                cfg = json.load(f)
            unity_args += ["--release-version", cfg.get("expected_version", "1.0.0.0")]

        ok, path = run_script("parse_unity_results.py", unity_args, "Unity Test Results")
        if ok and path:
            merged_path = path  # Use the combined file
    else:
        logger.info("\n  No Unity test results found at %s (skipping)", unity_xml)

    # -- 7. Generate HTML report -----------------------------------------------
    report_args = [
        "--results-file", merged_path,
        "--output", "html",
    ]
    if config_path:
        report_args += ["--config", config_path]

    run_script("coverage_report.py", report_args, "HTML Report Generation")

    # Find the generated report
    reports = sorted(
        glob.glob(str(OUTPUT_DIR / f"coverage_report_{app_slug}_*.html")),
        key=os.path.getmtime,
        reverse=True,
    )
    report_path = reports[0] if reports else None

    # -- 8. Hint: Jira issue creation from failures ----------------------------
    if config_path:
        logger.info("")
        logger.info("  To create Jira issues from failures:")
        logger.info('    python 03-test-management/create_jira_issues.py --results-file "%s" --config %s --dry-run', merged_path, config_path)

    return report_path


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Run full QA Automation for one or more zSpace Unity repos.",
        epilog=(
            "Examples:\n"
            "  python run_qa.py https://github.com/zspace/apps.studioa3\n"
            "  python run_qa.py https://github.com/zspace/apps.studioa3 "
            "https://github.com/zspace/apps.franklinslab-a3\n"
            "  python run_qa.py ./apps.studioa3 --config configs/studio-a3.json\n"
            "  python run_qa.py ../apps.studioa3\n"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "repos",
        nargs="+",
        help="GitHub URLs or local paths to Unity project repos",
    )
    parser.add_argument(
        "--config",
        default=None,
        help="Explicit config file (auto-detected if omitted)",
    )
    parser.add_argument(
        "--verbose", action="store_true",
        help="Enable verbose (DEBUG-level) logging",
    )
    parser.add_argument(
        "--quiet", action="store_true",
        help="Suppress informational output (WARNING-level only)",
    )

    args = parser.parse_args()

    # Matches setup_logging() in qa_common.py
    level = logging.DEBUG if args.verbose else logging.WARNING if args.quiet else logging.INFO
    logging.basicConfig(level=level, format="%(message)s",
                        handlers=[logging.StreamHandler(sys.stdout)])

    logger.info("#" * 64)
    logger.info("  zSpace QA Automation POC")
    logger.info("  Date: %s", datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    logger.info("  Repos: %d", len(args.repos))
    logger.info("#" * 64)

    reports = []

    for repo_arg in args.repos:
        logger.info("\n  Resolving: %s", repo_arg)
        repo_dir, repo_name = resolve_repo(repo_arg)
        if not repo_dir:
            logger.warning("  SKIPPED: Could not resolve %s", repo_arg)
            continue

        # Verify it's a Unity project
        if not os.path.isdir(os.path.join(repo_dir, "Assets")):
            logger.warning("  SKIPPED: No Assets/ folder in %s — not a Unity project", repo_dir)
            continue

        config_path = find_config(repo_name, args.config if len(args.repos) == 1 else None)
        report = run_pipeline(repo_dir, repo_name, config_path)
        if report:
            reports.append((repo_name, report))

    # -- Summary ---------------------------------------------------------------
    logger.info("\n%s", "#" * 64)
    logger.info("  QA AUTOMATION COMPLETE")
    logger.info("#" * 64)

    if reports:
        logger.info("\n  Reports generated:")
        for name, path in reports:
            logger.info("    %s: %s", name, path)
    else:
        logger.info("\n  No reports were generated.")

    logger.info("\n  All output: %s", OUTPUT_DIR)
    logger.info("")

    return 0 if reports else 1


if __name__ == "__main__":
    sys.exit(main())

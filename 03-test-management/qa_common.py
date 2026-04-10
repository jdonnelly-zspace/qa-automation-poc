#!/usr/bin/env python3
"""
qa_common.py - Shared utilities for QA scanner scripts.

Provides common patterns used by all scanner scripts:
  - Test result helpers (check, skip, warn)
  - CLI argument parsing with standard flags
  - Config loading and app name resolution
  - Output directory resolution
  - Result JSON writing and console summary

Part of QA Automation POC for zSpace Unity AR/VR applications.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path


# ---------------------------------------------------------------------------
# Test result helpers
# ---------------------------------------------------------------------------

def check(test_id, title, category, priority, passed, notes=""):
    """Create a test result entry (pass or fail)."""
    return {
        "id": test_id,
        "title": title,
        "category": category,
        "priority": priority,
        "status": "pass" if passed else "fail",
        "notes": notes if not passed else "",
        "remediation": "" if passed else notes,
    }


def skip(test_id, title, category, priority, reason):
    """Create a skipped test result entry."""
    return {
        "id": test_id,
        "title": title,
        "category": category,
        "priority": priority,
        "status": "skip",
        "notes": reason,
    }


def warn(test_id, title, category, priority, notes):
    """Create a warning result entry (passes but with notes)."""
    return {
        "id": test_id,
        "title": title,
        "category": category,
        "priority": priority,
        "status": "pass",
        "notes": notes,
    }


# ---------------------------------------------------------------------------
# CLI argument parsing
# ---------------------------------------------------------------------------

def build_scanner_argparser(description, require_config=True):
    """Build an argparse.ArgumentParser pre-loaded with standard scanner flags.

    Standard flags:
      --repo-dir   Path to the Unity project repository root
      --config     Path to the app config JSON file
      --output-dir Output directory for result files
    """
    parser = argparse.ArgumentParser(
        description=description,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--repo-dir",
        required=True,
        help="Path to the Unity project repository root (must contain Assets/)",
    )
    parser.add_argument(
        "--config",
        required=require_config,
        default=None,
        help="Path to an app config JSON (provides app_name, version, etc.)",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory for output JSON (default: output/reports/ in project root)",
    )
    return parser


# ---------------------------------------------------------------------------
# Config and path utilities
# ---------------------------------------------------------------------------

def load_config(config_path):
    """Load and return a config dict from a JSON file. Returns {} if None."""
    if not config_path:
        return {}
    if not os.path.isfile(config_path):
        print(f"ERROR: Config file not found: {config_path}")
        sys.exit(2)
    with open(config_path, "r", encoding="utf-8") as f:
        return json.load(f)


def app_slug(app_name):
    """Derive a filesystem-safe slug from an app name."""
    return re.sub(r"[^a-z0-9]+", "-", app_name.lower()).strip("-")


def resolve_output_dir(args_output_dir):
    """Resolve the output directory, defaulting to output/reports/ in project root."""
    if args_output_dir:
        out = os.path.abspath(args_output_dir)
    else:
        project_root = Path(__file__).resolve().parent.parent
        out = str(project_root / "output" / "reports")
    os.makedirs(out, exist_ok=True)
    return out


def validate_repo_dir(repo_dir_arg):
    """Resolve and validate a repo directory. Exits on error."""
    repo_dir = os.path.abspath(repo_dir_arg)
    if not os.path.isdir(repo_dir):
        print(f"ERROR: Repo directory not found: {repo_dir}")
        sys.exit(2)
    assets_dir = os.path.join(repo_dir, "Assets")
    if not os.path.isdir(assets_dir):
        print(f"ERROR: No Assets/ folder in {repo_dir} -- is this a Unity project?")
        sys.exit(2)
    return repo_dir


# ---------------------------------------------------------------------------
# Result output
# ---------------------------------------------------------------------------

def save_results(results, app_name, config, output_dir, scanner_name,
                 tester="Automated Scan", extra_env=None):
    """Write results to a timestamped JSON file and return the file path.

    Args:
        results:      List of test result dicts
        app_name:     Application name (e.g., "Studio A3")
        config:       Config dict (for version, unity_version, etc.)
        output_dir:   Output directory path
        scanner_name: Short name for filename (e.g., "scan_results", "l10n_results")
        tester:       Tester name for the JSON envelope
        extra_env:    Additional environment key/value pairs
    """
    slug = app_slug(app_name)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    env = {
        "scan_type": "Source code analysis (no build, no hardware)",
        "unity_version": config.get("unity_version", "unknown"),
    }
    if extra_env:
        env.update(extra_env)

    data = {
        "release_version": config.get("expected_version", "unknown"),
        "app_name": app_name,
        "test_date": datetime.now().strftime("%Y-%m-%d"),
        "tester": tester,
        "environment": env,
        "results": results,
    }

    results_path = os.path.join(output_dir, f"{scanner_name}_{slug}_{timestamp}.json")
    with open(results_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    return results_path, data


def print_summary(results, app_name, results_path, config_path=None):
    """Print a formatted console summary of scan results."""
    pass_count = sum(1 for r in results if r["status"] == "pass")
    fail_count = sum(1 for r in results if r["status"] == "fail")
    skip_count = sum(1 for r in results if r["status"] == "skip")
    total = len(results)

    print()
    print(f"{'=' * 60}")
    print(f"  SCAN RESULTS: {app_name}")
    print(f"{'=' * 60}")
    for r in results:
        icon = {"pass": "[+] PASS", "fail": "[X] FAIL", "skip": "[-] SKIP"}[r["status"]]
        line = f"  {icon}: {r['id']} - {r['title']}"
        if r["status"] != "pass" and r.get("notes"):
            first_line = r["notes"].split("\n")[0]
            line += f"\n         {first_line}"
        print(line)
    print(f"{'-' * 60}")
    print(f"  Total: {total} | Pass: {pass_count} | Fail: {fail_count} | Skip: {skip_count}")
    print(f"{'=' * 60}")
    print()
    print(f"Results saved to: {results_path}")

    if config_path:
        print()
        print("To generate an HTML report:")
        print(f'  python 03-test-management/coverage_report.py '
              f'--results-file "{results_path}" --config {config_path} --output html')

    return fail_count

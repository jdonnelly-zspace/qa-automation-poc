#!/usr/bin/env python3
"""
scan_test_coverage.py - Identify C# scripts with no test coverage.

Walks a Unity project's Assets/ tree to find production scripts and test
scripts, then matches them by naming convention to estimate test coverage.

Checks performed:
  COV-001  Overall test coverage percentage
  COV-002  High-priority scripts covered (Controller, Manager, Service, Importer)
  COV-003  Modules with zero test coverage

Usage:
    python scan_test_coverage.py --repo-dir "C:/repos/apps.studioa3"
    python scan_test_coverage.py --repo-dir ../apps.studioa3 --config configs/studio-a3.json
    python scan_test_coverage.py --repo-dir ../apps.studioa3 --output-dir ./my-reports

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import glob
import json
import os
import re
import sys
from collections import defaultdict
from datetime import datetime


# ---------------------------------------------------------------------------
# Helpers
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


# Directories that contain test code (case-insensitive match).
_TEST_DIR_NAMES = {"tests", "test", "editor"}

# Directories to exclude from production script discovery.
_EXCLUDE_DIR_NAMES = {"editor", "tests", "test", "testdata", "plugins"}

# Filename suffixes/prefixes that identify test scripts.
_TEST_SUFFIXES = ("Tests.cs", "Test.cs")
_TEST_PREFIXES = ("Test",)

# Keywords that mark a script as high-priority for testing.
_HIGH_PRIORITY_KEYWORDS = {"Controller", "Manager", "Importer", "Service"}


def _is_excluded_dir(rel_path):
    """Return True if any path component is an excluded directory."""
    parts = re.split(r"[\\/]", rel_path)
    for part in parts:
        if part.lower() in _EXCLUDE_DIR_NAMES:
            return True
    return False


def _is_test_dir(rel_path):
    """Return True if any path component is a test directory."""
    parts = re.split(r"[\\/]", rel_path)
    for part in parts:
        if part.lower() in _TEST_DIR_NAMES:
            return True
    return False


def _is_test_file(filename):
    """Return True if the filename matches test-file naming conventions."""
    for suffix in _TEST_SUFFIXES:
        if filename.endswith(suffix):
            return True
    name_no_ext = filename[:-3]  # strip ".cs"
    for prefix in _TEST_PREFIXES:
        if name_no_ext.startswith(prefix) and len(name_no_ext) > len(prefix):
            return True
    return False


def _module_name(rel_path, repo_dir_len):
    """
    Derive a human-readable module name from a script's relative path.
    Uses the first meaningful directory under Assets/ as the module.
    Falls back to 'Assets (root)' for scripts directly in Assets/.
    """
    parts = rel_path.replace("\\", "/").split("/")
    # parts[0] is "Assets"
    if len(parts) >= 3:
        # e.g. Assets/StudioA3/Scripts/Foo.cs -> "StudioA3/Scripts"
        # We take up to 2 levels under Assets for grouping.
        return "/".join(parts[1:min(len(parts) - 1, 3)])
    elif len(parts) == 2:
        return "Assets (root)"
    return "/".join(parts[1:-1]) if len(parts) > 1 else "Assets (root)"


def _is_high_priority(filename):
    """Return True if the script name contains a high-priority keyword."""
    name_no_ext = filename[:-3]
    for keyword in _HIGH_PRIORITY_KEYWORDS:
        if keyword in name_no_ext:
            return True
    return False


# ---------------------------------------------------------------------------
# Discovery
# ---------------------------------------------------------------------------

def discover_scripts(repo_dir):
    """
    Walk Assets/ and classify every .cs file as either production or test.

    Returns:
        production: list of (relative_path, filename)
        tests:      list of (relative_path, filename)
    """
    assets_dir = os.path.join(repo_dir, "Assets")
    all_cs = glob.glob(os.path.join(assets_dir, "**", "*.cs"), recursive=True)

    production = []
    tests = []

    for cs_path in all_cs:
        rel = os.path.relpath(cs_path, repo_dir)
        fname = os.path.basename(cs_path)

        if _is_test_dir(rel) or _is_test_file(fname):
            tests.append((rel, fname))
        elif not _is_excluded_dir(rel):
            production.append((rel, fname))
        # else: editor-only or test-data script — skip

    return production, tests


# ---------------------------------------------------------------------------
# Matching
# ---------------------------------------------------------------------------

def match_coverage(production, tests):
    """
    For each production script Foo.cs, check whether any of the following
    exist among test scripts:
        FooTests.cs, FooTest.cs, TestFoo.cs

    Returns:
        covered:   list of (prod_rel_path, matching_test_filename)
        uncovered: list of prod_rel_path
    """
    # Build a set of test filenames (lowered) for fast lookup.
    test_names_lower = {fname.lower() for _, fname in tests}

    covered = []
    uncovered = []

    for rel, fname in production:
        base = fname[:-3]  # strip .cs
        candidates = [
            f"{base}Tests.cs".lower(),
            f"{base}Test.cs".lower(),
            f"Test{base}.cs".lower(),
        ]

        match = None
        for c in candidates:
            if c in test_names_lower:
                match = c
                break

        if match:
            covered.append((rel, match))
        else:
            uncovered.append(rel)

    return covered, uncovered


# ---------------------------------------------------------------------------
# Grouping by module
# ---------------------------------------------------------------------------

def group_by_module(production, covered_set):
    """
    Group production scripts by module (parent directory) and compute
    per-module coverage.

    Returns:
        dict: module_name -> {total, covered, uncovered_scripts}
    """
    modules = defaultdict(lambda: {"total": 0, "covered": 0, "uncovered_scripts": []})

    for rel, fname in production:
        mod = _module_name(rel, 0)
        modules[mod]["total"] += 1
        if rel in covered_set:
            modules[mod]["covered"] += 1
        else:
            modules[mod]["uncovered_scripts"].append(fname)

    return dict(modules)


# ---------------------------------------------------------------------------
# Test result builders
# ---------------------------------------------------------------------------

def build_results(production, tests, covered, uncovered, modules):
    """
    Build COV-001 through COV-003 result entries compatible with
    coverage_report.py.
    """
    results = []
    total_prod = len(production)
    total_covered = len(covered)

    # -- COV-001: Overall coverage percentage ----------------------------------
    if total_prod == 0:
        pct = 0.0
    else:
        pct = (total_covered / total_prod) * 100.0

    # A passing threshold is configurable, but 50% is a reasonable baseline.
    cov_pass = pct >= 50.0
    cov_remediation = (
        "This is a program of work, not a quick fix. Start with the 5 "
        "highest-impact scripts: 1) LicenseManagerUnity - users can't launch if "
        "licensing breaks. 2) ActivityPackManager - blank screens if content "
        "loading breaks. 3) UndoRedoManager - data loss if undo breaks. "
        "4) SceneManager - core scene loading. 5) ActivityImporter - activity "
        "pipeline. POC test files for these are in 02-unity-unit-tests/EditModeTests/. "
        "Target 75% coverage on Controllers/Managers within 2-3 sprints, then "
        "work down remaining scripts by module priority."
    )
    results.append({
        "id": "COV-001",
        "title": f"Test coverage: {pct:.1f}% ({total_covered}/{total_prod} scripts)",
        "category": "Test Coverage",
        "priority": "High",
        "status": "pass" if cov_pass else "fail",
        "notes": (
            f"Only {pct:.1f}% of production scripts have a corresponding test "
            f"file. {len(uncovered)} scripts are untested."
        ),
        "remediation": cov_remediation,
    })

    # -- COV-002: High-priority scripts covered --------------------------------
    hp_scripts = [(rel, fname) for rel, fname in production if _is_high_priority(fname)]
    covered_set = {rel for rel, _ in covered}
    hp_covered = [rel for rel, _ in hp_scripts if rel in covered_set]
    hp_uncovered = [
        (rel, fname) for rel, fname in hp_scripts if rel not in covered_set
    ]

    if not hp_scripts:
        results.append(check(
            "COV-002",
            "High-priority scripts covered (none found)",
            "Test Coverage",
            "Critical",
            True,
        ))
    else:
        hp_pct = (len(hp_covered) / len(hp_scripts)) * 100.0
        hp_pass = hp_pct >= 75.0
        uncov_list = "\n".join(
            f"  {fname}  ({rel})" for rel, fname in hp_uncovered[:15]
        )
        if len(hp_uncovered) > 15:
            uncov_list += f"\n  ... and {len(hp_uncovered) - 15} more"

        hp_remediation = (
            "Prioritized starting list (by user impact): "
            "1) LicenseManagerUnity - users can't launch if licensing breaks. "
            "2) ActivityPackManager - blank screens if content loading breaks. "
            "3) UndoRedoManager - data loss if undo breaks. "
            "4) SceneManager - core scene loading. "
            "5) ActivityImporter - activity pipeline. "
            "POC test files for all 5 are ready in 02-unity-unit-tests/EditModeTests/. "
            "Copy them into the Unity project's Assets/Tests/ folder and adapt "
            "assembly references per the setup guide."
        )
        results.append({
            "id": "COV-002",
            "title": (
                f"High-priority scripts: {hp_pct:.1f}% covered "
                f"({len(hp_covered)}/{len(hp_scripts)})"
            ),
            "category": "Test Coverage",
            "priority": "Critical",
            "status": "pass" if hp_pass else "fail",
            "notes": (
                f"{len(hp_uncovered)} high-priority scripts "
                f"(Controller/Manager/Service/Importer) lack test files:\n"
                f"{uncov_list}"
            ),
            "remediation": hp_remediation,
        })

    # -- COV-003: Modules with zero coverage -----------------------------------
    zero_modules = [
        name for name, info in modules.items()
        if info["total"] > 0 and info["covered"] == 0
    ]
    zero_modules.sort()

    if not zero_modules:
        results.append(check(
            "COV-003",
            "All modules have at least some test coverage",
            "Test Coverage",
            "High",
            True,
        ))
    else:
        detail = "\n".join(
            f"  {m} ({modules[m]['total']} scripts, 0 tests)"
            for m in zero_modules[:15]
        )
        if len(zero_modules) > 15:
            detail += f"\n  ... and {len(zero_modules) - 15} more"

        results.append(check(
            "COV-003",
            f"Modules with zero coverage: {len(zero_modules)}",
            "Test Coverage",
            "High",
            False,
            f"{len(zero_modules)} modules have no test files at all:\n{detail}",
        ))

    return results


# ---------------------------------------------------------------------------
# Console table
# ---------------------------------------------------------------------------

def print_module_table(modules):
    """Print a table of modules with coverage percentages."""
    if not modules:
        print("  (no modules found)")
        return

    # Sort by coverage ascending so worst modules appear first.
    sorted_mods = sorted(
        modules.items(),
        key=lambda kv: (
            kv[1]["covered"] / kv[1]["total"] if kv[1]["total"] > 0 else 0
        ),
    )

    # Column widths.
    name_w = max(len(m) for m, _ in sorted_mods)
    name_w = max(name_w, 6)  # "Module" header

    header = f"  {'Module':<{name_w}}  {'Total':>5}  {'Tested':>6}  {'Coverage':>8}"
    print(header)
    print(f"  {'-' * name_w}  -----  ------  --------")

    for mod_name, info in sorted_mods:
        total = info["total"]
        covered = info["covered"]
        pct = (covered / total * 100.0) if total > 0 else 0.0
        bar = "*" if pct == 0 and total > 0 else ""
        print(f"  {mod_name:<{name_w}}  {total:>5}  {covered:>6}  {pct:>7.1f}% {bar}")

    print()
    print("  * = zero coverage")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description=(
            "Identify C# scripts with no test coverage by matching production "
            "scripts to test files by naming convention."
        ),
        epilog=(
            "Examples:\n"
            '  python scan_test_coverage.py --repo-dir "C:/repos/apps.studioa3"\n'
            '  python scan_test_coverage.py --repo-dir ../apps.studioa3 '
            "--config configs/studio-a3.json\n"
            '  python scan_test_coverage.py --repo-dir ../apps.studioa3 '
            "--output-dir ./my-reports\n"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--repo-dir",
        required=True,
        help="Path to the Unity project repository root (must contain Assets/)",
    )
    parser.add_argument(
        "--config",
        default=None,
        help="Path to an app config JSON (provides app_name, version, etc.)",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory for output JSON (default: output/reports/ in project root)",
    )

    args = parser.parse_args()

    # Resolve and validate repo directory.
    repo_dir = os.path.abspath(args.repo_dir)
    if not os.path.isdir(repo_dir):
        print(f"ERROR: Repo directory not found: {repo_dir}")
        sys.exit(2)

    assets_dir = os.path.join(repo_dir, "Assets")
    if not os.path.isdir(assets_dir):
        print(f"ERROR: No Assets/ folder in {repo_dir} — is this a Unity project?")
        sys.exit(2)

    # Load optional config.
    config = {}
    if args.config:
        if not os.path.isfile(args.config):
            print(f"ERROR: Config file not found: {args.config}")
            sys.exit(2)
        with open(args.config, "r", encoding="utf-8") as fh:
            config = json.load(fh)

    app_name = config.get("app_name", os.path.basename(repo_dir))
    app_slug = re.sub(r"[^a-z0-9]+", "-", app_name.lower()).strip("-")

    # Output directory.
    if args.output_dir:
        output_dir = os.path.abspath(args.output_dir)
    else:
        project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        output_dir = os.path.join(project_root, "output", "reports")
    os.makedirs(output_dir, exist_ok=True)

    # -- Discovery -------------------------------------------------------------
    print(f"=== Test Coverage Scanner ===")
    print(f"  App:  {app_name}")
    print(f"  Repo: {repo_dir}")
    print()

    print("  Discovering scripts...")
    production, tests = discover_scripts(repo_dir)
    print(f"    Production scripts: {len(production)}")
    print(f"    Test scripts:       {len(tests)}")
    print()

    # -- Matching --------------------------------------------------------------
    print("  Matching production scripts to tests...")
    covered, uncovered = match_coverage(production, tests)
    covered_set = {rel for rel, _ in covered}
    print(f"    Covered:   {len(covered)}")
    print(f"    Uncovered: {len(uncovered)}")
    print()

    # -- Grouping --------------------------------------------------------------
    modules = group_by_module(production, covered_set)

    # -- Build results ---------------------------------------------------------
    results = build_results(production, tests, covered, uncovered, modules)

    # -- Write JSON (compatible with coverage_report.py) -----------------------
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    data = {
        "release_version": config.get("expected_version", "unknown"),
        "app_name": app_name,
        "test_date": datetime.now().strftime("%Y-%m-%d"),
        "tester": "Automated Coverage Scan",
        "environment": {
            "scan_type": "Test coverage analysis (naming-convention matching)",
            "repo_path": repo_dir,
            "unity_version": config.get("unity_version", "unknown"),
        },
        "results": results,
        # Extra data for downstream tools that want the full breakdown.
        "coverage_detail": {
            "production_count": len(production),
            "test_count": len(tests),
            "covered_count": len(covered),
            "uncovered_count": len(uncovered),
            "modules": {
                name: {
                    "total": info["total"],
                    "covered": info["covered"],
                    "coverage_pct": round(
                        info["covered"] / info["total"] * 100, 1
                    ) if info["total"] > 0 else 0.0,
                    "uncovered_scripts": info["uncovered_scripts"],
                }
                for name, info in modules.items()
            },
            "high_priority_untested": [
                os.path.basename(rel)
                for rel in uncovered
                if _is_high_priority(os.path.basename(rel))
            ],
        },
    }

    results_path = os.path.join(
        output_dir, f"coverage_scan_{app_slug}_{timestamp}.json"
    )
    with open(results_path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2)

    # -- Console output --------------------------------------------------------
    print(f"{'=' * 64}")
    print(f"  MODULE COVERAGE: {app_name}")
    print(f"{'=' * 64}")
    print()
    print_module_table(modules)

    # High-priority untested scripts.
    hp_untested = [
        (rel, os.path.basename(rel))
        for rel in uncovered
        if _is_high_priority(os.path.basename(rel))
    ]
    if hp_untested:
        print(f"  HIGH-PRIORITY UNTESTED SCRIPTS ({len(hp_untested)}):")
        print(f"  {'-' * 50}")
        for rel, fname in hp_untested:
            print(f"    {fname:<40s}  {rel}")
        print()

    # Test result summary.
    print(f"{'=' * 64}")
    print(f"  TEST RESULTS")
    print(f"{'=' * 64}")
    for r in results:
        icon = {"pass": "[+] PASS", "fail": "[X] FAIL", "skip": "[-] SKIP"}[
            r["status"]
        ]
        line = f"  {icon}: {r['id']} - {r['title']}"
        if r["status"] != "pass" and r.get("notes"):
            first_line = r["notes"].split("\n")[0]
            line += f"\n         {first_line}"
        print(line)

    pass_count = sum(1 for r in results if r["status"] == "pass")
    fail_count = sum(1 for r in results if r["status"] == "fail")
    skip_count = sum(1 for r in results if r["status"] == "skip")

    print(f"{'-' * 64}")
    print(
        f"  Total: {len(results)} | Pass: {pass_count} | "
        f"Fail: {fail_count} | Skip: {skip_count}"
    )
    print(f"{'=' * 64}")
    print()
    print(f"Results saved to: {results_path}")
    print()
    print("To generate an HTML report:")
    cfg_flag = f' --config {args.config}' if args.config else ""
    print(
        f'  python 03-test-management/coverage_report.py '
        f'--results-file "{results_path}"{cfg_flag} --output html'
    )

    sys.exit(1 if fail_count > 0 else 0)


if __name__ == "__main__":
    main()

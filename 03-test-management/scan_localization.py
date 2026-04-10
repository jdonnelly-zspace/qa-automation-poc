#!/usr/bin/env python3
"""
scan_localization.py - Scan localization CSV files in a zSpace Unity project.

This script reads the CSV-based localization files used by zSpace Unity apps
and checks for three categories of problems:

  1. Missing translations  - A language cell is empty but en-US has content.
  2. Identical to English  - The translation is a copy-paste of en-US (not
                             actually translated).  Short strings and strings
                             that are the same across ALL languages are excluded.
  3. Placeholder mismatches - The translation is missing a placeholder that
                              en-US contains ({0}, {1}, <b>, </b>, etc.).
                              This causes runtime formatting errors.

The output is a results JSON compatible with coverage_report.py, plus a
detailed CSV listing every gap found, plus a console summary table.

Usage:
    python scan_localization.py \\
        --repo-dir "C:/repos/apps.studioa3" \\
        --config configs/studio-a3.json

    python scan_localization.py \\
        --repo-dir "C:/repos/apps.studioa3" \\
        --config configs/studio-a3.json \\
        --csv-files "Assets/StudioA3/LocalizationData/studioStrings.csv,Assets/CommonA3/zSpace/LocalizationData/strings.csv"

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import csv
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path


# ---------------------------------------------------------------------------
# Placeholder detection
# ---------------------------------------------------------------------------
# These are the formatting tokens used inside localization strings.  If en-US
# contains any of these, every translation must contain the same set or the
# app's UI will break at runtime.
# ---------------------------------------------------------------------------

# Matches {0}, {1}, {2}, etc.
_NUMERIC_PLACEHOLDER_RE = re.compile(r"\{\d+\}")

# Matches common HTML-like markup tags used in Unity rich text.
_MARKUP_TAGS = ["<b>", "</b>", "<i>", "</i>", "<br>", "<br/>", "<br />",
                "<color", "</color>", "<size", "</size>"]


def extract_placeholders(text):
    """Return a sorted list of all placeholders and markup tags found in text.

    This is used to compare the set of placeholders between en-US and a
    translation.  If the sets differ, the translation has a mismatch.
    """
    if not text:
        return []

    found = []

    # Numeric placeholders: {0}, {1}, etc.
    found.extend(_NUMERIC_PLACEHOLDER_RE.findall(text))

    # HTML/rich-text markup tags (case-insensitive match).
    text_lower = text.lower()
    for tag in _MARKUP_TAGS:
        # Count occurrences so "<b>bold</b>" produces ["<b>", "</b>"].
        count = text_lower.count(tag.lower())
        found.extend([tag] * count)

    return sorted(found)


# ---------------------------------------------------------------------------
# CSV reading
# ---------------------------------------------------------------------------

def read_localization_csv(csv_path):
    """Read a zSpace localization CSV and return (languages, rows).

    Returns:
        languages : list of str  - column headers after TAG (e.g. ["en-US", "es-US", ...])
        rows      : list of dict - one dict per key, mapping language -> value

    The CSV may contain multiline cells (quoted with embedded newlines).
    Python's csv module handles this correctly when quoting=QUOTE_MINIMAL.
    """
    languages = []
    rows = []

    with open(csv_path, "r", encoding="utf-8-sig", newline="") as f:
        reader = csv.reader(f)

        # -- Header row --
        header = next(reader, None)
        if header is None:
            return [], []

        # Strip BOM and whitespace from header cells.
        header = [h.strip() for h in header]

        # First column is "TAG" (the key), remaining columns are languages.
        if header[0].upper() != "TAG":
            print(f"  WARNING: First column is '{header[0]}', expected 'TAG'. Treating as TAG anyway.")

        languages = header[1:]

        # -- Data rows --
        for row_num, row in enumerate(reader, start=2):
            if not row or not row[0].strip():
                continue  # skip blank rows

            key = row[0].strip()
            values = {}
            for i, lang in enumerate(languages):
                cell_index = i + 1
                if cell_index < len(row):
                    values[lang] = row[cell_index]
                else:
                    values[lang] = ""  # row shorter than header = missing

            rows.append({"key": key, "values": values})

    return languages, rows


# ---------------------------------------------------------------------------
# Analysis
# ---------------------------------------------------------------------------

def analyze_csv(csv_path, languages, rows):
    """Run all three checks on a single CSV file.

    Returns:
        gaps     : list of dict - every individual gap found (for the detail CSV)
        stats    : dict         - per-language completeness stats
        summary  : dict         - totals for missing, identical, placeholder issues
    """
    gaps = []
    baseline_lang = "en-US"

    if baseline_lang not in languages:
        print(f"  WARNING: '{baseline_lang}' column not found in {csv_path}. Skipping analysis.")
        return gaps, {}, {"missing": 0, "identical": 0, "placeholder_mismatch": 0}

    non_english_languages = [lang for lang in languages if lang != baseline_lang]

    # Per-language counters.
    lang_stats = {}
    for lang in non_english_languages:
        lang_stats[lang] = {"total": 0, "missing": 0, "identical": 0, "placeholder_mismatch": 0}

    total_missing = 0
    total_identical = 0
    total_placeholder = 0

    csv_filename = os.path.basename(csv_path)

    for entry in rows:
        key = entry["key"]
        en_value = entry["values"].get(baseline_lang, "").strip()

        # If en-US itself is empty, nothing to check.
        if not en_value:
            continue

        en_placeholders = extract_placeholders(en_value)

        # Check if the string is the same across ALL languages (like "sa3", a
        # file extension or brand name).  If so, we skip the identical check.
        all_values = [entry["values"].get(lang, "").strip() for lang in languages]
        all_same = all(v == en_value for v in all_values if v)

        for lang in non_english_languages:
            translation = entry["values"].get(lang, "").strip()
            lang_stats[lang]["total"] += 1

            # ----- Check 1: Missing translation -----
            if not translation:
                gaps.append({
                    "file": csv_filename,
                    "language": lang,
                    "key": key,
                    "issue_type": "missing",
                    "en_value": en_value,
                    "translation": "",
                })
                lang_stats[lang]["missing"] += 1
                total_missing += 1
                continue  # no further checks if empty

            # ----- Check 2: Identical to English -----
            # Skip short strings (3 chars or fewer) and strings identical
            # across ALL languages (brand names, file extensions, etc.).
            if (translation == en_value
                    and len(en_value) > 3
                    and not all_same):
                gaps.append({
                    "file": csv_filename,
                    "language": lang,
                    "key": key,
                    "issue_type": "identical",
                    "en_value": en_value,
                    "translation": translation,
                })
                lang_stats[lang]["identical"] += 1
                total_identical += 1

            # ----- Check 3: Placeholder mismatch -----
            if en_placeholders:
                trans_placeholders = extract_placeholders(translation)
                if en_placeholders != trans_placeholders:
                    missing_ph = sorted(set(en_placeholders) - set(trans_placeholders))
                    extra_ph = sorted(set(trans_placeholders) - set(en_placeholders))
                    detail_parts = []
                    if missing_ph:
                        detail_parts.append(f"missing: {', '.join(missing_ph)}")
                    if extra_ph:
                        detail_parts.append(f"extra: {', '.join(extra_ph)}")
                    detail = "; ".join(detail_parts)

                    gaps.append({
                        "file": csv_filename,
                        "language": lang,
                        "key": key,
                        "issue_type": "placeholder_mismatch",
                        "en_value": en_value,
                        "translation": f"{translation}  [{detail}]",
                    })
                    lang_stats[lang]["placeholder_mismatch"] += 1
                    total_placeholder += 1

    summary = {
        "missing": total_missing,
        "identical": total_identical,
        "placeholder_mismatch": total_placeholder,
    }

    return gaps, lang_stats, summary


# ---------------------------------------------------------------------------
# Result builders (compatible with coverage_report.py)
# ---------------------------------------------------------------------------

def check(test_id, title, category, priority, passed, notes=""):
    """Create a test result entry (same format as scan_repo.py)."""
    return {
        "id": test_id,
        "title": title,
        "category": category,
        "priority": priority,
        "status": "pass" if passed else "fail",
        "notes": notes if not passed else "",
        "remediation": "" if passed else notes,
    }


def warn(test_id, title, category, priority, notes=""):
    """Create a warning-level result (status=skip, used for advisory findings)."""
    return {
        "id": test_id,
        "title": title,
        "category": category,
        "priority": priority,
        "status": "skip",
        "notes": notes,
    }


def build_test_results(csv_path, languages, rows, gaps, lang_stats, summary):
    """Build the list of test result dicts for one CSV file.

    Returns three result entries per file:
        L10N-001-{filename} : Overall completeness
        L10N-002-{filename} : Placeholder consistency
        L10N-003-{filename} : Identical-to-English check
    """
    results = []
    filename = os.path.basename(csv_path)
    # Safe suffix for test IDs (remove extension, replace dots/spaces).
    slug = Path(filename).stem.replace(".", "_").replace(" ", "_")

    baseline_lang = "en-US"
    non_english = [lang for lang in languages if lang != baseline_lang]
    total_cells = sum(lang_stats[lang]["total"] for lang in non_english) if non_english else 0
    total_missing = summary["missing"]

    # -- L10N-001: Overall completeness --
    # Pass threshold: >95% of all (language x key) cells are filled.
    if total_cells > 0:
        completeness_pct = ((total_cells - total_missing) / total_cells) * 100
    else:
        completeness_pct = 100.0

    passed_completeness = completeness_pct > 95.0

    # Build a per-language breakdown for the notes.
    lang_lines = []
    for lang in non_english:
        s = lang_stats[lang]
        if s["total"] > 0:
            pct = ((s["total"] - s["missing"]) / s["total"]) * 100
            lang_lines.append(f"  {lang}: {pct:.1f}% ({s['missing']} missing)")

    completeness_note = (
        f"Overall: {completeness_pct:.1f}% complete ({total_missing} missing out of "
        f"{total_cells} cells).\n" + "\n".join(lang_lines)
    )

    results.append(check(
        f"L10N-001-{slug}",
        f"Localization completeness - {filename}",
        "Localization",
        "High",
        passed_completeness,
        completeness_note if not passed_completeness else "",
    ))

    # -- L10N-002: Placeholder consistency --
    # Pass if zero placeholder mismatches.
    placeholder_count = summary["placeholder_mismatch"]
    passed_placeholder = placeholder_count == 0

    results.append(check(
        f"L10N-002-{slug}",
        f"Placeholder consistency - {filename}",
        "Localization",
        "Critical",
        passed_placeholder,
        f"{placeholder_count} placeholder mismatch(es) found. These will cause runtime "
        f"formatting errors.  Review the detail CSV for affected keys."
        if not passed_placeholder else "",
    ))

    # -- L10N-003: Identical-to-English --
    # Advisory: warn if >=10% of translations are identical to en-US.
    identical_count = summary["identical"]
    translated_cells = total_cells - total_missing  # cells that have *something*
    if translated_cells > 0:
        identical_pct = (identical_count / translated_cells) * 100
    else:
        identical_pct = 0.0

    if identical_pct >= 10.0:
        results.append(warn(
            f"L10N-003-{slug}",
            f"Identical-to-English check - {filename}",
            "Localization",
            "Medium",
            f"{identical_count} translations ({identical_pct:.1f}%) are identical to en-US. "
            f"Review the detail CSV to confirm these are intentional.",
        ))
    else:
        results.append(check(
            f"L10N-003-{slug}",
            f"Identical-to-English check - {filename}",
            "Localization",
            "Medium",
            True,
        ))

    return results


# ---------------------------------------------------------------------------
# File discovery
# ---------------------------------------------------------------------------

def discover_csv_files(repo_dir):
    """Auto-discover localization CSV files in the Unity project.

    Looks for:
      - Any CSV file under a folder named LocalizationData
      - vivedStrings.csv anywhere in the tree
    """
    found = set()
    repo_path = Path(repo_dir)

    # Pattern 1: **/LocalizationData/*.csv
    for p in repo_path.rglob("LocalizationData/*.csv"):
        found.add(str(p))

    # Pattern 2: **/vivedStrings.csv
    for p in repo_path.rglob("vivedStrings.csv"):
        found.add(str(p))

    return sorted(found)


# ---------------------------------------------------------------------------
# Detail CSV writer
# ---------------------------------------------------------------------------

def write_detail_csv(output_path, all_gaps):
    """Write the detailed gap report CSV.

    Columns: File, Language, Key, Issue Type, English Value, Translation Value
    """
    with open(output_path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
        writer.writerow(["File", "Language", "Key", "Issue Type",
                         "English Value", "Translation Value"])
        for gap in all_gaps:
            writer.writerow([
                gap["file"],
                gap["language"],
                gap["key"],
                gap["issue_type"],
                gap["en_value"],
                gap["translation"],
            ])


# ---------------------------------------------------------------------------
# Console output
# ---------------------------------------------------------------------------

def print_completeness_table(all_lang_stats, csv_files_processed):
    """Print a per-language completeness table to the console."""
    # Aggregate stats across all CSV files.
    aggregated = {}
    for csv_path, lang_stats in all_lang_stats.items():
        for lang, stats in lang_stats.items():
            if lang not in aggregated:
                aggregated[lang] = {"total": 0, "missing": 0, "identical": 0,
                                    "placeholder_mismatch": 0}
            for k in ("total", "missing", "identical", "placeholder_mismatch"):
                aggregated[lang][k] += stats[k]

    if not aggregated:
        print("  No language data to display.")
        return

    # Sort languages alphabetically.
    sorted_langs = sorted(aggregated.keys())

    # Print table header.
    print(f"  {'Language':<10} {'Complete':>10} {'Missing':>10} {'Identical':>10} {'PH Mismatch':>12}")
    print(f"  {'-'*10} {'-'*10} {'-'*10} {'-'*10} {'-'*12}")

    for lang in sorted_langs:
        s = aggregated[lang]
        if s["total"] > 0:
            pct = ((s["total"] - s["missing"]) / s["total"]) * 100
        else:
            pct = 100.0
        print(f"  {lang:<10} {pct:>9.1f}% {s['missing']:>10} {s['identical']:>10} "
              f"{s['placeholder_mismatch']:>12}")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Scan localization CSV files in a zSpace Unity project for translation gaps.",
        epilog=(
            "Examples:\n"
            "  python scan_localization.py --repo-dir ../apps.studioa3 --config configs/studio-a3.json\n"
            "  python scan_localization.py --repo-dir ../apps.studioa3 --config configs/studio-a3.json "
            "--csv-files \"Assets/StudioA3/LocalizationData/studioStrings.csv,"
            "Assets/CommonA3/zSpace/LocalizationData/strings.csv\"\n"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--repo-dir",
        required=True,
        help="Path to the Unity project repository root",
    )
    parser.add_argument(
        "--config",
        required=True,
        help="Path to the app config JSON file (used for app_name)",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory for output files (default: output/reports/ relative to project root)",
    )
    parser.add_argument(
        "--csv-files",
        default=None,
        help=(
            "Comma-separated list of CSV file paths relative to --repo-dir. "
            "If omitted, auto-discovers all CSVs under LocalizationData/ folders "
            "and any vivedStrings.csv."
        ),
    )

    args = parser.parse_args()

    # -- Validate inputs --
    repo_dir = os.path.abspath(args.repo_dir)
    if not os.path.isdir(repo_dir):
        print(f"ERROR: Repo directory not found: {repo_dir}")
        sys.exit(1)

    with open(args.config, "r", encoding="utf-8") as f:
        config = json.load(f)

    app_name = config.get("app_name", "Unknown App")
    app_slug = app_name.replace("'", "").replace(" ", "-").lower()

    # -- Output directory --
    if args.output_dir is None:
        project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        output_dir = os.path.join(project_root, "output", "reports")
    else:
        output_dir = args.output_dir
    os.makedirs(output_dir, exist_ok=True)

    # -- Resolve CSV files --
    if args.csv_files:
        csv_paths = []
        for rel_path in args.csv_files.split(","):
            rel_path = rel_path.strip()
            full_path = os.path.join(repo_dir, rel_path)
            if os.path.isfile(full_path):
                csv_paths.append(full_path)
            else:
                print(f"  WARNING: CSV file not found, skipping: {full_path}")
    else:
        csv_paths = discover_csv_files(repo_dir)

    if not csv_paths:
        print("ERROR: No localization CSV files found.")
        print("  Use --csv-files to specify paths, or check that the repo contains")
        print("  files under LocalizationData/ folders.")
        sys.exit(1)

    # -- Banner --
    print(f"=== zSpace Localization Scanner ===")
    print(f"  App:  {app_name}")
    print(f"  Repo: {repo_dir}")
    print(f"  CSV files to scan: {len(csv_paths)}")
    for p in csv_paths:
        print(f"    - {os.path.relpath(p, repo_dir)}")
    print()

    # -- Process each CSV file --
    all_results = []
    all_gaps = []
    all_lang_stats = {}   # keyed by csv_path

    for csv_path in csv_paths:
        rel = os.path.relpath(csv_path, repo_dir)
        print(f"Scanning: {rel}")

        languages, rows = read_localization_csv(csv_path)

        if not languages:
            print(f"  WARNING: No language columns found.  Skipping.")
            continue

        print(f"  {len(rows)} keys, {len(languages)} languages: {', '.join(languages)}")

        gaps, lang_stats, summary = analyze_csv(csv_path, languages, rows)

        all_gaps.extend(gaps)
        all_lang_stats[csv_path] = lang_stats

        # Build test results for this file.
        results = build_test_results(csv_path, languages, rows, gaps, lang_stats, summary)
        all_results.extend(results)

        # Per-file console summary.
        print(f"  Missing: {summary['missing']}  |  "
              f"Identical: {summary['identical']}  |  "
              f"Placeholder: {summary['placeholder_mismatch']}")
        print()

    # -- Write results JSON (compatible with coverage_report.py) --
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    results_data = {
        "release_version": config.get("expected_version", "1.0.0"),
        "app_name": app_name,
        "test_date": datetime.now().strftime("%Y-%m-%d"),
        "tester": "Automated Localization Scan",
        "environment": {
            "scan_type": "Localization CSV analysis",
            "repo_path": repo_dir,
            "csv_files_scanned": len(csv_paths),
        },
        "results": all_results,
    }

    results_path = os.path.join(output_dir, f"l10n_results_{app_slug}_{timestamp}.json")
    with open(results_path, "w", encoding="utf-8") as f:
        json.dump(results_data, f, indent=2)
    print(f"Results JSON: {results_path}")

    # -- Write detail CSV --
    detail_path = os.path.join(output_dir, f"localization_gaps_{app_slug}_{timestamp}.csv")
    write_detail_csv(detail_path, all_gaps)
    print(f"Detail CSV:   {detail_path}")
    print()

    # -- Console completeness table --
    print(f"{'='*64}")
    print(f"  LOCALIZATION COMPLETENESS BY LANGUAGE")
    print(f"{'='*64}")
    print_completeness_table(all_lang_stats, csv_paths)
    print()

    # -- Overall summary --
    pass_count = sum(1 for r in all_results if r["status"] == "pass")
    fail_count = sum(1 for r in all_results if r["status"] == "fail")
    warn_count = sum(1 for r in all_results if r["status"] == "skip")
    total = len(all_results)

    print(f"{'='*64}")
    print(f"  SUMMARY: {app_name}")
    print(f"{'='*64}")
    print(f"  Total checks:  {total}")
    print(f"  Passed:        {pass_count}")
    print(f"  Failed:        {fail_count}")
    print(f"  Warnings:      {warn_count}")
    print(f"  Total gaps:    {len(all_gaps)}")
    print(f"{'='*64}")

    # Exit code: 1 if any failures.
    if fail_count > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()

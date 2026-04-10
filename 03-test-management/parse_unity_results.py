#!/usr/bin/env python3
"""
parse_unity_results.py - Parse NUnit XML results from Unity Test Runner
into the JSON format used by coverage_report.py.

Usage:
    python parse_unity_results.py --xml-file ../output/reports/unity_test_results_studio-a3.xml
    python parse_unity_results.py --xml-file results.xml --app-name "Studio A3" --release-version 1.0.0.0

Part of Prototype #2 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path


def parse_nunit_xml(xml_path):
    """Parse NUnit XML test results and return a list of test result dicts."""
    tree = ET.parse(xml_path)
    root = tree.getroot()

    results = []
    test_cases = root.iter("test-case")

    for tc in test_cases:
        fullname = tc.get("fullname", "")
        classname = tc.get("classname", "")
        name = tc.get("name", "")
        result = tc.get("result", "Unknown")
        duration = tc.get("duration", "0")

        # Map NUnit result to our status
        status_map = {
            "Passed": "pass",
            "Failed": "fail",
            "Skipped": "skip",
            "Inconclusive": "skip",
        }
        status = status_map.get(result, "fail")

        # Extract class name for category grouping
        # e.g., "zSpace.StudioA3.Tests.UndoRedoManagerTests" -> "UndoRedoManagerTests"
        short_class = classname.split(".")[-1] if classname else "Unknown"

        # Build notes from failure message if present
        notes = ""
        failure = tc.find("failure")
        if failure is not None:
            msg = failure.find("message")
            if msg is not None and msg.text:
                notes = msg.text.strip()

        # Generate a test ID from class + index
        test_id = f"UT-{len(results) + 1:03d}"

        results.append({
            "id": test_id,
            "title": f"{short_class}: {name}",
            "category": "Unity Unit Tests",
            "priority": "High",
            "status": status,
            "notes": notes,
            "remediation": "",
            "duration_sec": float(duration),
            "full_name": fullname,
        })

    return results, root


def get_run_summary(root):
    """Extract top-level run summary from NUnit XML root."""
    return {
        "total": int(root.get("total", 0)),
        "passed": int(root.get("passed", 0)),
        "failed": int(root.get("failed", 0)),
        "skipped": int(root.get("skipped", 0)),
        "duration": float(root.get("duration", 0)),
        "start_time": root.get("start-time", ""),
        "end_time": root.get("end-time", ""),
    }


def main():
    parser = argparse.ArgumentParser(
        description="Parse Unity NUnit XML results into coverage report JSON"
    )
    parser.add_argument(
        "--xml-file", required=True,
        help="Path to the NUnit XML results file"
    )
    parser.add_argument(
        "--app-name", default="Studio A3",
        help="Application name (default: Studio A3)"
    )
    parser.add_argument(
        "--release-version", default="1.0.0.0",
        help="Release version (default: 1.0.0.0)"
    )
    parser.add_argument(
        "--output-dir", default=None,
        help="Output directory (default: output/reports/)"
    )
    parser.add_argument(
        "--merge-with", default=None,
        help="Merge results with an existing JSON results file"
    )
    args = parser.parse_args()

    xml_path = Path(args.xml_file)
    if not xml_path.exists():
        print(f"Error: XML file not found: {xml_path}")
        sys.exit(1)

    # Determine output directory
    if args.output_dir:
        output_dir = Path(args.output_dir)
    else:
        # Auto-detect project root
        script_dir = Path(__file__).resolve().parent
        output_dir = script_dir.parent / "output" / "reports"
    output_dir.mkdir(parents=True, exist_ok=True)

    # Parse
    print(f"Parsing Unity test results: {xml_path}")
    results, root = parse_nunit_xml(xml_path)
    summary = get_run_summary(root)

    print(f"  Total: {summary['total']}, Passed: {summary['passed']}, "
          f"Failed: {summary['failed']}, Skipped: {summary['skipped']}")
    print(f"  Duration: {summary['duration']:.3f}s")

    # Build output JSON
    app_slug = args.app_name.lower().replace(" ", "-").replace("'", "")
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    output_data = {
        "release_version": args.release_version,
        "app_name": args.app_name,
        "test_date": datetime.now().strftime("%Y-%m-%d"),
        "tester": "Unity Test Runner (Edit Mode, batch)",
        "environment": {
            "scan_type": "Unity Edit Mode tests via batch mode",
            "unity_test_duration_sec": summary["duration"],
            "nunit_engine": root.get("engine-version", "unknown"),
        },
        "results": results,
    }

    # Optionally merge with existing scan results
    if args.merge_with:
        merge_path = Path(args.merge_with)
        if merge_path.exists():
            print(f"  Merging with: {merge_path}")
            with open(merge_path, "r", encoding="utf-8") as f:
                existing = json.load(f)
            # Add unity results to existing results list
            existing["results"].extend(results)
            existing["tester"] += " + Unity Test Runner"
            output_data = existing
        else:
            print(f"  Warning: Merge file not found: {merge_path}")

    # Save standalone unity results JSON
    unity_json = output_dir / f"unity_results_{app_slug}_{timestamp}.json"
    with open(unity_json, "w", encoding="utf-8") as f:
        json.dump(output_data, f, indent=2)
    print(f"  Saved: {unity_json}")

    # If merging, also save the merged file
    if args.merge_with and Path(args.merge_with).exists():
        merged_json = output_dir / f"combined_scan_{app_slug}_{timestamp}.json"
        with open(merged_json, "w", encoding="utf-8") as f:
            json.dump(output_data, f, indent=2)
        print(f"  Saved merged: {merged_json}")
        return str(merged_json)

    return str(unity_json)


if __name__ == "__main__":
    main()

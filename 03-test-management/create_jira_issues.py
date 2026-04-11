#!/usr/bin/env python3
from __future__ import annotations
"""
create_jira_issues.py - Create Jira issues from QA scan failures.

Reads the merged scan results JSON produced by run_qa.py, filters to
failures, builds MCP-ready payloads for Jira issue creation, and outputs
either a dry-run preview or a batch JSON file for Claude to execute via
the Jira MCP tools.

Usage:
    # Dry run — preview what would be created
    python create_jira_issues.py \\
        --results-file output/reports/full_scan_studio-a3_*.json \\
        --config configs/studio-a3.json \\
        --dry-run

    # Generate batch file for Claude to execute via MCP
    python create_jira_issues.py \\
        --results-file output/reports/full_scan_studio-a3_*.json \\
        --config configs/studio-a3.json

    # Offline mode (skip duplicate check)
    python create_jira_issues.py \\
        --results-file ... --config ... --dry-run --skip-dedup

The batch JSON output contains one entry per issue, each with the exact
parameters needed for the Jira MCP createJiraIssue tool.  Claude reads
this file and calls createJiraIssue for each entry.

Part of QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import json
import logging
import os
import sys
from datetime import datetime
from pathlib import Path

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------

DEFAULT_JIRA_CONFIG = {
    "cloud_id": "zspace.atlassian.net",
    "default_issue_type": "Bug",
    "labels": ["qa-automation"],
    "summary_prefix": "[QA-Auto]",
    "priority_map": {
        "Critical": "Highest (Blocker)",
        "High": "High",
        "Medium": "Medium",
        "Low": "Low",
    },
}

MAX_SUMMARY_LENGTH = 255


# ---------------------------------------------------------------------------
# Load and filter scan results
# ---------------------------------------------------------------------------

def load_failures(results_path):
    """Load scan results JSON and return only failures."""
    with open(results_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    metadata = {
        "app_name": data.get("app_name", "Unknown"),
        "release_version": data.get("release_version", "unknown"),
        "test_date": data.get("test_date", datetime.now().strftime("%Y-%m-%d")),
    }

    failures = [r for r in data.get("results", []) if r.get("status") == "fail"]
    return failures, metadata


# ---------------------------------------------------------------------------
# Config helpers
# ---------------------------------------------------------------------------

def load_jira_config(config_path):
    """Extract jira section from app config, merged with defaults."""
    config = {}
    if config_path and os.path.isfile(config_path):
        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)

    jira_section = config.get("jira", {})

    # Merge with defaults — config values override defaults
    merged = dict(DEFAULT_JIRA_CONFIG)
    merged.update(jira_section)

    # Ensure priority_map is merged, not replaced
    if "priority_map" in jira_section:
        pm = dict(DEFAULT_JIRA_CONFIG["priority_map"])
        pm.update(jira_section["priority_map"])
        merged["priority_map"] = pm

    return merged


# ---------------------------------------------------------------------------
# Payload building
# ---------------------------------------------------------------------------

def build_payloads(failures, jira_config, metadata):
    """Build MCP-ready createJiraIssue payloads from scan failures."""
    project_key = jira_config.get("project_key")
    if not project_key:
        logger.error("No jira.project_key in config. Cannot build payloads.")
        return []

    cloud_id = jira_config.get("cloud_id", DEFAULT_JIRA_CONFIG["cloud_id"])
    issue_type = jira_config.get("default_issue_type", "Bug")
    prefix = jira_config.get("summary_prefix", "[QA-Auto]")
    labels = list(jira_config.get("labels", ["qa-automation"]))
    component = jira_config.get("component")
    priority_map = jira_config.get("priority_map", DEFAULT_JIRA_CONFIG["priority_map"])

    # Add version label
    version = metadata.get("release_version", "")
    if version and version != "unknown":
        labels.append(f"v{version}")

    payloads = []
    for result in failures:
        result_id = result.get("id", "UNKNOWN")
        title = result.get("title", "Untitled check")
        category = result.get("category", "General")
        priority = result.get("priority", "Medium")
        notes = result.get("notes", "")
        remediation = result.get("remediation", "")

        # Build summary (truncated to 255 chars)
        summary = f"{prefix} {result_id}: {title}"
        if len(summary) > MAX_SUMMARY_LENGTH:
            summary = summary[: MAX_SUMMARY_LENGTH - 3] + "..."

        # Build markdown description
        desc_parts = [
            f"## QA Automation Finding",
            f"",
            f"**Check ID:** {result_id}",
            f"**Category:** {category}",
            f"**Priority:** {priority}",
            f"**App:** {metadata.get('app_name', 'Unknown')}",
            f"**Version:** {metadata.get('release_version', 'unknown')}",
            f"**Scan Date:** {metadata.get('test_date', 'unknown')}",
            f"",
        ]
        if notes:
            desc_parts.append("### Details")
            desc_parts.append(notes)
            desc_parts.append("")
        if remediation:
            desc_parts.append("### Remediation")
            desc_parts.append(remediation)
            desc_parts.append("")
        desc_parts.append("---")
        desc_parts.append("*Created by QA Automation POC (`create_jira_issues.py`)*")

        description = "\n".join(desc_parts)

        # Build additional_fields
        additional_fields = {
            "priority": {"name": priority_map.get(priority, "Medium")},
            "labels": labels,
        }
        if component:
            additional_fields["components"] = [{"name": component}]

        payload = {
            "cloudId": cloud_id,
            "projectKey": project_key,
            "issueTypeName": issue_type,
            "summary": summary,
            "description": description,
            "contentFormat": "markdown",
            "additional_fields": json.dumps(additional_fields),
            "_meta": {
                "result_id": result_id,
                "category": category,
                "priority": priority,
                "dedup_jql": build_dedup_jql(result_id, project_key),
            },
        }
        payloads.append(payload)

    return payloads


def build_dedup_jql(result_id, project_key):
    """Build a JQL query to find existing open issues for this check."""
    return (
        f'project = {project_key} AND '
        f'summary ~ "[QA-Auto] {result_id}" AND '
        f'statusCategory != Done'
    )


# ---------------------------------------------------------------------------
# Dry-run report
# ---------------------------------------------------------------------------

def dry_run_report(payloads, output_dir):
    """Print a preview table and write preview JSON."""
    if not payloads:
        logger.info("\n  No failures found — nothing to create.\n")
        return None

    project_key = payloads[0]["projectKey"]
    app_name = payloads[0].get("_meta", {}).get("category", "")

    logger.info("")
    logger.info("  JIRA ISSUE PREVIEW -> %s", project_key)
    logger.info("  " + "=" * 68)
    logger.info("  %-4s %-14s %-10s %-24s %s", "#", "Check ID", "Priority", "Category", "Action")
    logger.info("  %-4s %-14s %-10s %-24s %s", "---", "-" * 12, "-" * 8, "-" * 22, "------")

    for i, p in enumerate(payloads, 1):
        meta = p.get("_meta", {})
        action = meta.get("action", "CREATE")
        existing = meta.get("existing_key", "")
        action_str = f"SKIP ({existing})" if action == "SKIP" else "CREATE"
        logger.info(
            "  %-4d %-14s %-10s %-24s %s",
            i,
            meta.get("result_id", "?"),
            meta.get("priority", "?"),
            meta.get("category", "?")[:22],
            action_str,
        )

    create_count = sum(1 for p in payloads if p.get("_meta", {}).get("action") != "SKIP")
    skip_count = len(payloads) - create_count
    logger.info("  " + "=" * 68)
    logger.info("  Would create: %d | Skipped (existing): %d", create_count, skip_count)
    logger.info("")

    # Write preview JSON
    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    preview_path = os.path.join(output_dir, f"jira_preview_{project_key.lower()}_{timestamp}.json")
    with open(preview_path, "w", encoding="utf-8") as f:
        json.dump(payloads, f, indent=2)
    logger.info("  Preview saved to: %s", preview_path)

    return preview_path


# ---------------------------------------------------------------------------
# Batch file output
# ---------------------------------------------------------------------------

def write_batch(payloads, output_dir):
    """Write MCP-ready payloads to a batch JSON file.

    Filters out SKIP entries and strips _meta before writing.
    The output file is designed to be read by Claude, which then
    calls createJiraIssue MCP tool for each entry.
    """
    to_create = []
    for p in payloads:
        if p.get("_meta", {}).get("action") == "SKIP":
            continue
        # Copy without _meta (internal tracking, not for MCP)
        clean = {k: v for k, v in p.items() if k != "_meta"}
        to_create.append(clean)

    if not to_create:
        logger.info("\n  No issues to create (all skipped or no failures).\n")
        return None

    os.makedirs(output_dir, exist_ok=True)
    project_key = to_create[0].get("projectKey", "unknown").lower()
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    batch_path = os.path.join(output_dir, f"jira_batch_{project_key}_{timestamp}.json")

    with open(batch_path, "w", encoding="utf-8") as f:
        json.dump(to_create, f, indent=2)

    logger.info("\n  Batch file written: %s", batch_path)
    logger.info("  Contains %d issue(s) ready for creation via MCP createJiraIssue.", len(to_create))
    logger.info("")
    logger.info("  Next step: Ask Claude to read this file and create each issue")
    logger.info("  using the Jira MCP tools, or run with --dry-run to preview first.")

    return batch_path


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Create Jira issues from QA scan failures.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Examples:\n"
            '  python create_jira_issues.py --results-file output/reports/full_scan_studio-a3_*.json --config configs/studio-a3.json --dry-run\n'
            '  python create_jira_issues.py --results-file output/reports/full_scan_studio-a3_*.json --config configs/studio-a3.json\n'
        ),
    )
    parser.add_argument(
        "--results-file", required=True,
        help="Path to the merged scan results JSON (from run_qa.py output)",
    )
    parser.add_argument(
        "--config", default=None,
        help="Path to the app config JSON (must contain a 'jira' section)",
    )
    parser.add_argument(
        "--dry-run", action="store_true",
        help="Preview what would be created without generating a batch file",
    )
    parser.add_argument(
        "--skip-dedup", action="store_true",
        help="Skip duplicate checking (for offline use or first run)",
    )
    parser.add_argument(
        "--output-dir", default=None,
        help="Output directory (default: output/reports/)",
    )
    parser.add_argument("--verbose", action="store_true", help="Enable verbose logging")
    parser.add_argument("--quiet", action="store_true", help="Suppress informational output")

    args = parser.parse_args()

    # Matches setup_logging() in qa_common.py
    level = logging.DEBUG if args.verbose else logging.WARNING if args.quiet else logging.INFO
    logging.basicConfig(level=level, format="%(message)s",
                        handlers=[logging.StreamHandler(sys.stdout)])

    # Resolve results file (support glob patterns)
    import glob as glob_mod
    results_matches = sorted(glob_mod.glob(args.results_file), key=os.path.getmtime, reverse=True)
    if not results_matches:
        logger.error("No files match: %s", args.results_file)
        sys.exit(2)
    results_path = results_matches[0]
    if len(results_matches) > 1:
        logger.info("  Multiple matches, using most recent: %s", results_path)

    # Load config
    jira_config = load_jira_config(args.config)
    if not jira_config.get("project_key"):
        logger.error("Config file must contain a 'jira.project_key' field.")
        logger.error("Example: {\"jira\": {\"project_key\": \"ZSSTUDIO\", ...}}")
        sys.exit(2)

    # Resolve output dir
    if args.output_dir:
        output_dir = os.path.abspath(args.output_dir)
    else:
        project_root = Path(__file__).resolve().parent.parent
        output_dir = str(project_root / "output" / "reports")

    # Load failures
    failures, metadata = load_failures(results_path)
    logger.info("")
    logger.info("  Scan results: %s", results_path)
    logger.info("  App: %s | Version: %s | Failures: %d",
                metadata["app_name"], metadata["release_version"], len(failures))

    if not failures:
        logger.info("\n  No failures in scan results — nothing to file.\n")
        sys.exit(0)

    # Build payloads
    payloads = build_payloads(failures, jira_config, metadata)

    if not payloads:
        logger.info("\n  Could not build payloads (check config).\n")
        sys.exit(2)

    # Dedup note
    if args.skip_dedup:
        logger.info("  Dedup: skipped (--skip-dedup)")
        for p in payloads:
            p["_meta"]["action"] = "CREATE"
    else:
        logger.info("  Dedup: To check for existing issues, Claude should run")
        logger.info("         JQL searches using the dedup_jql in each payload's _meta.")
        for p in payloads:
            p["_meta"]["action"] = "CREATE"

    # Output
    if args.dry_run:
        dry_run_report(payloads, output_dir)
    else:
        # Also show preview, then write batch
        dry_run_report(payloads, output_dir)
        write_batch(payloads, output_dir)

    sys.exit(0)


if __name__ == "__main__":
    main()

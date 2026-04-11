#!/usr/bin/env python3
from __future__ import annotations
"""
coverage_report.py - Generate a test coverage dashboard from test results.

This script reads test results (JSON or CSV) and produces either an HTML
dashboard or a markdown summary showing pass/fail/skip rates overall and
per category.

Usage:
    python coverage_report.py --output html
    python coverage_report.py --results-file ./templates/sample_results.json --output markdown
    python coverage_report.py --results-file results.csv --output html

Exit codes:
    0 - All tests passed (no failures)
    1 - One or more test failures detected

The script includes embedded sample results so it works standalone for
demo purposes. Point --results-file at real data for production use.

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import csv
import html as html_mod
import json
import logging
import os
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Sample results fallback (loaded from templates/sample_results.json)
# ---------------------------------------------------------------------------

def _load_sample_results():
    """Load sample results from the templates directory."""
    sample_path = os.path.join(os.path.dirname(__file__), "templates", "sample_results.json")
    if os.path.isfile(sample_path):
        with open(sample_path, "r", encoding="utf-8") as f:
            return json.load(f)
    # Minimal fallback if template file is missing
    return {
    "release_version": "2.1.0",
    "app_name": "Franklin's Lab A3",
    "test_date": "2026-04-09",
    "tester": "QA Team",
    "environment": {
        "os": "Windows 11 Pro",
        "hardware": "zSpace Inspire",
        "unity_version": "2022.3.20f1",
    },
    "results": [
        {"id": "INST-001", "title": "Fresh install on supported hardware", "category": "Installation & Licensing", "priority": "Critical", "status": "pass"},
        {"id": "INST-002", "title": "Upgrade from previous version preserves settings", "category": "Installation & Licensing", "priority": "Critical", "status": "pass"},
        {"id": "INST-003", "title": "Uninstall removes all application files", "category": "Installation & Licensing", "priority": "High", "status": "pass"},
        {"id": "INST-004", "title": "License activation with valid key", "category": "Installation & Licensing", "priority": "Critical", "status": "pass"},
        {"id": "INST-005", "title": "License activation with invalid key shows error", "category": "Installation & Licensing", "priority": "High", "status": "pass"},
        {"id": "INST-006", "title": "Offline license activation workflow", "category": "Installation & Licensing", "priority": "Medium", "status": "skip", "notes": "Offline activation not supported in this build"},
        {"id": "INST-007", "title": "License deactivation and transfer", "category": "Installation & Licensing", "priority": "High", "status": "pass"},
        {"id": "INST-008", "title": "Install on minimum spec hardware", "category": "Installation & Licensing", "priority": "High", "status": "fail",
         "notes": "Installer hangs at 87% on i5-8250U with 8GB RAM",
         "remediation": "Known issue: The installer's asset extraction step exceeds available memory on 8GB systems. Recommended fix: Add a pre-install memory check that warns the user, or optimize the extraction to stream assets instead of loading them all into memory. Workaround: Close all other applications before installing."},
        {"id": "INST-009", "title": "Silent install via command line", "category": "Installation & Licensing", "priority": "Medium", "status": "pass"},
        {"id": "INST-010", "title": "Install path with spaces and special characters", "category": "Installation & Licensing", "priority": "Medium", "status": "pass"},
        {"id": "STEREO-001", "title": "Stereo rendering activates on supported display", "category": "Stereoscopy & Head Tracking", "priority": "Critical", "status": "pass"},
        {"id": "STEREO-002", "title": "Head tracking responds to user movement", "category": "Stereoscopy & Head Tracking", "priority": "Critical", "status": "pass"},
        {"id": "STEREO-003", "title": "Stereo comfort - no excessive ghosting", "category": "Stereoscopy & Head Tracking", "priority": "High", "status": "pass"},
        {"id": "STEREO-004", "title": "IPD adjustment functions correctly", "category": "Stereoscopy & Head Tracking", "priority": "High", "status": "fail",
         "notes": "IPD slider does not update stereo separation in real-time; requires app restart",
         "remediation": "Root cause likely in the stereo camera rig not subscribing to IPD change events. Check the zSpace Core SDK callback registration for IPD updates. The camera separation value is probably only read at startup. Fix: Add a listener that updates camera separation when the IPD slider fires its OnValueChanged event."},
        {"id": "STEREO-005", "title": "Head tracking recovery after occlusion", "category": "Stereoscopy & Head Tracking", "priority": "Medium", "status": "pass"},
        {"id": "STEREO-006", "title": "Stereo rendering at native refresh rate", "category": "Stereoscopy & Head Tracking", "priority": "High", "status": "pass"},
        {"id": "INPUT-001", "title": "Stylus primary button interaction", "category": "Input Handling", "priority": "Critical", "status": "pass"},
        {"id": "INPUT-002", "title": "Stylus secondary button interaction", "category": "Input Handling", "priority": "High", "status": "pass"},
        {"id": "INPUT-003", "title": "Stylus 6DOF tracking accuracy", "category": "Input Handling", "priority": "Critical", "status": "pass"},
        {"id": "INPUT-004", "title": "Mouse fallback when stylus disconnected", "category": "Input Handling", "priority": "Medium", "status": "skip", "notes": "Mouse fallback is a stretch goal for this release"},
        {"id": "ZVIEW-001", "title": "zView presenter mode launches", "category": "zView", "priority": "High", "status": "pass"},
        {"id": "ZVIEW-002", "title": "zView video stream quality", "category": "zView", "priority": "High", "status": "pass"},
        {"id": "ZVIEW-003", "title": "zView augmented reality overlay", "category": "zView", "priority": "Medium", "status": "fail",
         "notes": "AR overlay offset by ~2cm on secondary display",
         "remediation": "Calibration issue between the zView camera feed and the Unity render pipeline. RCA steps: 1) Check if the offset is consistent across display models or specific to one. 2) Verify the zView SDK camera-to-world transform matrix. 3) Compare the AR overlay position with zView's built-in calibration tool. 4) If display-specific, likely a resolution/DPI scaling mismatch in the projection matrix."},
        {"id": "ZVIEW-004", "title": "zView disconnect and reconnect", "category": "zView", "priority": "Medium", "status": "pass"},
        {"id": "HW-001", "title": "Application renders on all supported GPU models", "category": "Hardware & Display", "priority": "Critical", "status": "pass"},
        {"id": "HW-002", "title": "Display resolution auto-detection", "category": "Hardware & Display", "priority": "High", "status": "pass"},
        {"id": "HW-003", "title": "Multi-monitor configuration handling", "category": "Hardware & Display", "priority": "Medium", "status": "pass"},
        {"id": "HW-004", "title": "GPU driver version compatibility", "category": "Hardware & Display", "priority": "High", "status": "pass"},
        {"id": "HW-005", "title": "Thermal throttling behavior under load", "category": "Hardware & Display", "priority": "Medium", "status": "skip", "notes": "Requires extended soak test; scheduled separately"},
        {"id": "PLAT-001", "title": "Windows Defender compatibility", "category": "Platform Features", "priority": "High", "status": "pass"},
        {"id": "PLAT-002", "title": "Windows Update does not break application", "category": "Platform Features", "priority": "Medium", "status": "pass"},
    ],
}

SAMPLE_RESULTS = _load_sample_results()


# ---------------------------------------------------------------------------
# Category-based RCA playbooks (used when no specific remediation is provided)
# ---------------------------------------------------------------------------
# When a test fails but no "remediation" field is in the results data, the
# report generates generic root cause analysis guidance based on the category.
# ---------------------------------------------------------------------------

RCA_PLAYBOOKS = {
    "Installation & Licensing": (
        "RCA Playbook: 1) Capture installer logs (check %TEMP% for MSI/NSIS logs). "
        "2) Reproduce on a clean VM to rule out environment-specific issues. "
        "3) Check Windows Event Viewer > Application for installer errors. "
        "4) Compare working vs failing machine specs (RAM, disk space, OS version, antivirus). "
        "5) If licensing-related, verify license server connectivity and check zCentral dashboard for activation records."
    ),
    "Stereoscopy & Head Tracking": (
        "RCA Playbook: 1) Check zSpace Diagnostics tool for hardware/driver status. "
        "2) Verify zSpace SDK version matches the app's expected version. "
        "3) Capture Unity Player.log for rendering errors or warnings. "
        "4) Test on a second zSpace display to isolate hardware vs software issues. "
        "5) Compare stereo camera settings in Unity Inspector against SDK documentation."
    ),
    "Input Handling": (
        "RCA Playbook: 1) Verify stylus firmware is up to date via zSpace System Software. "
        "2) Check Unity Input Manager settings for button mappings. "
        "3) Test with zSpace Diagnostics to confirm raw tracking data is correct. "
        "4) Review the input event handling code for race conditions or dropped events. "
        "5) If mouse fallback issue, check whether the app's input abstraction layer has a fallback path."
    ),
    "zView": (
        "RCA Playbook: 1) Verify zView version compatibility with the app and SDK. "
        "2) Check secondary display resolution and refresh rate settings. "
        "3) Test zView in isolation (without the app) to confirm it functions normally. "
        "4) Capture zView logs from %APPDATA%/zSpace/zView/. "
        "5) If overlay/alignment issue, run the zView calibration wizard and re-test."
    ),
    "Hardware & Display": (
        "RCA Playbook: 1) Document exact hardware model, GPU driver version, and OS build. "
        "2) Check GPU vendor's known issues list for the installed driver. "
        "3) Test with both the minimum and latest recommended driver versions. "
        "4) Monitor GPU/CPU temps during reproduction with HWMonitor or similar. "
        "5) If display-specific, test on another display model to isolate."
    ),
    "Platform Features": (
        "RCA Playbook: 1) Check Windows version and recent update history (winver, Get-HotFix). "
        "2) Review Windows Defender/SmartScreen logs for false positives. "
        "3) Verify code signing certificate chain is valid and not expired. "
        "4) Test with a clean Windows install (no third-party antivirus). "
        "5) Check Windows compatibility settings on the app executable."
    ),
}

DEFAULT_RCA_PLAYBOOK = (
    "RCA Playbook: 1) Reproduce the failure and capture logs/screenshots. "
    "2) Check if this is a regression (did it pass in the previous release?). "
    "3) Isolate the variable: hardware, software version, environment config. "
    "4) Review recent code changes in the related area (git log). "
    "5) File a bug with reproduction steps, logs, and environment details."
)


# ---------------------------------------------------------------------------
# Remediation helper
# ---------------------------------------------------------------------------

def get_remediation(test_result: dict) -> str:
    """
    Return remediation guidance for a failed test.

    Priority order:
      1. If the result has a "remediation" field, use it (known fix).
      2. Otherwise, return the category-specific RCA playbook.
      3. If the category isn't recognized, return the generic playbook.
    """
    if test_result.get("remediation"):
        return test_result["remediation"]

    category = test_result.get("category", "")
    return RCA_PLAYBOOKS.get(category, DEFAULT_RCA_PLAYBOOK)


# ---------------------------------------------------------------------------
# Data loading
# ---------------------------------------------------------------------------

def load_results(results_file: str | None) -> dict:
    """
    Load test results from a JSON or CSV file, or use embedded sample data.

    JSON files should match the structure of sample_results.json.
    CSV files should have at minimum: id, title, category, priority, status columns.

    Args:
        results_file: Path to a results file, or None to use sample data.

    Returns:
        A dict with keys: release_version, app_name, test_date, results (list).
    """
    if results_file and os.path.isfile(results_file):
        ext = Path(results_file).suffix.lower()

        if ext == ".json":
            with open(results_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            logger.info("  Loaded %d results from %s", len(data.get("results", [])), results_file)
            return data

        elif ext == ".csv":
            results = []
            with open(results_file, "r", encoding="utf-8") as f:
                reader = csv.DictReader(f)
                for row in reader:
                    results.append({
                        "id": row.get("id", ""),
                        "title": row.get("title", ""),
                        "category": row.get("category", "Unknown"),
                        "priority": row.get("priority", "Medium"),
                        "status": row.get("status", "pending").lower(),
                        "notes": row.get("notes", ""),
                    })
            logger.info("  Loaded %d results from %s", len(results), results_file)
            return {
                "release_version": "unknown",
                "app_name": "unknown",
                "test_date": datetime.now().strftime("%Y-%m-%d"),
                "results": results,
            }

        else:
            logger.warning("  Warning: Unsupported file format '%s'. Using sample data.", ext)

    logger.info("  Using embedded sample results (standalone POC mode)")
    return SAMPLE_RESULTS


# ---------------------------------------------------------------------------
# Analysis helpers
# ---------------------------------------------------------------------------

def analyze_results(results: list[dict]) -> dict:
    """
    Compute summary statistics from a list of test result dicts.

    Returns a dict with:
        - total, pass_count, fail_count, skip_count, pass_rate
        - categories: dict of category_name -> {total, pass, fail, skip, pass_rate}
        - failures: list of failed test dicts
        - skipped: list of skipped test dicts
    """
    total = len(results)
    pass_count = sum(1 for r in results if r["status"] == "pass")
    fail_count = sum(1 for r in results if r["status"] == "fail")
    skip_count = sum(1 for r in results if r["status"] == "skip")

    # Per-category breakdown.
    categories = defaultdict(lambda: {"total": 0, "pass": 0, "fail": 0, "skip": 0})
    for r in results:
        cat = r.get("category", "Unknown")
        categories[cat]["total"] += 1
        status = r["status"]
        if status in ("pass", "fail", "skip"):
            categories[cat][status] += 1

    # Compute pass rates.
    for cat_data in categories.values():
        executed = cat_data["total"] - cat_data["skip"]
        if executed > 0:
            cat_data["pass_rate"] = round(cat_data["pass"] / executed * 100, 1)
        else:
            cat_data["pass_rate"] = 0.0

    overall_executed = total - skip_count
    overall_pass_rate = round(pass_count / overall_executed * 100, 1) if overall_executed > 0 else 0.0

    passed = [r for r in results if r["status"] == "pass"]
    failures = [r for r in results if r["status"] == "fail"]
    skipped = [r for r in results if r["status"] == "skip"]

    return {
        "total": total,
        "pass_count": pass_count,
        "fail_count": fail_count,
        "skip_count": skip_count,
        "pass_rate": overall_pass_rate,
        "categories": dict(categories),
        "passed": passed,
        "failures": failures,
        "skipped": skipped,
    }


# ---------------------------------------------------------------------------
# Markdown output
# ---------------------------------------------------------------------------

def generate_markdown(data: dict, analysis: dict) -> str:
    """
    Build a markdown report string for terminal or file output.

    Args:
        data: The full results dict (with release_version, app_name, etc.)
        analysis: The output of analyze_results().

    Returns:
        A markdown-formatted string.
    """
    lines = []
    lines.append("# Test Coverage Report")
    lines.append("")
    lines.append(f"**Application:** {data.get('app_name', 'N/A')}")
    lines.append(f"**Release:** {data.get('release_version', 'N/A')}")
    lines.append(f"**Date:** {data.get('test_date', 'N/A')}")
    lines.append(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M')}")
    lines.append("")

    # Overall summary.
    lines.append("## Overall Results")
    lines.append("")
    lines.append(f"| Metric | Count | Percentage |")
    lines.append(f"|--------|------:|----------:|")
    lines.append(f"| Total  | {analysis['total']} | |")
    lines.append(f"| Pass   | {analysis['pass_count']} | {analysis['pass_rate']}% |")
    fail_pct = round(analysis['fail_count'] / max(analysis['total'], 1) * 100, 1)
    skip_pct = round(analysis['skip_count'] / max(analysis['total'], 1) * 100, 1)
    lines.append(f"| Fail   | {analysis['fail_count']} | {fail_pct}% |")
    lines.append(f"| Skip   | {analysis['skip_count']} | {skip_pct}% |")
    lines.append("")

    # Per-category breakdown.
    lines.append("## Per-Category Breakdown")
    lines.append("")
    lines.append(f"| Category | Total | Pass | Fail | Skip | Pass Rate |")
    lines.append(f"|----------|------:|-----:|-----:|-----:|----------:|")
    for cat_name, cat_data in sorted(analysis["categories"].items()):
        lines.append(
            f"| {cat_name} | {cat_data['total']} | {cat_data['pass']} "
            f"| {cat_data['fail']} | {cat_data['skip']} | {cat_data['pass_rate']}% |"
        )
    lines.append("")

    # Passed tests — full detail so reviewers can see exactly what was validated.
    if analysis["passed"]:
        lines.append("## Passed Tests")
        lines.append("")
        lines.append(f"| ID | Title | Priority | Category |")
        lines.append(f"|-----|-------|----------|----------|")
        for p in analysis["passed"]:
            lines.append(
                f"| {p['id']} | {p['title']} | {p.get('priority', '')} | {p.get('category', '')} |"
            )
        lines.append("")

    # Failures with remediation guidance.
    if analysis["failures"]:
        lines.append("## Failing Tests")
        lines.append("")
        for f in analysis["failures"]:
            notes = f.get("notes", "")
            remediation = get_remediation(f)
            lines.append(f"### {f['id']} - {f['title']} [{f.get('priority', '')}]")
            lines.append("")
            if notes:
                lines.append(f"**Observation:** {notes}")
                lines.append("")
            lines.append(f"**Assessment & Next Steps:** {remediation}")
            lines.append("")

    # Skipped.
    if analysis["skipped"]:
        lines.append("## Skipped Tests")
        lines.append("")
        for s in analysis["skipped"]:
            notes = f" - {s.get('notes', '')}" if s.get("notes") else ""
            lines.append(f"- **{s['id']}** {s['title']}{notes}")
        lines.append("")

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# HTML output
# ---------------------------------------------------------------------------

def _progress_bar_html(label: str, passed: int, failed: int, skipped: int, total: int) -> str:
    """
    Generate an HTML progress bar segment for one category.

    Uses simple inline CSS -- no JavaScript frameworks required.
    """
    if total == 0:
        return ""

    pass_pct = passed / total * 100
    fail_pct = failed / total * 100
    skip_pct = skipped / total * 100

    executed = total - skipped
    pass_rate = round(passed / executed * 100, 1) if executed > 0 else 0.0

    return f"""
    <div class="category-row">
      <div class="category-label">{html_mod.escape(label)}</div>
      <div class="bar-container">
        <div class="bar-segment bar-pass" style="width:{pass_pct:.1f}%"></div>
        <div class="bar-segment bar-fail" style="width:{fail_pct:.1f}%"></div>
        <div class="bar-segment bar-skip" style="width:{skip_pct:.1f}%"></div>
      </div>
      <div class="category-stats">{passed}/{total} passed ({pass_rate}%)</div>
    </div>
    """


def generate_html(data: dict, analysis: dict, output_path: str) -> str:
    """
    Generate a self-contained HTML coverage report.

    The report uses only inline CSS -- no external dependencies or JS frameworks.

    Args:
        data: The full results dict.
        analysis: The output of analyze_results().
        output_path: File path to write the HTML report.

    Returns:
        The output file path.
    """
    release = data.get("release_version", "N/A")
    app = data.get("app_name", "N/A")
    test_date = data.get("test_date", "N/A")
    env = data.get("environment", {})
    generated = datetime.now().strftime("%Y-%m-%d %H:%M")

    # Determine overall status color.
    if analysis["fail_count"] == 0 and analysis["skip_count"] == 0:
        status_class = "status-pass"
        status_text = "ALL TESTS PASSED"
    elif analysis["fail_count"] == 0:
        status_class = "status-warn"
        status_text = f"PASSED (with {analysis['skip_count']} skipped)"
    else:
        status_class = "status-fail"
        status_text = f"{analysis['fail_count']} FAILURE(S) DETECTED"

    # Build category progress bars.
    category_bars = ""
    for cat_name in sorted(analysis["categories"].keys()):
        cd = analysis["categories"][cat_name]
        category_bars += _progress_bar_html(cat_name, cd["pass"], cd["fail"], cd["skip"], cd["total"])

    # Build failure cards (each failure gets its own card with remediation).
    failure_cards = ""
    esc = html_mod.escape
    for f in analysis["failures"]:
        notes = esc(f.get("notes", ""))
        remediation = esc(get_remediation(f))
        has_known_fix = bool(f.get("remediation"))
        fix_label = "Known Fix" if has_known_fix else "RCA Playbook"
        fix_icon = "&#9989;" if has_known_fix else "&#128270;"  # checkmark vs magnifying glass

        failure_cards += f"""
        <div class="failure-card">
          <div class="failure-header">
            <code>{esc(f['id'])}</code>
            <span class="priority-{esc(f.get('priority','Medium').lower())}">{esc(f.get('priority','Medium'))}</span>
            <span class="failure-category">{esc(f.get('category',''))}</span>
          </div>
          <div class="failure-title">{esc(f['title'])}</div>
          {"<div class='failure-notes'><strong>Observation:</strong> " + notes + "</div>" if notes else ""}
          <div class="failure-remediation">
            <div class="remediation-label">{fix_icon} {fix_label}</div>
            <div class="remediation-text">{remediation}</div>
          </div>
        </div>
        """

    # Build skipped rows.
    skip_rows = ""
    for s in analysis["skipped"]:
        skip_rows += f"""
        <tr>
          <td><code>{esc(s['id'])}</code></td>
          <td>{esc(s['title'])}</td>
          <td>{esc(s.get('category',''))}</td>
          <td>{esc(s.get('notes',''))}</td>
        </tr>
        """

    # Build passed rows.
    pass_rows = ""
    for p in analysis["passed"]:
        pass_rows += f"""
        <tr>
          <td><code>{esc(p['id'])}</code></td>
          <td>{esc(p['title'])}</td>
          <td><span class="priority-{esc(p.get('priority','Medium').lower())}">{esc(p.get('priority','Medium'))}</span></td>
          <td>{esc(p.get('category',''))}</td>
        </tr>
        """

    # Environment info section.
    env_html = ""
    if env:
        env_items = "".join(f"<li><strong>{esc(str(k))}:</strong> {esc(str(v))}</li>" for k, v in env.items())
        env_html = f"<ul>{env_items}</ul>"
    else:
        env_html = "<p>No environment data provided.</p>"

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Test Coverage Report - {esc(app)} v{esc(release)}</title>
  <style>
    * {{ box-sizing: border-box; margin: 0; padding: 0; }}
    body {{
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      background: #f5f5f5;
      color: #333;
      line-height: 1.6;
      padding: 20px;
    }}
    .container {{ max-width: 1000px; margin: 0 auto; }}
    h1 {{ font-size: 1.8em; margin-bottom: 4px; }}
    h2 {{ font-size: 1.3em; margin: 24px 0 12px 0; border-bottom: 2px solid #ddd; padding-bottom: 4px; }}
    .meta {{ color: #666; font-size: 0.9em; margin-bottom: 20px; }}

    /* Summary cards */
    .summary-cards {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: 12px;
      margin-bottom: 24px;
    }}
    .card {{
      background: #fff;
      border-radius: 8px;
      padding: 16px;
      text-align: center;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    }}
    .card .number {{ font-size: 2em; font-weight: bold; }}
    .card .label {{ font-size: 0.85em; color: #666; text-transform: uppercase; letter-spacing: 0.5px; }}
    .card-pass .number {{ color: #2e7d32; }}
    .card-fail .number {{ color: #c62828; }}
    .card-skip .number {{ color: #f57f17; }}
    .card-total .number {{ color: #1565c0; }}

    /* Status banner */
    .status-banner {{
      padding: 12px 20px;
      border-radius: 8px;
      font-weight: bold;
      text-align: center;
      font-size: 1.1em;
      margin-bottom: 24px;
    }}
    .status-pass {{ background: #e8f5e9; color: #2e7d32; border: 1px solid #a5d6a7; }}
    .status-warn {{ background: #fff8e1; color: #f57f17; border: 1px solid #ffe082; }}
    .status-fail {{ background: #ffebee; color: #c62828; border: 1px solid #ef9a9a; }}

    /* Progress bars */
    .category-row {{
      display: grid;
      grid-template-columns: 200px 1fr 180px;
      align-items: center;
      gap: 12px;
      margin-bottom: 8px;
    }}
    .category-label {{ font-weight: 600; font-size: 0.9em; text-align: right; }}
    .bar-container {{
      display: flex;
      height: 24px;
      border-radius: 4px;
      overflow: hidden;
      background: #eee;
    }}
    .bar-segment {{ height: 100%; }}
    .bar-pass {{ background: #4caf50; }}
    .bar-fail {{ background: #e53935; }}
    .bar-skip {{ background: #ffc107; }}
    .category-stats {{ font-size: 0.85em; color: #555; }}

    /* Legend */
    .legend {{
      display: flex;
      gap: 20px;
      margin: 12px 0 20px 0;
      font-size: 0.85em;
    }}
    .legend-item {{ display: flex; align-items: center; gap: 6px; }}
    .legend-swatch {{
      width: 14px;
      height: 14px;
      border-radius: 3px;
      display: inline-block;
    }}
    .swatch-pass {{ background: #4caf50; }}
    .swatch-fail {{ background: #e53935; }}
    .swatch-skip {{ background: #ffc107; }}

    /* Failure cards */
    .failure-card {{
      background: #fff;
      border-left: 4px solid #e53935;
      border-radius: 8px;
      padding: 16px 20px;
      margin-bottom: 16px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    }}
    .failure-header {{
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 6px;
    }}
    .failure-category {{
      color: #666;
      font-size: 0.85em;
      margin-left: auto;
    }}
    .failure-title {{
      font-size: 1.05em;
      font-weight: 600;
      margin-bottom: 8px;
    }}
    .failure-notes {{
      background: #fff3f3;
      border-radius: 4px;
      padding: 8px 12px;
      margin-bottom: 10px;
      font-size: 0.9em;
      color: #b71c1c;
    }}
    .failure-remediation {{
      background: #f5f5f5;
      border-radius: 4px;
      padding: 10px 14px;
    }}
    .remediation-label {{
      font-weight: 600;
      font-size: 0.9em;
      margin-bottom: 4px;
      color: #333;
    }}
    .remediation-text {{
      font-size: 0.88em;
      color: #444;
      line-height: 1.5;
    }}

    /* Tables */
    table {{
      width: 100%;
      border-collapse: collapse;
      background: #fff;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      margin-bottom: 20px;
    }}
    th, td {{ padding: 10px 14px; text-align: left; border-bottom: 1px solid #eee; }}
    th {{ background: #fafafa; font-weight: 600; font-size: 0.85em; text-transform: uppercase; letter-spacing: 0.3px; }}
    tr:last-child td {{ border-bottom: none; }}
    code {{ background: #f0f0f0; padding: 2px 6px; border-radius: 3px; font-size: 0.9em; }}

    /* Priority badges */
    .priority-critical {{ color: #c62828; font-weight: bold; }}
    .priority-high {{ color: #e65100; font-weight: 600; }}
    .priority-medium {{ color: #f57f17; }}
    .priority-low {{ color: #558b2f; }}

    /* Environment section */
    .env-section {{ background: #fff; padding: 16px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
    .env-section ul {{ list-style: none; padding: 0; }}
    .env-section li {{ padding: 4px 0; }}

    .footer {{ text-align: center; color: #999; font-size: 0.8em; margin-top: 30px; padding-top: 16px; border-top: 1px solid #ddd; }}
  </style>
</head>
<body>
  <div class="container">
    <h1>Test Coverage Report</h1>
    <div class="meta">
      {esc(app)} &mdash; v{esc(release)} &mdash; {esc(test_date)} &mdash; Generated {esc(generated)}
    </div>

    <div class="status-banner {status_class}">{status_text}</div>

    <div class="summary-cards">
      <div class="card card-total">
        <div class="number">{analysis['total']}</div>
        <div class="label">Total Tests</div>
      </div>
      <div class="card card-pass">
        <div class="number">{analysis['pass_count']}</div>
        <div class="label">Passed</div>
      </div>
      <div class="card card-fail">
        <div class="number">{analysis['fail_count']}</div>
        <div class="label">Failed</div>
      </div>
      <div class="card card-skip">
        <div class="number">{analysis['skip_count']}</div>
        <div class="label">Skipped</div>
      </div>
      <div class="card">
        <div class="number">{analysis['pass_rate']}%</div>
        <div class="label">Pass Rate</div>
      </div>
    </div>

    <h2>Coverage by Category</h2>
    <div class="legend">
      <div class="legend-item"><span class="legend-swatch swatch-pass"></span> Pass</div>
      <div class="legend-item"><span class="legend-swatch swatch-fail"></span> Fail</div>
      <div class="legend-item"><span class="legend-swatch swatch-skip"></span> Skip</div>
    </div>
    {category_bars}

    {"<h2>Failing Tests - Assessment &amp; Remediation</h2>" if analysis["failures"] else ""}
    {failure_cards if analysis["failures"] else ""}

    {"<h2>Skipped Tests</h2>" if analysis["skipped"] else ""}
    {"<table><tr><th>ID</th><th>Title</th><th>Category</th><th>Notes</th></tr>" + skip_rows + "</table>" if analysis["skipped"] else ""}

    {"<h2>Passed Tests</h2>" if analysis["passed"] else ""}
    {"<table><tr><th>ID</th><th>Title</th><th>Priority</th><th>Category</th></tr>" + pass_rows + "</table>" if analysis["passed"] else ""}

    <h2>Test Environment</h2>
    <div class="env-section">
      {env_html}
    </div>

    <div class="footer">
      zSpace QA Automation POC &mdash; Coverage Report &mdash; {generated}
    </div>
  </div>
</body>
</html>
"""

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(html)

    return output_path


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Generate a test coverage dashboard from test results.",
        epilog="Example: python coverage_report.py --output html",
    )
    parser.add_argument(
        "--results-file",
        default=None,
        help="Path to a JSON or CSV file with test results (default: embedded sample data)",
    )
    parser.add_argument(
        "--output",
        choices=["html", "markdown"],
        default="html",
        help="Output format: html or markdown (default: html)",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory to write output files (default: output/reports/ in the project root)",
    )
    parser.add_argument(
        "--config",
        default=None,
        help="Path to a JSON config file (e.g., configs/studio-a3.json). Sets the app name for the report.",
    )
    parser.add_argument("--verbose", action="store_true", help="Enable verbose logging")
    parser.add_argument("--quiet", action="store_true", help="Suppress informational output")

    args = parser.parse_args()

    level = logging.DEBUG if args.verbose else logging.WARNING if args.quiet else logging.INFO
    # Matches setup_logging() in qa_common.py
    logging.basicConfig(level=level, format="%(message)s",
                        handlers=[logging.StreamHandler(sys.stdout)])

    # Default output dir to output/reports/ relative to the project root.
    if args.output_dir is None:
        project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        args.output_dir = os.path.join(project_root, "output", "reports")

    # Load config if provided — overrides app_name in the results data.
    config_app_name = None
    if args.config:
        with open(args.config, "r", encoding="utf-8") as f:
            config = json.load(f)
        config_app_name = config.get("app_name")
        logger.info("  Loaded config: %s (app: %s)", args.config, config_app_name)

    logger.info("=== zSpace QA Coverage Report Generator ===")
    logger.info("")

    # Load test results.
    logger.info("Loading results...")
    data = load_results(args.results_file)
    results = data.get("results", [])

    if not results:
        logger.error("  Error: No test results found.")
        sys.exit(1)

    # Override app_name from config if provided.
    if config_app_name:
        data["app_name"] = config_app_name

    logger.info("  %d test results loaded for %s", len(results), data.get("app_name", "N/A"))
    logger.info("")

    # Analyze.
    analysis = analyze_results(results)

    # Generate the requested output format.
    os.makedirs(args.output_dir, exist_ok=True)
    release = data.get("release_version", "unknown")
    app_slug = data.get("app_name", "unknown").replace("'", "").replace(" ", "-").lower()
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    if args.output == "markdown":
        md = generate_markdown(data, analysis)
        output_path = os.path.join(args.output_dir, f"coverage_report_{app_slug}_{release}_{timestamp}.md")
        with open(output_path, "w", encoding="utf-8") as f:
            f.write(md)
        logger.info("Markdown report written to: %s", output_path)
        # Also print to stdout for terminal viewing.
        logger.info("")
        logger.info(md)

    elif args.output == "html":
        output_path = os.path.join(args.output_dir, f"coverage_report_{app_slug}_{release}_{timestamp}.html")
        generate_html(data, analysis, output_path)
        logger.info("HTML report written to: %s", output_path)

    # Summary line.
    logger.info("")
    logger.info("Summary: %d passed, %d failed, %d skipped out of %d total (%s%% pass rate)",
                analysis['pass_count'], analysis['fail_count'],
                analysis['skip_count'], analysis['total'], analysis['pass_rate'])

    # Exit code: 0 if no failures, 1 if any failures.
    if analysis["fail_count"] > 0:
        logger.info("\nExit code 1: %d test failure(s) detected.", analysis['fail_count'])
        sys.exit(1)
    else:
        logger.info("\nExit code 0: No failures.")
        sys.exit(0)


if __name__ == "__main__":
    main()

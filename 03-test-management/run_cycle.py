#!/usr/bin/env python3
"""
run_cycle.py - Generate a complete test cycle for a zSpace application release.

This script produces three outputs for a given release:
  1. Jira-importable CSV  - for creating test-execution tickets in Jira
  2. Markdown checklist    - for manual QA sign-off during test sessions
  3. TestRail XML import   - for importing test cases into TestRail

Usage:
    python run_cycle.py --release-version 2.1.0
    python run_cycle.py --release-version 2.1.0 --app-name "Franklins Lab A3" --output-dir ./output

The script can load test definitions from a JSON directory (--test-cases-dir) or
fall back to embedded sample data so the POC works standalone without external files.

Part of Prototype #3 - QA Automation POC for zSpace Unity AR/VR applications.
"""

import argparse
import csv
import json
import os
import sys
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path
from xml.dom import minidom


# ---------------------------------------------------------------------------
# Embedded sample test data
# ---------------------------------------------------------------------------
# This data mirrors a subset of the 111 test cases from the
# jdonnelly-zspace/3rdParty_QA_Requirements repo. It allows the script to
# produce realistic output without needing the external repository.
# ---------------------------------------------------------------------------

SAMPLE_TEST_DATA = {
    "Installation & Licensing": [
        {
            "id": "INST-001",
            "testrail_id": "C10001",
            "title": "Fresh install on supported hardware",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Ensure the target machine meets minimum hardware requirements",
                "Download the installer from the official distribution channel",
                "Run the installer with default settings",
                "Complete the installation wizard",
                "Launch the application",
            ],
            "expected_results": [
                "Hardware requirements are verified",
                "Installer downloads without errors",
                "Installation proceeds without errors or warnings",
                "Wizard completes and shows success message",
                "Application launches to the main menu within 30 seconds",
            ],
        },
        {
            "id": "INST-002",
            "testrail_id": "C10002",
            "title": "Upgrade from previous version preserves settings",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Install the previous release version",
                "Configure custom application settings",
                "Run the upgrade installer for the new version",
                "Launch the upgraded application",
                "Verify that custom settings are preserved",
            ],
            "expected_results": [
                "Previous version installs successfully",
                "Settings are saved to the expected location",
                "Upgrade completes without data loss warnings",
                "Application launches without re-configuration prompts",
                "All custom settings match pre-upgrade values",
            ],
        },
        {
            "id": "INST-003",
            "testrail_id": "C10003",
            "title": "Uninstall removes all application files",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Install the application",
                "Note the installation directory and registry entries",
                "Run the uninstaller from Add/Remove Programs",
                "Verify the installation directory is removed",
                "Check for residual registry entries",
            ],
            "expected_results": [
                "Application installs successfully",
                "Installation artifacts are documented",
                "Uninstaller runs to completion",
                "Installation directory is deleted or empty",
                "No orphaned registry entries remain",
            ],
        },
        {
            "id": "INST-004",
            "testrail_id": "C10004",
            "title": "License activation with valid key",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Launch the application without a license",
                "Navigate to the license activation dialog",
                "Enter a valid license key",
                "Click Activate",
                "Verify full application functionality is unlocked",
            ],
            "expected_results": [
                "Application shows trial/unlicensed state",
                "License dialog is accessible",
                "Key is accepted without format errors",
                "Activation succeeds and confirmation is shown",
                "All features are available after activation",
            ],
        },
        {
            "id": "INST-005",
            "testrail_id": "C10005",
            "title": "License activation with invalid key shows error",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Navigate to the license activation dialog",
                "Enter an invalid license key",
                "Click Activate",
                "Verify appropriate error message",
            ],
            "expected_results": [
                "Application launches",
                "License dialog is displayed",
                "Invalid key is entered",
                "Activation fails gracefully",
                "Clear error message indicates the key is invalid",
            ],
        },
        {
            "id": "INST-006",
            "testrail_id": "C10006",
            "title": "Offline license activation workflow",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Disconnect the machine from the network",
                "Launch the application",
                "Attempt license activation",
                "Follow the offline activation instructions",
                "Verify license is activated",
            ],
            "expected_results": [
                "Machine is offline",
                "Application launches in offline mode",
                "Offline activation option is presented",
                "Offline workflow completes with manual code entry",
                "License activates successfully",
            ],
        },
        {
            "id": "INST-007",
            "testrail_id": "C10007",
            "title": "License deactivation and transfer",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Activate a license on Machine A",
                "Deactivate the license on Machine A",
                "Activate the same license on Machine B",
                "Verify application works on Machine B",
            ],
            "expected_results": [
                "License is active on Machine A",
                "License is released from Machine A",
                "License activates on Machine B",
                "Full functionality available on Machine B",
            ],
        },
        {
            "id": "INST-008",
            "testrail_id": "C10008",
            "title": "Install on minimum spec hardware",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Prepare a machine with minimum supported specs",
                "Run the installer",
                "Launch the application",
                "Perform basic interaction tests",
            ],
            "expected_results": [
                "Machine meets minimum requirements",
                "Installation completes successfully",
                "Application launches within acceptable time",
                "Basic interactions are responsive",
            ],
        },
        {
            "id": "INST-009",
            "testrail_id": "C10009",
            "title": "Silent install via command line",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Open an elevated command prompt",
                "Run the installer with /S or --silent flag",
                "Wait for the process to exit",
                "Verify the application is installed",
            ],
            "expected_results": [
                "Command prompt is running as admin",
                "Installer runs without GUI",
                "Process exits with code 0",
                "Application files are present in the target directory",
            ],
        },
        {
            "id": "INST-010",
            "testrail_id": "C10010",
            "title": "Install path with spaces and special characters",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Choose a custom install path containing spaces and special characters",
                "Run the installer to that path",
                "Launch the application from the custom path",
            ],
            "expected_results": [
                "Custom path is accepted by the installer",
                "Installation completes without path-related errors",
                "Application launches correctly from the custom path",
            ],
        },
    ],
    "Stereoscopy & Head Tracking": [
        {
            "id": "STEREO-001",
            "testrail_id": "C10020",
            "title": "Stereo rendering activates on supported display",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Launch the application on a zSpace display",
                "Verify that stereoscopic rendering is enabled by default",
                "Observe the 3D depth effect on displayed objects",
            ],
            "expected_results": [
                "Application launches successfully on zSpace display",
                "Stereo rendering is active (left/right eye images differ)",
                "Objects appear with correct depth separation",
            ],
        },
        {
            "id": "STEREO-002",
            "testrail_id": "C10021",
            "title": "Head tracking responds to user movement",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Launch the application on a zSpace display",
                "Move your head left and right",
                "Move your head forward and back",
                "Verify the perspective shifts accordingly",
            ],
            "expected_results": [
                "Application is running with head tracking",
                "Perspective shifts laterally with head movement",
                "Perspective changes with depth movement",
                "Tracking is smooth with no visible jitter",
            ],
        },
        {
            "id": "STEREO-003",
            "testrail_id": "C10022",
            "title": "Stereo comfort - no excessive ghosting",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Navigate to a scene with high-contrast objects",
                "Observe for crosstalk or ghosting artifacts",
            ],
            "expected_results": [
                "Application renders the scene",
                "High-contrast objects are displayed",
                "Ghosting is within acceptable limits (no double images)",
            ],
        },
        {
            "id": "STEREO-004",
            "testrail_id": "C10023",
            "title": "IPD adjustment functions correctly",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Open application settings",
                "Locate the IPD (inter-pupillary distance) control",
                "Adjust the IPD slider",
                "Verify stereo separation changes in real-time",
            ],
            "expected_results": [
                "Settings menu is accessible",
                "IPD control is present and labeled",
                "Slider moves smoothly",
                "Stereo separation updates immediately",
            ],
        },
        {
            "id": "STEREO-005",
            "testrail_id": "C10024",
            "title": "Head tracking recovery after occlusion",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Launch the application with head tracking active",
                "Block the tracking camera briefly with your hand",
                "Remove the obstruction",
                "Verify tracking resumes",
            ],
            "expected_results": [
                "Head tracking is active",
                "Tracking pauses or holds last known position",
                "Obstruction is removed",
                "Tracking resumes smoothly within 1 second",
            ],
        },
        {
            "id": "STEREO-006",
            "testrail_id": "C10025",
            "title": "Stereo rendering at native refresh rate",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Open a performance monitoring overlay or tool",
                "Verify frame rate matches display refresh rate",
            ],
            "expected_results": [
                "Application is running",
                "Performance data is visible",
                "Frame rate is stable at or near the native refresh rate",
            ],
        },
    ],
    "Input Handling": [
        {
            "id": "INPUT-001",
            "testrail_id": "C10040",
            "title": "Stylus primary button interaction",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Point the zSpace stylus at an interactive object",
                "Press the primary stylus button",
            ],
            "expected_results": [
                "Application is running",
                "Object highlights or shows hover feedback",
                "Object is selected or activated as expected",
            ],
        },
        {
            "id": "INPUT-002",
            "testrail_id": "C10041",
            "title": "Stylus secondary button interaction",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Point the stylus at an interactive object",
                "Press the secondary (middle) stylus button",
            ],
            "expected_results": [
                "Application is running",
                "Object shows hover feedback",
                "Context menu or secondary action is triggered",
            ],
        },
        {
            "id": "INPUT-003",
            "testrail_id": "C10042",
            "title": "Stylus 6DOF tracking accuracy",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Move the stylus slowly through the tracking volume",
                "Verify the on-screen cursor follows the physical stylus position",
            ],
            "expected_results": [
                "Application is running",
                "Cursor tracks stylus in all 6 degrees of freedom",
                "Tracking is smooth with sub-millimeter accuracy",
            ],
        },
        {
            "id": "INPUT-004",
            "testrail_id": "C10043",
            "title": "Mouse fallback when stylus disconnected",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Disconnect or turn off the stylus",
                "Attempt to interact using the mouse",
            ],
            "expected_results": [
                "Application is running with stylus",
                "Application detects stylus disconnection",
                "Mouse input is accepted as fallback",
            ],
        },
    ],
    "zView": [
        {
            "id": "ZVIEW-001",
            "testrail_id": "C10050",
            "title": "zView presenter mode launches",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Connect a secondary display or device for zView",
                "Launch the application",
                "Activate zView presenter mode from the menu",
                "Verify the secondary display shows the zView output",
            ],
            "expected_results": [
                "Secondary display is connected and detected",
                "Application launches normally",
                "zView mode activates without errors",
                "Secondary display mirrors or augments the 3D view",
            ],
        },
        {
            "id": "ZVIEW-002",
            "testrail_id": "C10051",
            "title": "zView video stream quality",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Activate zView presenter mode",
                "Observe the video stream on the secondary display",
                "Check for frame drops or artifacts",
            ],
            "expected_results": [
                "zView is active",
                "Video stream is clear and synchronized",
                "No significant frame drops or visual artifacts",
            ],
        },
        {
            "id": "ZVIEW-003",
            "testrail_id": "C10052",
            "title": "zView augmented reality overlay",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Activate zView in AR mode",
                "Verify the camera feed shows on the secondary display",
                "Check that virtual objects are overlaid on the camera feed",
            ],
            "expected_results": [
                "zView AR mode is active",
                "Camera feed is displayed",
                "Virtual objects align correctly with the physical scene",
            ],
        },
        {
            "id": "ZVIEW-004",
            "testrail_id": "C10053",
            "title": "zView disconnect and reconnect",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Activate zView",
                "Disconnect the secondary display",
                "Reconnect the secondary display",
                "Verify zView resumes",
            ],
            "expected_results": [
                "zView is active",
                "Application handles disconnection gracefully",
                "Secondary display is re-detected",
                "zView resumes without restarting the application",
            ],
        },
    ],
    "Hardware & Display": [
        {
            "id": "HW-001",
            "testrail_id": "C10060",
            "title": "Application renders on all supported GPU models",
            "priority": "Critical",
            "is_common": True,
            "steps": [
                "Prepare machines with each supported GPU model",
                "Install and launch the application on each machine",
                "Verify rendering is correct on each",
            ],
            "expected_results": [
                "Machines are prepared with different GPUs",
                "Application launches on all machines",
                "Rendering is correct with no GPU-specific artifacts",
            ],
        },
        {
            "id": "HW-002",
            "testrail_id": "C10061",
            "title": "Display resolution auto-detection",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Connect the zSpace display",
                "Launch the application",
                "Verify the application uses the native display resolution",
            ],
            "expected_results": [
                "Display is connected",
                "Application launches",
                "Render resolution matches native display resolution",
            ],
        },
        {
            "id": "HW-003",
            "testrail_id": "C10062",
            "title": "Multi-monitor configuration handling",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Connect the zSpace display plus one or more additional monitors",
                "Launch the application",
                "Verify the application launches on the correct display",
            ],
            "expected_results": [
                "Multiple monitors are connected",
                "Application launches",
                "Application renders on the zSpace display, not a secondary monitor",
            ],
        },
        {
            "id": "HW-004",
            "testrail_id": "C10063",
            "title": "GPU driver version compatibility",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Install the minimum supported GPU driver version",
                "Launch the application",
                "Verify rendering and stereoscopy function correctly",
            ],
            "expected_results": [
                "Minimum driver version is installed",
                "Application launches",
                "All visual features work correctly",
            ],
        },
        {
            "id": "HW-005",
            "testrail_id": "C10064",
            "title": "Thermal throttling behavior under load",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Launch the application",
                "Run a demanding scene for 30+ minutes",
                "Monitor CPU/GPU temperatures",
                "Check for performance degradation",
            ],
            "expected_results": [
                "Application is running",
                "Scene runs continuously",
                "Temperatures stay within safe operating range",
                "Frame rate remains stable or degrades gracefully",
            ],
        },
    ],
    "Platform Features": [
        {
            "id": "PLAT-001",
            "testrail_id": "C10070",
            "title": "Windows Defender compatibility",
            "priority": "High",
            "is_common": True,
            "steps": [
                "Ensure Windows Defender is enabled with default settings",
                "Install the application",
                "Launch the application",
                "Verify no false-positive detections",
            ],
            "expected_results": [
                "Windows Defender is active",
                "Installation is not blocked",
                "Application launches without SmartScreen warnings",
                "No quarantine or threat alerts for application files",
            ],
        },
        {
            "id": "PLAT-002",
            "testrail_id": "C10071",
            "title": "Windows Update does not break application",
            "priority": "Medium",
            "is_common": True,
            "steps": [
                "Install the application",
                "Run Windows Update and install all available updates",
                "Restart the machine",
                "Launch the application",
                "Verify core functionality",
            ],
            "expected_results": [
                "Application is installed",
                "Updates install successfully",
                "Machine restarts normally",
                "Application launches",
                "Core features work as expected",
            ],
        },
    ],
}


# ---------------------------------------------------------------------------
# Test data loading
# ---------------------------------------------------------------------------

def load_test_cases(test_cases_dir: str | None) -> dict:
    """
    Load test case definitions from a JSON directory, or fall back to
    the embedded sample data.

    The expected directory layout is one JSON file per category, each
    containing a list of test-case objects.

    Args:
        test_cases_dir: Path to a directory of JSON test-case files, or None.

    Returns:
        dict mapping category name -> list of test-case dicts.
    """
    if test_cases_dir and os.path.isdir(test_cases_dir):
        data = {}
        for filename in sorted(os.listdir(test_cases_dir)):
            if not filename.endswith(".json"):
                continue
            filepath = os.path.join(test_cases_dir, filename)
            try:
                with open(filepath, "r", encoding="utf-8") as f:
                    contents = json.load(f)
                # Support both a plain list and a dict with a "test_cases" key.
                if isinstance(contents, list):
                    cases = contents
                elif isinstance(contents, dict) and "test_cases" in contents:
                    cases = contents["test_cases"]
                else:
                    continue
                if cases:
                    category = cases[0].get("category", filename.replace(".json", ""))
                    data[category] = cases
            except (json.JSONDecodeError, KeyError):
                print(f"  Warning: Could not parse {filepath}, skipping.")
        if data:
            print(f"  Loaded {sum(len(v) for v in data.values())} test cases from {test_cases_dir}")
            return data

    # Fall back to embedded sample data.
    print("  Using embedded sample test data (standalone POC mode)")
    return SAMPLE_TEST_DATA


# ---------------------------------------------------------------------------
# Output generators
# ---------------------------------------------------------------------------

def generate_jira_csv(test_data: dict, release: str, app: str, output_dir: str) -> str:
    """
    Generate a Jira-importable CSV file with one row per test case.

    Columns: Summary, Description, Priority, Labels, Component, Test Steps, Expected Results

    Args:
        test_data: dict of category -> test cases.
        release: Release version string.
        app: Application name.
        output_dir: Directory to write the CSV into.

    Returns:
        Path to the generated CSV file.
    """
    filepath = os.path.join(output_dir, f"jira_test_cycle_{release}.csv")

    with open(filepath, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow([
            "Summary",
            "Description",
            "Priority",
            "Labels",
            "Component",
            "Test Steps",
            "Expected Results",
        ])

        for category, cases in test_data.items():
            for tc in cases:
                # Build a numbered step list for the Test Steps column.
                steps_text = "\n".join(
                    f"{i+1}. {step}" for i, step in enumerate(tc["steps"])
                )
                expected_text = "\n".join(
                    f"{i+1}. {exp}" for i, exp in enumerate(tc["expected_results"])
                )

                summary = f"[{release}] {tc['title']}"
                description = (
                    f"Test Case: {tc['id']}\n"
                    f"Application: {app}\n"
                    f"Release: {release}\n"
                    f"Category: {category}\n"
                    f"Common Test: {'Yes' if tc.get('is_common', False) else 'No'}"
                )
                labels = f"qa-automation test-cycle v{release}"
                component = category

                writer.writerow([
                    summary,
                    description,
                    tc.get("priority", "Medium"),
                    labels,
                    component,
                    steps_text,
                    expected_text,
                ])

    return filepath


def generate_markdown_checklist(test_data: dict, release: str, app: str, output_dir: str) -> str:
    """
    Generate a markdown checklist for manual QA sign-off.

    The checklist is organized by category with checkboxes for each test.

    Args:
        test_data: dict of category -> test cases.
        release: Release version string.
        app: Application name.
        output_dir: Directory to write the markdown into.

    Returns:
        Path to the generated markdown file.
    """
    filepath = os.path.join(output_dir, f"qa_checklist_{release}.md")

    lines = []
    lines.append(f"# QA Test Cycle Checklist")
    lines.append(f"")
    lines.append(f"**Application:** {app}")
    lines.append(f"**Release:** {release}")
    lines.append(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M')}")
    lines.append(f"**Total Test Cases:** {sum(len(v) for v in test_data.values())}")
    lines.append(f"")
    lines.append(f"---")
    lines.append(f"")
    lines.append(f"## Instructions")
    lines.append(f"")
    lines.append(f"Mark each test case with its result:")
    lines.append(f"- [x] = Pass")
    lines.append(f"- [ ] = Not yet executed")
    lines.append(f"- Add `FAIL` or `SKIP` next to the checkbox if applicable")
    lines.append(f"")

    for category, cases in test_data.items():
        lines.append(f"---")
        lines.append(f"")
        lines.append(f"## {category} ({len(cases)} tests)")
        lines.append(f"")
        for tc in cases:
            priority_tag = f"[{tc.get('priority', 'Medium')}]"
            lines.append(f"- [ ] **{tc['id']}** - {tc['title']} {priority_tag}")
        lines.append(f"")

    lines.append(f"---")
    lines.append(f"")
    lines.append(f"## Sign-Off")
    lines.append(f"")
    lines.append(f"| Role | Name | Date | Signature |")
    lines.append(f"|------|------|------|-----------|")
    lines.append(f"| QA Lead | | | |")
    lines.append(f"| Dev Lead | | | |")
    lines.append(f"| Product Owner | | | |")
    lines.append(f"")

    with open(filepath, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    return filepath


def generate_testrail_xml(test_data: dict, release: str, app: str, output_dir: str) -> str:
    """
    Generate a TestRail-compatible XML import file.

    The XML follows the TestRail import format with sections (categories)
    containing individual test cases.

    Args:
        test_data: dict of category -> test cases.
        release: Release version string.
        app: Application name.
        output_dir: Directory to write the XML into.

    Returns:
        Path to the generated XML file.
    """
    filepath = os.path.join(output_dir, f"testrail_import_{release}.xml")

    # Build the XML tree. TestRail expects <suite> -> <sections> -> <section> -> <cases>.
    suite = ET.Element("suite")
    suite_name = ET.SubElement(suite, "name")
    suite_name.text = f"{app} - v{release} Test Cycle"
    suite_desc = ET.SubElement(suite, "description")
    suite_desc.text = f"Auto-generated test cycle for {app} release {release}"

    sections_elem = ET.SubElement(suite, "sections")

    for category, cases in test_data.items():
        section = ET.SubElement(sections_elem, "section")
        section_name = ET.SubElement(section, "name")
        section_name.text = category

        cases_elem = ET.SubElement(section, "cases")

        for tc in cases:
            case = ET.SubElement(cases_elem, "case")

            # Test case ID (for reference).
            case_id = ET.SubElement(case, "id")
            case_id.text = tc.get("testrail_id", tc["id"])

            title = ET.SubElement(case, "title")
            title.text = tc["title"]

            # Map priority strings to TestRail numeric values.
            priority_map = {"Critical": "4", "High": "3", "Medium": "2", "Low": "1"}
            priority = ET.SubElement(case, "priority")
            priority.text = priority_map.get(tc.get("priority", "Medium"), "2")

            # Custom fields for steps and expected results.
            custom = ET.SubElement(case, "custom")

            steps_separated = ET.SubElement(custom, "steps_separated")
            for i, (step, expected) in enumerate(
                zip(tc["steps"], tc["expected_results"])
            ):
                step_elem = ET.SubElement(steps_separated, "step")
                step_index = ET.SubElement(step_elem, "index")
                step_index.text = str(i + 1)
                step_content = ET.SubElement(step_elem, "content")
                step_content.text = step
                step_expected = ET.SubElement(step_elem, "expected")
                step_expected.text = expected

    # Pretty-print the XML.
    rough_string = ET.tostring(suite, encoding="unicode")
    reparsed = minidom.parseString(rough_string)
    pretty_xml = reparsed.toprettyxml(indent="  ", encoding=None)

    # Remove the XML declaration line that minidom adds (TestRail prefers without).
    lines = pretty_xml.split("\n")
    if lines and lines[0].startswith("<?xml"):
        lines = lines[1:]
    pretty_xml = "\n".join(lines)

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(pretty_xml)

    return filepath


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Generate a complete test cycle for a zSpace application release.",
        epilog="Example: python run_cycle.py --release-version 2.1.0",
    )
    parser.add_argument(
        "--release-version",
        required=True,
        help="The release version string (e.g., 2.1.0)",
    )
    parser.add_argument(
        "--app-name",
        default="Franklin's Lab A3",
        help="Application name (default: Franklin's Lab A3)",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory to write output files (default: ./output/<version>)",
    )
    parser.add_argument(
        "--test-cases-dir",
        default=None,
        help="Path to directory containing test-case JSON files (default: embedded sample data)",
    )

    args = parser.parse_args()

    # Determine output directory.
    if args.output_dir:
        output_dir = args.output_dir
    else:
        output_dir = os.path.join(".", "output", args.release_version)

    os.makedirs(output_dir, exist_ok=True)

    print(f"=== zSpace QA Test Cycle Generator ===")
    print(f"  Application: {args.app_name}")
    print(f"  Release:     {args.release_version}")
    print(f"  Output:      {os.path.abspath(output_dir)}")
    print()

    # Load test case definitions.
    print("Loading test cases...")
    test_data = load_test_cases(args.test_cases_dir)
    total_cases = sum(len(cases) for cases in test_data.values())
    total_categories = len(test_data)
    print()

    # Generate all three output files.
    print("Generating outputs...")

    csv_path = generate_jira_csv(test_data, args.release_version, args.app_name, output_dir)
    print(f"  Jira CSV:        {csv_path}")

    md_path = generate_markdown_checklist(test_data, args.release_version, args.app_name, output_dir)
    print(f"  QA Checklist:    {md_path}")

    xml_path = generate_testrail_xml(test_data, args.release_version, args.app_name, output_dir)
    print(f"  TestRail XML:    {xml_path}")

    print()
    print(f"Generated test cycle for {args.app_name} v{args.release_version}: "
          f"{total_cases} test cases across {total_categories} categories")


if __name__ == "__main__":
    main()

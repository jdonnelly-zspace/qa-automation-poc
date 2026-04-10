#!/usr/bin/env python3
"""
validate_build.py - Post-Build Validation for Franklin's Lab A3 Unity Builds
=============================================================================

This script validates a completed Unity build to catch problems before the
build is packaged, deployed, or handed off for testing.

It checks:
  - Required files exist (EXE, DLLs, data folders for Win64; index.html for WebGL)
  - File sizes are reasonable (not zero, not suspiciously small)
  - Code signing is valid on the Windows EXE (GlobalSign certificate)
  - Unity player log does not contain errors

Usage:
    python validate_build.py --build-dir "C:/Builds/FranklinsLabA3" --build-type win64
    python validate_build.py --build-dir "C:/Builds/WebGL" --build-type webgl

Exit Codes:
    0 = All checks passed
    1 = One or more checks failed (build should not be shipped)

Designed for integration into Jenkins CI/CD pipelines.
"""

import argparse
import glob
import json
import os
import subprocess
import sys


# ---------------------------------------------------------------------------
# Configuration - adjust these values if the project structure changes
# ---------------------------------------------------------------------------

# Default app name (overridden by --app-name or --config CLI args).
APP_NAME = "FranklinsLabA3"

# Minimum acceptable file sizes (in bytes).  A valid Unity build will always
# be larger than these thresholds; anything smaller almost certainly means the
# build failed silently.
MIN_EXE_SIZE_BYTES = 1_000_000        # 1 MB - the Unity player alone is larger
MIN_DLL_SIZE_BYTES = 1_000            # 1 KB
MIN_WEBGL_INDEX_SIZE_BYTES = 1_000    # 1 KB
MIN_WEBGL_DATA_SIZE_BYTES = 100_000   # 100 KB - compressed data should be significant

# DLLs that must be present in a Win64 build (Unity 2019.4 ships these).
REQUIRED_WIN64_DLLS = [
    "UnityPlayer.dll",
    "WinPixEventRuntime.dll",
]

# Patterns for Unity log files to scan for errors.
# Unity writes logs to the build output or to the user's AppData folder.
UNITY_LOG_PATTERNS = [
    "output_log.txt",
    "Player.log",
]

# Lines in the Unity log that indicate a real problem.
LOG_ERROR_KEYWORDS = [
    "Fatal Error",
    "Crash!!!",
    "NullReferenceException",
    "DllNotFoundException",
    "Could not load",
    "Build failed",
    "Script compilation error",
]


# ---------------------------------------------------------------------------
# Result tracking
# ---------------------------------------------------------------------------

class ValidationResult:
    """Collects pass/fail results and prints a summary report."""

    def __init__(self):
        self.checks = []  # list of (name, passed: bool, detail: str)

    def add(self, name, passed, detail=""):
        """Record the outcome of a single check."""
        self.checks.append((name, passed, detail))

    @property
    def all_passed(self):
        return all(passed for _, passed, _ in self.checks)

    def print_report(self):
        """Print a human-readable report to stdout."""
        print("")
        print("=" * 70)
        print("  BUILD VALIDATION REPORT")
        print("=" * 70)

        for name, passed, detail in self.checks:
            status = "PASS" if passed else "FAIL"
            icon = "[+]" if passed else "[X]"
            line = f"  {icon} {status}: {name}"
            if detail:
                line += f"  --  {detail}"
            print(line)

        print("-" * 70)
        total = len(self.checks)
        passed_count = sum(1 for _, p, _ in self.checks if p)
        failed_count = total - passed_count
        print(f"  Total: {total}  |  Passed: {passed_count}  |  Failed: {failed_count}")

        if self.all_passed:
            print("  OVERALL RESULT: PASS")
        else:
            print("  OVERALL RESULT: FAIL")
        print("=" * 70)
        print("")


# ---------------------------------------------------------------------------
# Validation checks - Windows 64-bit build
# ---------------------------------------------------------------------------

def validate_win64(build_dir, result):
    """Run all checks appropriate for a Win64 build."""

    # --- Check 1: Main executable exists ---
    exe_path = os.path.join(build_dir, f"{APP_NAME}.exe")
    exe_exists = os.path.isfile(exe_path)
    result.add(
        "Main executable exists",
        exe_exists,
        exe_path if exe_exists else f"MISSING: {exe_path}",
    )

    # --- Check 2: Executable file size is reasonable ---
    if exe_exists:
        exe_size = os.path.getsize(exe_path)
        size_ok = exe_size >= MIN_EXE_SIZE_BYTES
        result.add(
            "Executable size is reasonable",
            size_ok,
            f"{exe_size:,} bytes (minimum: {MIN_EXE_SIZE_BYTES:,})",
        )
    else:
        result.add("Executable size is reasonable", False, "Skipped - EXE not found")

    # --- Check 3: Data folder exists ---
    # Unity creates a folder named <AppName>_Data next to the executable.
    data_folder = os.path.join(build_dir, f"{APP_NAME}_Data")
    data_exists = os.path.isdir(data_folder)
    result.add(
        "Unity data folder exists",
        data_exists,
        data_folder if data_exists else f"MISSING: {data_folder}",
    )

    # --- Check 4: Data folder is not empty ---
    if data_exists:
        data_file_count = sum(len(files) for _, _, files in os.walk(data_folder))
        data_not_empty = data_file_count > 0
        result.add(
            "Unity data folder is not empty",
            data_not_empty,
            f"{data_file_count} files found",
        )
    else:
        result.add("Unity data folder is not empty", False, "Skipped - folder not found")

    # --- Check 5: Required DLLs are present and non-trivial ---
    for dll_name in REQUIRED_WIN64_DLLS:
        dll_path = os.path.join(build_dir, dll_name)
        dll_exists = os.path.isfile(dll_path)
        if dll_exists:
            dll_size = os.path.getsize(dll_path)
            dll_ok = dll_size >= MIN_DLL_SIZE_BYTES
            result.add(
                f"DLL present: {dll_name}",
                dll_ok,
                f"{dll_size:,} bytes",
            )
        else:
            result.add(f"DLL present: {dll_name}", False, f"MISSING: {dll_path}")

    # --- Check 6: Code signing verification ---
    # Uses PowerShell Get-AuthenticodeSignature which is available on all
    # modern Windows systems without needing the Windows SDK.
    if exe_exists:
        check_code_signing(exe_path, result)
    else:
        result.add("Code signing valid", False, "Skipped - EXE not found")

    # --- Check 7: Unity player log scan ---
    check_unity_log(build_dir, result)


def check_code_signing(exe_path, result):
    """Verify that the EXE has a valid Authenticode signature."""

    # Build a PowerShell command to check the signature status.
    # The StatusMessage field gives a human-readable explanation.
    ps_command = (
        f"$sig = Get-AuthenticodeSignature -FilePath '{exe_path}'; "
        f"Write-Output $sig.Status; "
        f"Write-Output '|||'; "
        f"Write-Output $sig.StatusMessage"
    )

    try:
        completed = subprocess.run(
            ["powershell", "-NoProfile", "-Command", ps_command],
            capture_output=True,
            text=True,
            timeout=30,
        )
        output = completed.stdout.strip()
        parts = output.split("|||")
        status = parts[0].strip() if len(parts) >= 1 else "Unknown"
        message = parts[1].strip() if len(parts) >= 2 else ""

        # "Valid" means the signature is present and trusted.
        signing_ok = (status == "Valid")
        detail = f"Status: {status}"
        if message:
            detail += f" - {message}"

        result.add("Code signing valid", signing_ok, detail)

    except FileNotFoundError:
        # PowerShell is not available (unlikely on Windows, but be safe).
        result.add(
            "Code signing valid",
            False,
            "PowerShell not found - cannot verify signature",
        )
    except subprocess.TimeoutExpired:
        result.add(
            "Code signing valid",
            False,
            "Timed out checking signature",
        )
    except Exception as exc:
        result.add(
            "Code signing valid",
            False,
            f"Error checking signature: {exc}",
        )


# ---------------------------------------------------------------------------
# Validation checks - WebGL build
# ---------------------------------------------------------------------------

def validate_webgl(build_dir, result):
    """Run all checks appropriate for a WebGL build."""

    # --- Check 1: index.html exists ---
    index_path = os.path.join(build_dir, "index.html")
    index_exists = os.path.isfile(index_path)
    result.add(
        "index.html exists",
        index_exists,
        index_path if index_exists else f"MISSING: {index_path}",
    )

    # --- Check 2: index.html is not trivially small ---
    if index_exists:
        index_size = os.path.getsize(index_path)
        size_ok = index_size >= MIN_WEBGL_INDEX_SIZE_BYTES
        result.add(
            "index.html size is reasonable",
            size_ok,
            f"{index_size:,} bytes (minimum: {MIN_WEBGL_INDEX_SIZE_BYTES:,})",
        )

    # --- Check 3: Build subfolder exists ---
    # Unity WebGL builds place compiled output in a "Build" subfolder.
    webgl_build_dir = os.path.join(build_dir, "Build")
    build_dir_exists = os.path.isdir(webgl_build_dir)
    result.add(
        "Build/ subfolder exists",
        build_dir_exists,
        webgl_build_dir if build_dir_exists else f"MISSING: {webgl_build_dir}",
    )

    # --- Check 4: Compiled data files exist in Build/ ---
    if build_dir_exists:
        # Unity 2019.4 WebGL builds produce .data, .wasm, .framework.js, and .loader.js files.
        # These may have .gz or .br compression extensions depending on build settings.
        data_files = glob.glob(os.path.join(webgl_build_dir, "*"))
        data_file_count = len(data_files)
        has_data_files = data_file_count >= 2  # At minimum: a data file and a framework file
        result.add(
            "Build/ contains compiled output",
            has_data_files,
            f"{data_file_count} files found in Build/",
        )

        # Check that at least one file is reasonably large (the .data or .wasm file).
        if data_files:
            largest_size = max(os.path.getsize(f) for f in data_files if os.path.isfile(f))
            large_enough = largest_size >= MIN_WEBGL_DATA_SIZE_BYTES
            result.add(
                "Compiled output has substantial size",
                large_enough,
                f"Largest file: {largest_size:,} bytes (minimum: {MIN_WEBGL_DATA_SIZE_BYTES:,})",
            )
    else:
        result.add("Build/ contains compiled output", False, "Skipped - Build/ not found")

    # --- Check 5: TemplateData folder exists (contains loading screen assets) ---
    template_dir = os.path.join(build_dir, "TemplateData")
    template_exists = os.path.isdir(template_dir)
    result.add(
        "TemplateData/ folder exists",
        template_exists,
        template_dir if template_exists else f"MISSING: {template_dir}",
    )

    # --- Check 6: Unity log scan (may or may not be present for WebGL) ---
    check_unity_log(build_dir, result)


# ---------------------------------------------------------------------------
# Shared checks
# ---------------------------------------------------------------------------

def check_unity_log(build_dir, result):
    """Scan Unity player/editor log files for error keywords."""

    # Look for log files in the build directory and common system locations.
    search_dirs = [build_dir]

    # Also check the Unity editor log location (where build-time errors appear).
    local_app_data = os.environ.get("LOCALAPPDATA", "")
    if local_app_data:
        search_dirs.append(os.path.join(local_app_data, "Unity", "Editor"))

    log_files_found = []
    for search_dir in search_dirs:
        for pattern in UNITY_LOG_PATTERNS:
            matches = glob.glob(os.path.join(search_dir, "**", pattern), recursive=True)
            log_files_found.extend(matches)

    if not log_files_found:
        # No log files to check - this is not a failure, just informational.
        result.add(
            "Unity log scan",
            True,
            "No log files found to scan (this is OK for packaged builds)",
        )
        return

    # Scan each log file for error keywords.
    errors_found = []
    for log_path in log_files_found:
        try:
            with open(log_path, "r", encoding="utf-8", errors="replace") as log_file:
                for line_number, line in enumerate(log_file, start=1):
                    for keyword in LOG_ERROR_KEYWORDS:
                        if keyword.lower() in line.lower():
                            # Record the first few errors; don't overwhelm the report.
                            if len(errors_found) < 10:
                                short_line = line.strip()[:120]
                                errors_found.append(
                                    f"  {os.path.basename(log_path)} line {line_number}: {short_line}"
                                )
        except OSError:
            # If we cannot read the log, note it but don't fail the whole check.
            errors_found.append(f"  Could not read: {log_path}")

    if errors_found:
        detail = f"{len(errors_found)} error(s) found:\n" + "\n".join(errors_found)
        result.add("Unity log scan (no errors)", False, detail)
    else:
        result.add(
            "Unity log scan (no errors)",
            True,
            f"Scanned {len(log_files_found)} log file(s), no errors found",
        )


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Validate a completed Unity build for any zSpace application.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Examples:\n"
            '  python validate_build.py --build-dir "C:/Builds/Win64" --build-type win64\n'
            '  python validate_build.py --build-dir "C:/Builds/Win64" --build-type win64 --app-name StudioA3\n'
            '  python validate_build.py --build-dir "C:/Builds/Win64" --build-type win64 --config ../configs/studio-a3.json\n'
        ),
    )
    parser.add_argument(
        "--build-dir",
        required=True,
        help="Path to the build output directory.",
    )
    parser.add_argument(
        "--build-type",
        required=True,
        choices=["win64", "webgl"],
        help="Type of build to validate: 'win64' or 'webgl'.",
    )
    parser.add_argument(
        "--app-name",
        default=None,
        help="Application EXE name without extension (e.g., StudioA3, FranklinsLabA3). "
             "Determines EXE filename and _Data folder name. Default: FranklinsLabA3",
    )
    parser.add_argument(
        "--config",
        default=None,
        help="Path to a JSON config file (e.g., configs/studio-a3.json). "
             "Overrides --app-name and other defaults with app-specific values.",
    )

    args = parser.parse_args()

    # Load config file if provided, then apply CLI overrides.
    global APP_NAME
    if args.config:
        with open(args.config, "r", encoding="utf-8") as f:
            config = json.load(f)
        APP_NAME = config.get("exe_name", APP_NAME)
        print(f"  Loaded config: {args.config} (app: {config.get('app_name', APP_NAME)})")
    if args.app_name:
        APP_NAME = args.app_name

    # Normalize the path (handle forward/back slashes, etc.).
    build_dir = os.path.abspath(args.build_dir)

    # Make sure the build directory actually exists before we start checking.
    if not os.path.isdir(build_dir):
        print(f"ERROR: Build directory does not exist: {build_dir}")
        sys.exit(1)

    print(f"Validating {args.build_type.upper()} build in: {build_dir}")

    result = ValidationResult()

    if args.build_type == "win64":
        validate_win64(build_dir, result)
    elif args.build_type == "webgl":
        validate_webgl(build_dir, result)

    result.print_report()

    # Exit with 0 (success) or 1 (failure) for CI/CD integration.
    sys.exit(0 if result.all_passed else 1)


if __name__ == "__main__":
    main()

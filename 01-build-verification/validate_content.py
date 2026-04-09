#!/usr/bin/env python3
"""
validate_content.py - Activity Pack Content Validation for Franklin's Lab A3
=============================================================================

This script validates that all expected .fla3 activity pack files are present
in a build output directory.  Franklin's Lab A3 ships with 28+ activity packs
that are loaded at runtime via the Addressable Assets system; if any are
missing or corrupted, students will see broken activities.

It checks:
  - All .fla3 files are found (recursively scans the given directory)
  - The count matches the expected number (default: 28, configurable)
  - Every file has a non-zero size (empty files indicate a build/copy failure)
  - Optionally, file SHA-256 hashes match a known-good manifest

Usage:
    python validate_content.py --content-dir "C:/Builds/Win64/FranklinsLabA3_Data"
    python validate_content.py --content-dir "C:/Builds/Win64" --expected-count 28
    python validate_content.py --content-dir "C:/Builds/Win64" --manifest manifest.json

Exit Codes:
    0 = All checks passed
    1 = One or more checks failed

Designed for integration into Jenkins CI/CD pipelines.
"""

import argparse
import hashlib
import json
import os
import sys


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

# The default number of .fla3 activity packs expected in a complete build.
# Override with --expected-count if the project adds or removes packs.
DEFAULT_EXPECTED_COUNT = 28

# Files smaller than this are almost certainly corrupt or empty.
MIN_FLA3_SIZE_BYTES = 100  # 100 bytes - even the smallest pack is larger


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
        print("  CONTENT VALIDATION REPORT")
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
# Helpers
# ---------------------------------------------------------------------------

def find_fla3_files(content_dir):
    """Recursively find all .fla3 files under the given directory.

    Returns a sorted list of absolute file paths.
    """
    fla3_files = []
    for root, _dirs, files in os.walk(content_dir):
        for filename in files:
            if filename.lower().endswith(".fla3"):
                fla3_files.append(os.path.join(root, filename))
    return sorted(fla3_files)


def compute_sha256(file_path):
    """Compute the SHA-256 hash of a file.  Returns the hex digest string."""
    sha256 = hashlib.sha256()
    with open(file_path, "rb") as f:
        # Read in 64 KB chunks to handle large files without excessive memory.
        while True:
            chunk = f.read(65536)
            if not chunk:
                break
            sha256.update(chunk)
    return sha256.hexdigest()


def load_manifest(manifest_path):
    """Load a JSON manifest file mapping filenames to expected SHA-256 hashes.

    Expected format:
    {
        "ActivityPack01.fla3": "a1b2c3d4...",
        "ActivityPack02.fla3": "e5f6g7h8...",
        ...
    }

    Returns a dict of {filename: expected_hash}.
    """
    with open(manifest_path, "r", encoding="utf-8") as f:
        manifest = json.load(f)

    # Validate structure: should be a flat dict of string -> string.
    if not isinstance(manifest, dict):
        raise ValueError(
            f"Manifest must be a JSON object (dict), got {type(manifest).__name__}"
        )
    for key, value in manifest.items():
        if not isinstance(key, str) or not isinstance(value, str):
            raise ValueError(
                f"Manifest entries must be string -> string, got {key!r} -> {value!r}"
            )

    return manifest


# ---------------------------------------------------------------------------
# Validation logic
# ---------------------------------------------------------------------------

def validate_content(content_dir, expected_count, manifest_path, result):
    """Run all content validation checks."""

    # --- Step 1: Find all .fla3 files ---
    fla3_files = find_fla3_files(content_dir)
    found_count = len(fla3_files)

    # Print the list of found files for diagnostic purposes.
    if fla3_files:
        print(f"\nFound {found_count} .fla3 file(s):")
        for filepath in fla3_files:
            size = os.path.getsize(filepath)
            print(f"  {os.path.basename(filepath)}  ({size:,} bytes)")
        print("")
    else:
        print(f"\nNo .fla3 files found under: {content_dir}\n")

    # --- Check 1: Expected file count ---
    count_ok = found_count >= expected_count
    result.add(
        f"Activity pack count ({found_count} of {expected_count} expected)",
        count_ok,
        f"Found {found_count}, expected at least {expected_count}",
    )

    # --- Check 2: No empty or suspiciously small files ---
    empty_files = []
    small_files = []
    for filepath in fla3_files:
        size = os.path.getsize(filepath)
        name = os.path.basename(filepath)
        if size == 0:
            empty_files.append(name)
        elif size < MIN_FLA3_SIZE_BYTES:
            small_files.append(f"{name} ({size} bytes)")

    if empty_files:
        result.add(
            "No empty .fla3 files",
            False,
            f"Empty files: {', '.join(empty_files)}",
        )
    else:
        result.add("No empty .fla3 files", True, "All files have content")

    if small_files:
        result.add(
            f"No suspiciously small .fla3 files (min {MIN_FLA3_SIZE_BYTES} bytes)",
            False,
            f"Small files: {', '.join(small_files)}",
        )
    else:
        result.add(
            f"No suspiciously small .fla3 files (min {MIN_FLA3_SIZE_BYTES} bytes)",
            True,
            "All files meet minimum size",
        )

    # --- Check 3: Manifest hash verification (optional) ---
    if manifest_path:
        verify_manifest(fla3_files, manifest_path, result)
    else:
        print("  (Skipping hash verification - no --manifest provided)\n")


def verify_manifest(fla3_files, manifest_path, result):
    """Compare found .fla3 files against a manifest of expected hashes."""

    # Load the manifest.
    try:
        manifest = load_manifest(manifest_path)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        result.add("Manifest file readable", False, f"Error: {exc}")
        return

    result.add("Manifest file readable", True, f"{len(manifest)} entries loaded")

    # Build a lookup from filename to filepath for the files we found.
    found_by_name = {}
    for filepath in fla3_files:
        name = os.path.basename(filepath)
        found_by_name[name] = filepath

    # Check each manifest entry.
    missing_from_build = []
    hash_mismatches = []
    hash_matches = 0

    for expected_name, expected_hash in sorted(manifest.items()):
        if expected_name not in found_by_name:
            missing_from_build.append(expected_name)
            continue

        actual_hash = compute_sha256(found_by_name[expected_name])
        if actual_hash.lower() != expected_hash.lower():
            hash_mismatches.append(
                f"{expected_name} (expected {expected_hash[:12]}..., "
                f"got {actual_hash[:12]}...)"
            )
        else:
            hash_matches += 1

    # Report missing files from manifest.
    if missing_from_build:
        result.add(
            "All manifest files present in build",
            False,
            f"Missing: {', '.join(missing_from_build)}",
        )
    else:
        result.add(
            "All manifest files present in build",
            True,
            f"All {len(manifest)} manifest entries found",
        )

    # Report hash mismatches.
    if hash_mismatches:
        detail = f"{len(hash_mismatches)} mismatch(es):\n"
        for mismatch in hash_mismatches:
            detail += f"    {mismatch}\n"
        result.add("File hashes match manifest", False, detail)
    else:
        result.add(
            "File hashes match manifest",
            True,
            f"{hash_matches} file(s) verified OK",
        )


# ---------------------------------------------------------------------------
# Generating a manifest (utility feature)
# ---------------------------------------------------------------------------

def generate_manifest(content_dir, output_path):
    """Scan a directory and write a manifest JSON file of .fla3 hashes.

    This is a convenience function for creating the initial manifest from a
    known-good build.  Run it once to generate the file, then commit the
    manifest to source control for future validation.

    Usage:
        python validate_content.py --content-dir "C:/GoodBuild" --generate-manifest manifest.json
    """
    fla3_files = find_fla3_files(content_dir)
    if not fla3_files:
        print(f"No .fla3 files found in {content_dir} - cannot generate manifest.")
        sys.exit(1)

    manifest = {}
    for filepath in fla3_files:
        name = os.path.basename(filepath)
        file_hash = compute_sha256(filepath)
        manifest[name] = file_hash
        print(f"  {name}: {file_hash}")

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, sort_keys=True)

    print(f"\nManifest written to: {output_path} ({len(manifest)} entries)")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Validate .fla3 activity pack content for Franklin's Lab A3.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Examples:\n"
            '  python validate_content.py --content-dir "C:/Builds/Win64"\n'
            '  python validate_content.py --content-dir "C:/Builds/Win64" --expected-count 30\n'
            '  python validate_content.py --content-dir "C:/Builds/Win64" --manifest manifest.json\n'
            "\n"
            "To generate a manifest from a known-good build:\n"
            '  python validate_content.py --content-dir "C:/GoodBuild" --generate-manifest manifest.json\n'
        ),
    )
    parser.add_argument(
        "--content-dir",
        required=True,
        help="Path to the directory containing .fla3 files (scanned recursively).",
    )
    parser.add_argument(
        "--expected-count",
        type=int,
        default=DEFAULT_EXPECTED_COUNT,
        help=f"Number of .fla3 files expected (default: {DEFAULT_EXPECTED_COUNT}).",
    )
    parser.add_argument(
        "--manifest",
        default=None,
        help="Path to a JSON manifest file for hash verification (optional).",
    )
    parser.add_argument(
        "--generate-manifest",
        default=None,
        metavar="OUTPUT_PATH",
        help=(
            "Instead of validating, generate a manifest file from the current "
            "content directory.  Use this once on a known-good build."
        ),
    )

    args = parser.parse_args()

    # Normalize the path.
    content_dir = os.path.abspath(args.content_dir)

    if not os.path.isdir(content_dir):
        print(f"ERROR: Content directory does not exist: {content_dir}")
        sys.exit(1)

    # If the user asked to generate a manifest, do that and exit.
    if args.generate_manifest:
        print(f"Generating manifest from: {content_dir}")
        generate_manifest(content_dir, args.generate_manifest)
        sys.exit(0)

    # Otherwise, run validation.
    print(f"Validating activity pack content in: {content_dir}")
    print(f"Expected count: {args.expected_count}")

    result = ValidationResult()
    validate_content(content_dir, args.expected_count, args.manifest, result)
    result.print_report()

    # Exit with 0 (success) or 1 (failure) for CI/CD integration.
    sys.exit(0 if result.all_passed else 1)


if __name__ == "__main__":
    main()

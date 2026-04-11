#!/usr/bin/env python3
from __future__ import annotations
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
import logging
import os
import sys

logger = logging.getLogger(__name__)


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

# ValidationResult is imported from qa_common to avoid duplication.
# Add the test-management directory to sys.path so we can import it.
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "03-test-management"))
from qa_common import ValidationResult  # noqa: E402


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
        logger.info("\nFound %d .fla3 file(s):", found_count)
        for filepath in fla3_files:
            size = os.path.getsize(filepath)
            logger.info("  %s  (%s bytes)", os.path.basename(filepath), f"{size:,}")
        logger.info("")
    else:
        logger.info("\nNo .fla3 files found under: %s\n", content_dir)

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
        logger.info("  (Skipping hash verification - no --manifest provided)\n")


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
        logger.error("No .fla3 files found in %s - cannot generate manifest.", content_dir)
        sys.exit(1)

    manifest = {}
    for filepath in fla3_files:
        name = os.path.basename(filepath)
        file_hash = compute_sha256(filepath)
        manifest[name] = file_hash
        logger.info("  %s: %s", name, file_hash)

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, sort_keys=True)

    logger.info("\nManifest written to: %s (%d entries)", output_path, len(manifest))


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

    parser.add_argument("--verbose", action="store_true", help="Enable verbose logging")
    parser.add_argument("--quiet", action="store_true", help="Suppress informational output")

    args = parser.parse_args()

    # Matches setup_logging() in qa_common.py
    level = logging.DEBUG if args.verbose else logging.WARNING if args.quiet else logging.INFO
    logging.basicConfig(level=level, format="%(message)s",
                        handlers=[logging.StreamHandler(sys.stdout)])

    # Normalize the path.
    content_dir = os.path.abspath(args.content_dir)

    if not os.path.isdir(content_dir):
        logger.error("ERROR: Content directory does not exist: %s", content_dir)
        sys.exit(1)

    # If the user asked to generate a manifest, do that and exit.
    if args.generate_manifest:
        logger.info("Generating manifest from: %s", content_dir)
        generate_manifest(content_dir, args.generate_manifest)
        sys.exit(0)

    # Otherwise, run validation.
    logger.info("Validating activity pack content in: %s", content_dir)
    logger.info("Expected count: %d", args.expected_count)

    result = ValidationResult("CONTENT VALIDATION REPORT")
    validate_content(content_dir, args.expected_count, args.manifest, result)
    result.print_report()

    # Exit with 0 (success) or 1 (failure) for CI/CD integration.
    sys.exit(0 if result.all_passed else 1)


if __name__ == "__main__":
    main()

# QA Automation POC - zSpace Unity AR/VR Applications

A proof-of-concept demonstrating automated QA testing for **Franklin's Lab A3** and other zSpace Unity applications. This repo is designed as a **handoff to the development team** — each prototype is self-contained, documented, and ready to extend.

## Why This Exists

Today, Franklin's Lab ships with:
- **Zero automated tests** (Unity Test Framework is installed but unused)
- **No post-build validation** (builds go out without checking if files are intact)
- **Manual test tracking** (111 test cases managed by hand across Jira and TestRail)
- **Manual install/license testing** (34 test cases run by hand every release)

This POC proves that automation is practical and high-value, without requiring zSpace hardware.

## The 4 Prototypes

| # | Prototype | What It Does | Effort |
|---|-----------|-------------|--------|
| 1 | [Build Verification](01-build-verification/) | Post-build scripts that catch missing files, broken builds, corrupted content | ~1-2 weeks |
| 2 | [Unity Unit Tests](02-unity-unit-tests/) | C# test templates for core app logic (activity gallery, inventory, labels) | ~2-3 weeks |
| 3 | [Test Management](03-test-management/) | One-command test cycle generation for Jira + TestRail + coverage dashboard | ~1-2 weeks |
| 4 | [Install/Licensing](04-install-licensing/) | PowerShell scripts automating 15-20 of the 34 install/license test cases | ~2-3 weeks |

## Quick Start

### Prototype #1 — Build Verification
```bash
# Validate a Win64 build
python 01-build-verification/validate_build.py --build-dir "C:/Builds/FranklinsLab" --build-type win64

# Validate activity pack content
python 01-build-verification/validate_content.py --content-dir "C:/Builds/FranklinsLab/ActivityPacks"
```

### Prototype #2 — Unity Unit Tests
Copy the `02-unity-unit-tests/EditModeTests/` folder into your Unity project's `Assets/Tests/` directory. See the [setup guide](02-unity-unit-tests/setup-guide.md) for details.

```bash
# Run from command line (for Jenkins)
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

### Prototype #3 — Test Management Pipeline
```bash
# Generate a full test cycle for a release
python 03-test-management/run_cycle.py --release-version 5.6.0 --output-dir ./output

# Generate a coverage report from test results
python 03-test-management/coverage_report.py --results-file 03-test-management/templates/sample_results.json --output html
```

### Prototype #4 — Install/Licensing
```powershell
# Dry run first (shows what would happen, doesn't execute)
.\04-install-licensing\install_tests.ps1 -DryRun

# Run install tests
.\04-install-licensing\install_tests.ps1

# Run licensing tests
.\04-install-licensing\license_tests.ps1 -DryRun
```

## Related Repos

| Repo | Purpose |
|------|---------|
| [apps.franklins-lab-a3](https://github.com/zspace/apps.franklins-lab-a3) | Main Unity application |
| [apps.franklinslaba3.activity-pack](https://github.com/zspace/apps.franklinslaba3.activity-pack) | Activity content files (.fla3) |
| [apps.franklins-lab-a3.activity-pack-source](https://github.com/zspace/apps.franklins-lab-a3.activity-pack-source) | Activity pack source |
| [3rdParty_QA_Requirements](https://github.com/jdonnelly-zspace/3rdParty_QA_Requirements) | 111 QA test cases in JSON/YAML |

## Future Backlog

See [docs/backlog.md](docs/backlog.md) for the full prioritized list of 10 additional automation items beyond these 4 prototypes.

## Requirements

- **Python 3.8+** (Prototypes #1, #3)
- **PowerShell 5.1+** (Prototype #4, included with Windows 10/11)
- **Unity 2019.4+** with Test Framework package (Prototype #2)
- No zSpace hardware required for any prototype

# Developer Handoff Document

## Overview

This repo contains 4 QA automation prototypes ready for the dev team to extend and integrate into the zSpace build pipeline. Each prototype is self-contained in its own folder with working code and documentation.

## What's Been Done (POC Scope)

### Prototype #1: Build Verification
- `validate_build.py` — checks build outputs (EXE exists, DLLs present, code signing valid)
- `validate_content.py` — checks activity pack files (.fla3 count, sizes, optional hash verification)
- `jenkins/Jenkinsfile.example` — shows how to add validation as a Jenkins post-build stage
- **Status:** Ready to test against a real build output directory

### Prototype #2: Unity Unit Tests
- Template C# test files for ActivityGallery, Inventory, and ContentValidation
- Assembly definition file (.asmdef) for Unity Test Framework integration
- Setup guide explaining how to add tests to any zSpace Unity project
- **Status:** Templates need adaptation to actual class names and method signatures. Look for `TODO` comments.

### Prototype #3: Test Management Pipeline
- `run_cycle.py` — generates Jira CSV, markdown checklist, and TestRail XML from test case definitions
- `coverage_report.py` — generates HTML or markdown coverage dashboards
- Sample data included so scripts work out of the box for demos
- **Status:** Ready to connect to real test case data from 3rdParty_QA_Requirements repo

### Prototype #4: Install/Licensing Automation
- `install_tests.ps1` — automates fresh install, upgrade, uninstall, standard user, and repair scenarios
- `license_tests.ps1` — automates zCentral activation, offline activation, deactivation, and network-disabled scenarios
- `test_matrix.md` — maps all 34 QA requirement test cases to automation coverage
- **Status:** Scripts have DryRun mode. Test on a dedicated VM before running live.

## What the Dev Team Needs to Do

### Immediate (Week 1)
1. **Review all `TODO` markers** in the code — these mark spots that need real values (paths, app names, installer locations)
2. **Test Prototype #1** against an actual Jenkins build output
3. **Test Prototype #3** by pointing it at the real `3rdParty_QA_Requirements` test case files

### Short Term (Weeks 2-4)
4. **Adapt Prototype #2** C# tests to actual Franklin's Lab class names and integrate into the Unity project
5. **Test Prototype #4** on a dedicated Windows VM (use DryRun first!)
6. **Integrate Prototype #1** into the real Jenkins pipeline using the Jenkinsfile.example as reference

### Medium Term (Months 2-3)
7. Expand unit test coverage beyond the 10-15 template tests
8. Add Play-Mode integration tests (Backlog item #5)
9. Connect test management pipeline to live Jira/TestRail instances
10. Set up Sentry regression alerts (Backlog item #7)

## Key Technical Decisions Left for Dev Team

| Decision | Options | POC Default | Notes |
|----------|---------|-------------|-------|
| Python version | 3.8+ | 3.8 | No external dependencies needed for Prototypes #1 and #3 |
| PowerShell execution policy | RemoteSigned vs Unrestricted | RemoteSigned | May need adjustment for test VMs |
| Test results storage | Local files vs database vs TestRail | Local JSON | Scale up when ready |
| Jenkins integration | New stage vs separate job | New stage | Jenkinsfile.example shows the pattern |
| Unity test assembly references | Per-project | Generic templates | Dev team adds project-specific .asmdef references |

## Files Quick Reference

```
qa-automation-poc/
├── README.md                           # Start here
├── 01-build-verification/
│   ├── validate_build.py               # Run against build output
│   ├── validate_content.py             # Run against content directory
│   └── jenkins/Jenkinsfile.example     # Copy into your pipeline
├── 02-unity-unit-tests/
│   ├── EditModeTests/*.cs              # Copy into Unity project
│   ├── EditModeTests/*.asmdef          # Unity assembly definition
│   └── setup-guide.md                  # How to integrate
├── 03-test-management/
│   ├── run_cycle.py                    # Generate test cycles
│   ├── coverage_report.py             # Generate dashboards
│   └── templates/                      # Sample data
├── 04-install-licensing/
│   ├── install_tests.ps1               # Run on test VM
│   ├── license_tests.ps1              # Run on test VM
│   └── test_matrix.md                  # Coverage mapping
└── docs/
    ├── handoff.md                      # This file
    └── backlog.md                      # Future items
```

## Questions? Contact

This POC was created by the QA team. For questions about:
- **Test requirements and priorities:** See the 3rdParty_QA_Requirements repo
- **Franklin's Lab app architecture:** See the apps.franklins-lab-a3 repo
- **Extending these prototypes:** Start with the README in each prototype folder

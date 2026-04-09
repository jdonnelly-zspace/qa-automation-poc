# Installation & Licensing Test Matrix

**Prototype #4 -- POC QA Automation**
**Date:** 2026-04-09
**Total QA Requirement Test Cases:** 34
**Automated (Full or Partial):** 18
**Manual Only:** 16

---

## How to Read This Table

| Column | Description |
|--------|-------------|
| **Test Case ID** | Identifier from the QA requirements repository |
| **Title** | Short description of what the test verifies |
| **Category** | Install, License, Permissions, or Network |
| **Automated?** | **Yes** = fully scripted, **Partial** = scripted but needs manual verification for some steps, **No** = manual only |
| **Script Reference** | Which script and function covers this test case |
| **Notes** | Why a test is manual-only, or what the partial gap is |

---

## Fresh Install

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-001 | Fresh install via App Manager (admin user) | Install | Yes | `install_tests.ps1` / `Test-FreshInstall` | Silent install, verifies registry, shortcuts, and app launch |
| IL-002 | Verify install directory structure | Install | Yes | `install_tests.ps1` / `Test-FreshInstall` | Checks expected install path and main executable |
| IL-003 | Verify Start Menu and Desktop shortcuts | Install | Yes | `install_tests.ps1` / `Test-FreshInstall` | Validates configurable list of shortcut paths |
| IL-004 | Verify app launches after fresh install | Install | Yes | `install_tests.ps1` / `Test-FreshInstall` | Starts the executable, confirms it does not crash within 10 seconds |
| IL-005 | Verify correct version in Programs and Features | Install | Yes | `install_tests.ps1` / `Test-FreshInstall` | Reads DisplayVersion from the uninstall registry key |
| IL-006 | Fresh install with custom install path | Install | No | -- | Requires interactive installer UI to change the install directory |
| IL-007 | Fresh install on minimum-spec hardware | Install | No | -- | Requires specific hardware environment; not scriptable |

## Upgrade

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-008 | Upgrade from previous version (in-place) | Install | Yes | `install_tests.ps1` / `Test-Upgrade` | Installs old version, then new version; checks version bump |
| IL-009 | Verify user data preserved after upgrade | Install | Partial | `install_tests.ps1` / `Test-Upgrade` | Script checks version; data preservation requires manual inspection of app state |
| IL-010 | Upgrade from two major versions back | Install | Partial | `install_tests.ps1` / `Test-Upgrade` | Requires a specific older installer; same script logic applies |
| IL-011 | Verify rollback if upgrade fails mid-install | Install | No | -- | Requires simulating a mid-install failure; difficult to automate reliably |

## Uninstall

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-012 | Uninstall via Programs and Features | Install | Yes | `install_tests.ps1` / `Test-Uninstall` | Runs the uninstall string, verifies cleanup |
| IL-013 | Verify no leftover files after uninstall | Install | Yes | `install_tests.ps1` / `Test-Uninstall` | Checks install directory and shortcut paths |
| IL-014 | Verify no leftover registry entries | Install | Yes | `install_tests.ps1` / `Test-Uninstall` | Checks configurable list of registry paths |
| IL-015 | Reinstall after uninstall (clean slate) | Install | Partial | `install_tests.ps1` / `Test-Uninstall` then `Test-FreshInstall` | Run Uninstall then FreshInstall in sequence; manual review of state between |
| IL-016 | Uninstall with app running | Install | No | -- | Requires launching the app and attempting uninstall concurrently; needs UI interaction to handle prompts |

## Repair Install

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-017 | Repair install restores corrupted files | Install | Yes | `install_tests.ps1` / `Test-RepairInstall` | Runs repair mode, verifies app still launches |
| IL-018 | Repair install preserves user settings | Install | Partial | `install_tests.ps1` / `Test-RepairInstall` | Script verifies app launches; settings preservation requires manual check |

## Permissions

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-019 | Install as standard (non-admin) user | Permissions | Yes | `install_tests.ps1` / `Test-StandardUserInstall` | Verifies install is blocked or prompts UAC |
| IL-020 | Run app as standard user after admin install | Permissions | Partial | `install_tests.ps1` / `Test-FreshInstall` | Install is automated; running as another user requires additional credential setup |
| IL-021 | Verify UAC prompt appears for install | Permissions | No | -- | UAC dialog requires physical screen interaction; cannot be automated in script |
| IL-022 | Verify file permissions on install directory | Permissions | No | -- | Requires inspecting ACLs; could be partially automated in a future prototype |

## Licensing -- Online (zCentral)

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-023 | Activate license via zCentral | License | Yes | `license_tests.ps1` / `Test-ZCentralActivation` | Runs CLI activation, verifies state change |
| IL-024 | Verify licensed state in the app UI | License | Partial | `license_tests.ps1` / `Test-ZCentralActivation` | Registry/file state checked; in-app UI label requires UI automation |
| IL-025 | Deactivate license via zCentral | License | Yes | `license_tests.ps1` / `Test-LicenseDeactivation` | Runs CLI deactivation, verifies state change |
| IL-026 | Re-activate after deactivation | License | Yes | `license_tests.ps1` / `Test-ZCentralActivation` | Run deactivation then activation in sequence |
| IL-027 | Activate with invalid license key | License | No | -- | Requires testing multiple invalid key formats; would need error-code mapping from the licensing system |
| IL-028 | Legacy zSpace licensing vs new zCentral | License | No | -- | Requires access to legacy licensing infrastructure; manual comparison test |

## Licensing -- Offline

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-029 | Offline activation with license file | License | Yes | `license_tests.ps1` / `Test-OfflineActivation` | Places pre-generated license file, verifies state |
| IL-030 | Generate offline license request | License | No | -- | Request generation typically involves app UI interaction |
| IL-031 | Offline activation with wrong hardware ID | License | No | -- | Requires a license file for a different machine; error handling not yet mapped |

## Network Scenarios

| Test Case ID | Title | Category | Automated? | Script Reference | Notes |
|:---:|--------|:--------:|:----------:|------------------|-------|
| IL-032 | Launch app with network disabled | Network | Yes | `license_tests.ps1` / `Test-NetworkDisabled` | Disables adapter, launches app, checks for crash, restores adapter |
| IL-033 | License check-in with intermittent network | Network | No | -- | Requires simulating packet loss or flaky connection; needs specialized tooling |
| IL-034 | Expired license behavior | License | Partial | `license_tests.ps1` / `Test-ExpiredLicense` | Places expired license file; in-app message verification requires UI automation |

---

## Summary by Automation Status

| Status | Count | Percentage |
|--------|:-----:|:----------:|
| **Yes** (fully automated) | 12 | 35% |
| **Partial** (scripted with manual gaps) | 6 | 18% |
| **No** (manual only) | 16 | 47% |
| **Total** | **34** | 100% |

## Common Reasons for Manual-Only

| Reason | Test Case IDs |
|--------|---------------|
| Requires interactive installer UI | IL-006, IL-021 |
| Requires specific hardware | IL-007 |
| Requires simulating failure conditions | IL-011, IL-033 |
| Requires app to be running during operation | IL-016 |
| Requires UI automation for in-app state | IL-027, IL-030, IL-031 |
| Requires legacy infrastructure | IL-028 |
| Requires ACL inspection (future candidate) | IL-022 |

## Next Steps for Increasing Coverage

1. **UI Automation (biggest uplift):** Adding a UI automation layer (e.g., WinAppDriver, FlaUI, or Appium for Windows) would move 4-5 "Partial" tests to "Yes" and enable 3-4 currently manual tests.
2. **ACL Verification:** A PowerShell function using `Get-Acl` could automate IL-022.
3. **Network Simulation:** Tools like `clumsy` or `tc` (via WSL) could enable IL-033.
4. **Error-Code Mapping:** Documenting the licensing CLI's error codes would enable IL-027 and IL-031.

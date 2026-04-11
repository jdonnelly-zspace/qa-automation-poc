# How to Run QA Automation for a New zSpace App

This guide shows how to use the QA automation prototypes for any zSpace Unity application. Studio A3 is used as a worked example.

## Step 1: Create a Config File

Copy an existing config and edit it for your app:

```
configs/
  franklins-lab-a3.json   <-- existing
  studio-a3.json          <-- existing
  your-new-app.json       <-- create this
```

The config file contains all app-specific values:

```json
{
  "app_name": "Studio A3",
  "exe_name": "StudioA3",
  "display_name": "zSpace Studio A3",
  "install_dir": "C:\\Program Files\\zSpace\\Studio A3",
  "installer_path": "C:\\Installers\\StudioA3_Setup.exe",
  "main_executable": "StudioA3.exe",
  "expected_content_count": 0,
  "expected_version": "1.0.0.0"
}
```

Key fields:
- `exe_name` - The EXE filename without `.exe` (must match what Unity builds)
- `expected_content_count` - Number of .fla3 activity files (0 if the app doesn't use them)
- `install_dir` - Where the installer puts the app on Windows

## Step 2: Run the Prototypes

All commands below assume you're in the `qa-automation-poc` folder:

```
cd "C:\Users\Jilldonnelly\Documents\Claude\Projects\POC QA Automation\qa-automation-poc"
```

### Prototype #1 - Build Verification

Validate a Win64 build:
```
python 01-build-verification/validate_build.py --build-dir "C:\Builds\StudioA3\Win64" --build-type win64 --config configs/studio-a3.json
```

Validate a WebGL build:
```
python 01-build-verification/validate_build.py --build-dir "C:\Builds\StudioA3\WebGL" --build-type webgl --config configs/studio-a3.json
```

Validate activity pack content (if applicable):
```
python 01-build-verification/validate_content.py --content-dir "C:\Builds\StudioA3\ActivityPacks" --expected-count 0
```

You can also skip the config file and just pass the app name directly:
```
python 01-build-verification/validate_build.py --build-dir "C:\Builds\StudioA3\Win64" --build-type win64 --app-name StudioA3
```

### Prototype #2 - Unity Unit Tests

This requires changes inside the Unity project itself:

1. Open `apps.studioa3` in Unity
2. If Unity Test Framework is not installed, add it to `Packages/manifest.json`:
   ```json
   "com.unity.test-framework": "1.1.31"
   ```
3. Copy `02-unity-unit-tests/EditModeTests/` into `Assets/Tests/EditModeTests/`
4. Edit the `.cs` files to reference Studio A3's actual class names (look for `TODO` comments)
5. Open Window > General > Test Runner in Unity to run them

Or run from the command line (for Jenkins):
```
Unity -batchmode -runTests -testPlatform EditMode -projectPath "C:\Projects\apps.studioa3" -testResults results.xml
```

See `02-unity-unit-tests/setup-guide.md` for full details.

### Prototype #3 - Test Management Pipeline

Generate a test cycle for Studio A3:
```
python 03-test-management/run_cycle.py --release-version 1.0.0 --config configs/studio-a3.json
```

This creates three files in `./output/1.0.0/`:
- `jira_test_cycle_1.0.0.csv` - Import into Jira
- `qa_checklist_1.0.0.md` - Markdown checklist for QA sign-off
- `testrail_import_1.0.0.xml` - Import into TestRail

Preview Jira tickets without creating them:
```
python 03-test-management/run_cycle.py --release-version 1.0.0 --config configs/studio-a3.json --jira-dry-run --jira-project-key QA
```

Generate a coverage report from test results:
```
python 03-test-management/coverage_report.py --output html
```

### Prototype #4 - Install/Licensing Tests

Dry run first (safe - shows what would happen without doing anything):
```powershell
.\04-install-licensing\install_tests.ps1 -DryRun -ConfigFile configs\studio-a3.json
.\04-install-licensing\license_tests.ps1 -DryRun -ConfigFile configs\studio-a3.json
```

Run for real (on a test machine only!):
```powershell
.\04-install-licensing\install_tests.ps1 -ConfigFile configs\studio-a3.json
.\04-install-licensing\license_tests.ps1 -ConfigFile configs\studio-a3.json
```

Or pass values directly without a config file:
```powershell
.\04-install-licensing\install_tests.ps1 -DryRun -AppName "zSpace Studio A3" -InstallerPathParam "C:\Installers\StudioA3_Setup.exe"
```

## Step 3: What If Your App Is Different?

| Difference | What to do |
|-----------|-----------|
| No activity packs (.fla3 files) | Set `expected_content_count` to 0 in config. Skip `validate_content.py`. |
| Different installer format (MSI vs EXE) | Update `SilentInstallArgs` in config or pass via CLI |
| No Unity Test Framework | Add it to `Packages/manifest.json` (one line) |
| Different build output structure | Update `validate_build.py` required DLLs list if needed |
| WebGL-only (no Win64) | Only run `--build-type webgl` validation |

## How to Add a New Scanner

If you need a scanner for a new category of checks (beyond source code, localization, assets, and coverage), follow this pattern:

### 1. Create the scanner file

Create `03-test-management/scan_yourcheck.py` and import the shared utilities:

```python
from qa_common import (
    check, skip, warn,
    build_scanner_argparser, load_config, setup_logging,
    resolve_output_dir, save_results, print_summary,
)
```

### 2. Use the standard CLI parser

```python
def main():
    parser = build_scanner_argparser("Scan something for a zSpace Unity project")
    # Add any scanner-specific args:
    parser.add_argument("--extra-flag", default=None, help="...")
    args = parser.parse_args()
    setup_logging(args.verbose, args.quiet)
```

### 3. Implement scan logic using check()/skip()/warn()

```python
results = []
results.append(check("MY-001", "Some check title", "Category", "High",
                      some_condition, "Detail if failed"))
results.append(skip("MY-002", "Skipped check", "Category", "Low",
                     "Reason it was skipped"))
```

### 4. Save results and print summary

```python
output_dir = resolve_output_dir(args.output_dir)
results_path, data = save_results(results, app_name, config, output_dir, "my_scan")
print_summary(results, app_name, results_path, args.config)
```

### 5. Wire into run_qa.py

Add your scanner to the `run_pipeline()` function in `run_qa.py`, following the pattern of existing scanner calls.

## Quick Reference: Studio A3 Commands

```
# From the qa-automation-poc folder:

# Validate a build
python 01-build-verification/validate_build.py --build-dir "C:\Builds\StudioA3\Win64" --build-type win64 --config configs/studio-a3.json

# Generate test cycle
python 03-test-management/run_cycle.py --release-version 1.0.0 --config configs/studio-a3.json

# Preview Jira tickets
python 03-test-management/run_cycle.py --release-version 1.0.0 --config configs/studio-a3.json --jira-dry-run --jira-project-key QA

# Coverage report
python 03-test-management/coverage_report.py --output html

# Install tests (dry run)
powershell .\04-install-licensing\install_tests.ps1 -DryRun -ConfigFile configs\studio-a3.json

# License tests (dry run)
powershell .\04-install-licensing\license_tests.ps1 -DryRun -ConfigFile configs\studio-a3.json
```

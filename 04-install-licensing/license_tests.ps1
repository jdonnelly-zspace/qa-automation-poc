#Requires -Version 5.1
<#
.SYNOPSIS
    Automated Licensing Tests for zSpace App Manager
    Prototype #4 - POC QA Automation

.DESCRIPTION
    This script automates licensing test cases for the zSpace App Manager,
    covering zCentral cloud activation, offline activation, deactivation,
    network-disabled scenarios, and expired-license behavior.

    IMPORTANT: This is a PROTOTYPE. Review all TODO comments and configure
    the variables in the CONFIGURATION section before running.

    WARNING: Some tests (deactivation, network disable) can affect a real
    license or network connection. Use only on dedicated test machines.

.PARAMETER DryRun
    When specified, the script prints what it WOULD do without actually
    modifying any license state or network settings. Safe for stakeholder demos.

.PARAMETER OutputPath
    Path for the JSON results file. Defaults to .\license_results.json

.PARAMETER TestFilter
    Run only tests whose names match this wildcard.

.EXAMPLE
    # Safe preview -- no license changes, no network changes
    .\license_tests.ps1 -DryRun

.EXAMPLE
    # Run all license tests
    .\license_tests.ps1

.EXAMPLE
    # Run only the offline activation test
    .\license_tests.ps1 -TestFilter "OfflineActivation"

.NOTES
    Author:  QA Automation POC Team
    Date:    2026-04-09
    Version: 0.4.0 (Prototype #4)
    Platform: Windows 10/11 only
#>

[CmdletBinding()]
param(
    [switch]$DryRun,

    [string]$OutputPath = (Join-Path $PSScriptRoot "license_results.json"),

    [string]$TestFilter = "*",

    # App-specific parameters (override defaults or use --ConfigFile)
    [string]$AppName = "",
    [string]$ConfigFile = "",
    [string]$AppExecutableParam = "",
    [string]$LicenseCliToolParam = ""
)

# ============================================================================
# CONFIGURATION -- Defaults (override via parameters or config file)
# ============================================================================

# Load from config file if provided
$config = $null
if ($ConfigFile -and (Test-Path $ConfigFile)) {
    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    Write-Host "  Loaded config: $ConfigFile (app: $($config.app_name))" -ForegroundColor Cyan
}

# --- Application paths ---

$AppDisplayName = if ($AppName) { $AppName }
    elseif ($config.display_name) { $config.display_name }
    else { "zSpace App Manager" }

$AppExecutable = if ($AppExecutableParam) { $AppExecutableParam }
    elseif ($config.install_dir -and $config.main_executable) { Join-Path $config.install_dir $config.main_executable }
    else { "C:\Program Files\zSpace\zSpace App Manager\zSpaceAppManager.exe" }

# --- zCentral (cloud) activation settings ---

$ZCentralUrl = "https://zcentral.zspace.com"
$TestLicenseKey = "XXXX-XXXX-XXXX-XXXX"

$LicenseCliTool = if ($LicenseCliToolParam) { $LicenseCliToolParam }
    elseif ($config.license_cli_tool) { $config.license_cli_tool }
    else { "C:\Program Files\zSpace\zSpace App Manager\zSpaceLicense.exe" }

# TODO: Set the CLI arguments for activation via zCentral
#       Example: --activate --key <KEY> --server <URL>
$ActivationArgs = "--activate --key $TestLicenseKey --server $ZCentralUrl"

# TODO: Set the CLI arguments for deactivation
$DeactivationArgs = "--deactivate --key $TestLicenseKey --server $ZCentralUrl"

# --- Offline activation settings ---

# TODO: Set the path where offline license request files are generated
$OfflineRequestPath = "C:\ProgramData\zSpace\LicenseRequest.xml"

# TODO: Set the path where you place the offline license response file
$OfflineLicenseFile = "C:\ProgramData\zSpace\License.lic"

# TODO: Set a pre-generated offline license file for testing
#       This should be a valid license file for your test machine's hardware ID.
$TestOfflineLicenseFile = "C:\TestData\test_offline_license.lic"

# TODO: Set the CLI arguments for offline activation (import license file)
$OfflineActivationArgs = "--activate-offline --license-file `"$OfflineLicenseFile`""

# --- License state verification ---

# TODO: Set the registry path or file path where the app stores license state
$LicenseRegistryPath = "HKLM:\SOFTWARE\zSpace\App Manager\License"

# TODO: Set the registry value name that indicates licensed status
$LicenseStatusValue = "LicenseStatus"

# TODO: Set what value means "licensed" (e.g., "Active", "1", "Licensed")
$LicensedStateExpected = "Active"

# TODO: Set what value means "unlicensed" (e.g., "Inactive", "0", "Trial")
$UnlicensedStateExpected = "Inactive"

# --- Network adapter settings (for network-disabled test) ---

# TODO: Set the name of the network adapter to disable/enable
#       Run Get-NetAdapter to see adapter names on this machine
$NetworkAdapterName = "Ethernet"

# --- Safety flags ---

# Set to $true ONLY on dedicated test machines
$IsTestMachine = $false  # TODO: Set to $true on your test machine

# Extra safety: set to $true to allow network adapter manipulation
$AllowNetworkChanges = $false  # TODO: Set to $true if network tests are needed

# Extra safety: set to $true to allow license deactivation
$AllowDeactivation = $false  # TODO: Set to $true if deactivation tests are needed

# ============================================================================
# INTERNAL STATE
# ============================================================================

$script:TestResults = @()
$script:StartTime = Get-Date

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-Log {
    <#
    .SYNOPSIS
        Writes a timestamped, color-coded message to the console.
    #>
    param(
        [string]$Message,
        [ValidateSet("INFO","WARN","ERROR","PASS","FAIL","DRYRUN")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $colors = @{
        INFO   = "Cyan"
        WARN   = "Yellow"
        ERROR  = "Red"
        PASS   = "Green"
        FAIL   = "Red"
        DRYRUN = "Magenta"
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $colors[$Level]
}

function Add-TestResult {
    <#
    .SYNOPSIS
        Records a test result into the results collection.
    #>
    param(
        [string]$TestName,
        [string]$Status,
        [string]$Details = "",
        [double]$DurationSec = 0
    )

    $script:TestResults += [PSCustomObject]@{
        TestName    = $TestName
        Status      = $Status
        Details     = $Details
        DurationSec = [math]::Round($DurationSec, 2)
        Timestamp   = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss")
    }
}

function Assert-TestMachine {
    <#
    .SYNOPSIS
        Returns $true if this is a designated test machine, $false otherwise.
    #>
    if (-not $IsTestMachine) {
        Write-Log "SAFETY: `$IsTestMachine is `$false. Skipping destructive operation." "WARN"
        Write-Log "Set `$IsTestMachine = `$true in CONFIGURATION if this is a test machine." "WARN"
        return $false
    }
    return $true
}

function Get-LicenseState {
    <#
    .SYNOPSIS
        Reads the current license state from the registry or license file.
        Returns a string representing the current state, or "UNKNOWN" if
        the state cannot be determined.
    #>

    # Method 1: Check registry
    if (Test-Path $LicenseRegistryPath) {
        try {
            $value = (Get-ItemProperty -Path $LicenseRegistryPath -Name $LicenseStatusValue -ErrorAction Stop).$LicenseStatusValue
            return $value
        } catch {
            Write-Log "Could not read license state from registry: $_" "WARN"
        }
    }

    # Method 2: Check for license file existence
    if (Test-Path $OfflineLicenseFile) {
        return "LicenseFilePresent"
    }

    # TODO: Method 3: You could also query the app's CLI for license status
    #       Example: & $LicenseCliTool --status
    #       Parse the output to determine the state.

    return "UNKNOWN"
}

function Test-AppShowsLicensedState {
    <#
    .SYNOPSIS
        Launches the app briefly and checks whether it reports itself as licensed.
        Returns $true if the app appears to be in a licensed state.

        NOTE: This is a simplified check. In practice, you may need to use
        UI automation or a CLI query to verify the in-app license display.
    #>

    # TODO: Replace this with actual verification logic.
    #       Options:
    #       1. Query a CLI: & $LicenseCliTool --status | Select-String "Licensed"
    #       2. Check a log file the app writes on startup
    #       3. Use UI automation to read a label in the app window

    $state = Get-LicenseState
    Write-Log "Current license state: $state" "INFO"
    return ($state -eq $LicensedStateExpected -or $state -eq "LicenseFilePresent")
}

# ============================================================================
# TEST FUNCTIONS
# ============================================================================

function Test-ZCentralActivation {
    <#
    .SYNOPSIS
        Activates the zSpace license via the zCentral cloud service and
        verifies the application transitions to a licensed state.

    .DESCRIPTION
        Steps:
        1. Verify the app is currently NOT licensed (clean starting state)
        2. Run the activation command with the test license key
        3. Verify the license state changes to "Active"
        4. Verify the app launches without license warnings
    #>

    $testName = "ZCentralActivation"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would check current license state" "DRYRUN"
        Write-Log "[DRYRUN] Would run: $LicenseCliTool $ActivationArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify license state changes to '$LicensedStateExpected'" "DRYRUN"
        Write-Log "[DRYRUN] Would launch app and verify no license warnings" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Pre-check: license CLI tool exists ---
    if (-not (Test-Path $LicenseCliTool)) {
        Write-Log "License CLI tool not found: $LicenseCliTool" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Missing: $LicenseCliTool" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Check current state ---
    $currentState = Get-LicenseState
    Write-Log "Current license state: $currentState" "INFO"
    if ($currentState -eq $LicensedStateExpected) {
        Write-Log "App is already licensed. Deactivate first for a clean test." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Already licensed. Deactivate first." -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 2: Activate via zCentral ---
    Write-Log "Activating via zCentral: $LicenseCliTool $ActivationArgs" "INFO"
    try {
        $output = & $LicenseCliTool $ActivationArgs.Split(" ") 2>&1
        $exitCode = $LASTEXITCODE
        Write-Log "Activation output: $output" "INFO"
        Write-Log "Activation exit code: $exitCode" "INFO"

        if ($exitCode -ne 0) {
            Write-Log "Activation command failed" "FAIL"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Exit code: $exitCode. Output: $output" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
    } catch {
        Write-Log "Exception during activation: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 3: Verify license state ---
    Start-Sleep -Seconds 5
    $newState = Get-LicenseState
    if ($newState -eq $LicensedStateExpected) {
        Write-Log "License state is now: $newState (expected: $LicensedStateExpected)" "PASS"
    } else {
        Write-Log "License state is: $newState (expected: $LicensedStateExpected)" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "State after activation: $newState" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 4: Verify app launches in licensed mode ---
    if (Test-Path $AppExecutable) {
        try {
            $appProc = Start-Process -FilePath $AppExecutable -PassThru -ErrorAction Stop
            Start-Sleep -Seconds 10
            if (-not $appProc.HasExited) {
                Write-Log "App running in licensed state" "PASS"
                $appProc.Kill()
                $appProc.WaitForExit(10000)
            } else {
                Write-Log "App exited unexpectedly after activation" "WARN"
            }
        } catch {
            Write-Log "Could not launch app: $_" "WARN"
        }
    }

    Add-TestResult -TestName $testName -Status "PASS" -Details "Activated via zCentral. State: $newState" -DurationSec $sw.Elapsed.TotalSeconds
    Write-Log "=== Test $testName completed: PASS ===" "PASS"
}

function Test-OfflineActivation {
    <#
    .SYNOPSIS
        Tests the offline license activation workflow:
        1. Generate a license request file
        2. Place the offline license response file
        3. Import the offline license
        4. Verify the app shows licensed state

    .DESCRIPTION
        Offline activation is used when the test machine has no internet.
        This test simulates the flow by using a pre-generated license file.
    #>

    $testName = "OfflineActivation"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would verify test offline license file exists: $TestOfflineLicenseFile" "DRYRUN"
        Write-Log "[DRYRUN] Would copy license file to: $OfflineLicenseFile" "DRYRUN"
        Write-Log "[DRYRUN] Would run: $LicenseCliTool $OfflineActivationArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify license state changes to '$LicensedStateExpected'" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Pre-check: test license file exists ---
    if (-not (Test-Path $TestOfflineLicenseFile)) {
        Write-Log "Test offline license file not found: $TestOfflineLicenseFile" "ERROR"
        Write-Log "You must pre-generate this file for your test machine's hardware ID." "WARN"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Missing test license: $TestOfflineLicenseFile" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Copy the offline license file into place ---
    Write-Log "Copying offline license to: $OfflineLicenseFile" "INFO"
    try {
        $parentDir = Split-Path $OfflineLicenseFile -Parent
        if (-not (Test-Path $parentDir)) {
            New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
        }
        Copy-Item -Path $TestOfflineLicenseFile -Destination $OfflineLicenseFile -Force
        Write-Log "License file copied" "INFO"
    } catch {
        Write-Log "Failed to copy license file: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Copy failed: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 2: Import the offline license ---
    if (Test-Path $LicenseCliTool) {
        Write-Log "Importing offline license: $LicenseCliTool $OfflineActivationArgs" "INFO"
        try {
            $output = & $LicenseCliTool $OfflineActivationArgs.Split(" ") 2>&1
            Write-Log "Import output: $output" "INFO"
        } catch {
            Write-Log "Import failed: $_" "ERROR"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Import exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
    } else {
        Write-Log "No license CLI tool found. Relying on file placement only." "WARN"
    }

    # --- Step 3: Verify license state ---
    Start-Sleep -Seconds 5
    $newState = Get-LicenseState
    if ($newState -eq $LicensedStateExpected -or $newState -eq "LicenseFilePresent") {
        Write-Log "License state after offline activation: $newState" "PASS"
        Add-TestResult -TestName $testName -Status "PASS" -Details "Offline activation OK. State: $newState" -DurationSec $sw.Elapsed.TotalSeconds
    } else {
        Write-Log "Unexpected license state: $newState" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "State: $newState" -DurationSec $sw.Elapsed.TotalSeconds
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

function Test-LicenseDeactivation {
    <#
    .SYNOPSIS
        Deactivates the license and verifies the app returns to an
        unlicensed state.

    .DESCRIPTION
        WARNING: This will remove the active license from this machine.
        Only run on a test machine with a test license.
    #>

    $testName = "LicenseDeactivation"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would verify app is currently licensed" "DRYRUN"
        Write-Log "[DRYRUN] Would run: $LicenseCliTool $DeactivationArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify license state changes to '$UnlicensedStateExpected'" "DRYRUN"
        Write-Log "[DRYRUN] Would launch app and verify unlicensed behavior" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Extra safety check for deactivation ---
    if (-not $AllowDeactivation) {
        Write-Log "SAFETY: `$AllowDeactivation is `$false. Refusing to deactivate." "WARN"
        Write-Log "Set `$AllowDeactivation = `$true if you want to test license removal." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Deactivation not allowed by safety flag" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Verify currently licensed ---
    $currentState = Get-LicenseState
    if ($currentState -ne $LicensedStateExpected -and $currentState -ne "LicenseFilePresent") {
        Write-Log "App is not currently licensed (state: $currentState). Cannot test deactivation." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not licensed. State: $currentState" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 2: Run deactivation ---
    if (-not (Test-Path $LicenseCliTool)) {
        Write-Log "License CLI not found: $LicenseCliTool" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Missing CLI tool" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    Write-Log "Deactivating license: $LicenseCliTool $DeactivationArgs" "INFO"
    try {
        $output = & $LicenseCliTool $DeactivationArgs.Split(" ") 2>&1
        Write-Log "Deactivation output: $output" "INFO"
    } catch {
        Write-Log "Deactivation exception: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 3: Also remove offline license file if present ---
    if (Test-Path $OfflineLicenseFile) {
        Write-Log "Removing offline license file: $OfflineLicenseFile" "INFO"
        Remove-Item -Path $OfflineLicenseFile -Force -ErrorAction SilentlyContinue
    }

    # --- Step 4: Verify unlicensed state ---
    Start-Sleep -Seconds 5
    $newState = Get-LicenseState
    if ($newState -eq $UnlicensedStateExpected -or $newState -eq "UNKNOWN") {
        Write-Log "License state after deactivation: $newState" "PASS"
        Add-TestResult -TestName $testName -Status "PASS" -Details "Deactivated. State: $newState" -DurationSec $sw.Elapsed.TotalSeconds
    } else {
        Write-Log "License state still shows: $newState" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Still licensed: $newState" -DurationSec $sw.Elapsed.TotalSeconds
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

function Test-NetworkDisabled {
    <#
    .SYNOPSIS
        Disables the network adapter, launches the app, and verifies it
        handles the no-network condition gracefully (no crash, shows
        appropriate message).

    .DESCRIPTION
        WARNING: This test will temporarily disable the network adapter
        specified in $NetworkAdapterName. It will be re-enabled after the
        test completes, even if the test fails.

        The network is restored in a finally block to prevent leaving the
        machine disconnected.
    #>

    $testName = "NetworkDisabled"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would disable network adapter: $NetworkAdapterName" "DRYRUN"
        Write-Log "[DRYRUN] Would launch app: $AppExecutable" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app handles no-network gracefully (no crash)" "DRYRUN"
        Write-Log "[DRYRUN] Would re-enable network adapter: $NetworkAdapterName" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Extra safety check for network changes ---
    if (-not $AllowNetworkChanges) {
        Write-Log "SAFETY: `$AllowNetworkChanges is `$false. Refusing to touch the network." "WARN"
        Write-Log "Set `$AllowNetworkChanges = `$true to enable network tests." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Network changes not allowed by safety flag" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Test-Path $AppExecutable)) {
        Write-Log "App not found: $AppExecutable" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "App not installed" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Verify the adapter exists ---
    $adapter = Get-NetAdapter -Name $NetworkAdapterName -ErrorAction SilentlyContinue
    if (-not $adapter) {
        Write-Log "Network adapter '$NetworkAdapterName' not found." "ERROR"
        Write-Log "Run Get-NetAdapter to see available adapters on this machine." "WARN"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Adapter not found: $NetworkAdapterName" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    try {
        # --- Step 1: Disable the network adapter ---
        Write-Log "Disabling network adapter: $NetworkAdapterName" "WARN"
        Disable-NetAdapter -Name $NetworkAdapterName -Confirm:$false -ErrorAction Stop
        Start-Sleep -Seconds 3

        # Verify it is disabled
        $adapterState = (Get-NetAdapter -Name $NetworkAdapterName).Status
        Write-Log "Adapter status: $adapterState" "INFO"

        # --- Step 2: Launch the app ---
        Write-Log "Launching app with network disabled..." "INFO"
        $appProc = Start-Process -FilePath $AppExecutable -PassThru -ErrorAction Stop
        Start-Sleep -Seconds 15  # Give it time to attempt network operations

        # --- Step 3: Check if app is still running (did not crash) ---
        if ($appProc.HasExited) {
            $exitCode = $appProc.ExitCode
            Write-Log "App exited while network was disabled. Exit code: $exitCode" "WARN"
            # Exit code 0 might be acceptable; non-zero suggests a crash
            if ($exitCode -ne 0) {
                Add-TestResult -TestName $testName -Status "FAIL" -Details "App crashed without network. Exit code: $exitCode" -DurationSec $sw.Elapsed.TotalSeconds
            } else {
                Add-TestResult -TestName $testName -Status "PARTIAL" -Details "App exited gracefully (code 0) without network" -DurationSec $sw.Elapsed.TotalSeconds
            }
        } else {
            Write-Log "App is still running with network disabled (no crash)" "PASS"
            $appProc.Kill()
            $appProc.WaitForExit(10000)
            Add-TestResult -TestName $testName -Status "PASS" -Details "App handled no-network gracefully" -DurationSec $sw.Elapsed.TotalSeconds
        }
    }
    catch {
        Write-Log "Exception during network-disabled test: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
    }
    finally {
        # --- ALWAYS re-enable the network adapter ---
        Write-Log "Re-enabling network adapter: $NetworkAdapterName" "INFO"
        try {
            Enable-NetAdapter -Name $NetworkAdapterName -Confirm:$false -ErrorAction Stop
            Start-Sleep -Seconds 5
            $finalState = (Get-NetAdapter -Name $NetworkAdapterName).Status
            Write-Log "Adapter restored. Status: $finalState" "INFO"
        } catch {
            Write-Log "CRITICAL: Failed to re-enable adapter! Manually enable '$NetworkAdapterName'." "ERROR"
        }
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

function Test-ExpiredLicense {
    <#
    .SYNOPSIS
        Tests how the application behaves when the license has expired.

    .DESCRIPTION
        This test checks the app's behavior with an expired license.
        There are several approaches depending on your licensing system:

        1. Use a pre-expired test license file
        2. Set the system clock forward (risky, affects other processes)
        3. Modify the license expiry in the registry (if possible)

        This implementation uses approach 1 (pre-expired license file).
        Modify as needed for your licensing system.
    #>

    $testName = "ExpiredLicense"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    # TODO: Set the path to a pre-expired license file for testing
    $expiredLicenseFile = "C:\TestData\expired_license.lic"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would back up current license file (if present)" "DRYRUN"
        Write-Log "[DRYRUN] Would place expired license file: $expiredLicenseFile" "DRYRUN"
        Write-Log "[DRYRUN] Would launch app and verify it shows 'license expired' message" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app does not crash" "DRYRUN"
        Write-Log "[DRYRUN] Would restore original license file" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Test-Path $expiredLicenseFile)) {
        Write-Log "Expired license file not found: $expiredLicenseFile" "WARN"
        Write-Log "Create a pre-expired license file and set the path above." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "No expired license file available" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    $backupLicense = $null

    try {
        # --- Step 1: Back up current license ---
        if (Test-Path $OfflineLicenseFile) {
            $backupLicense = "$OfflineLicenseFile.bak"
            Write-Log "Backing up current license to: $backupLicense" "INFO"
            Copy-Item -Path $OfflineLicenseFile -Destination $backupLicense -Force
        }

        # --- Step 2: Place expired license ---
        Write-Log "Placing expired license file" "INFO"
        Copy-Item -Path $expiredLicenseFile -Destination $OfflineLicenseFile -Force

        # --- Step 3: Launch app and check behavior ---
        if (Test-Path $AppExecutable) {
            $appProc = Start-Process -FilePath $AppExecutable -PassThru -ErrorAction Stop
            Start-Sleep -Seconds 15

            if ($appProc.HasExited) {
                Write-Log "App exited with expired license. Exit code: $($appProc.ExitCode)" "INFO"
                # The app might exit with a specific code for expired license
                # TODO: Determine what exit code your app uses for expired license
                Add-TestResult -TestName $testName -Status "PARTIAL" -Details "App exited (code $($appProc.ExitCode)). Manual verification needed." -DurationSec $sw.Elapsed.TotalSeconds
            } else {
                Write-Log "App is running with expired license (checking for degraded mode)" "INFO"
                # TODO: Add checks here to verify the app shows an expiration message
                #       This likely requires UI automation or log file inspection
                $appProc.Kill()
                $appProc.WaitForExit(10000)
                Add-TestResult -TestName $testName -Status "PARTIAL" -Details "App ran with expired license. Manual UI check needed." -DurationSec $sw.Elapsed.TotalSeconds
            }
        } else {
            Write-Log "App executable not found: $AppExecutable" "FAIL"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "App not installed" -DurationSec $sw.Elapsed.TotalSeconds
        }
    }
    catch {
        Write-Log "Exception during expired-license test: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
    }
    finally {
        # --- ALWAYS restore the original license ---
        if ($backupLicense -and (Test-Path $backupLicense)) {
            Write-Log "Restoring original license from backup" "INFO"
            Copy-Item -Path $backupLicense -Destination $OfflineLicenseFile -Force
            Remove-Item -Path $backupLicense -Force -ErrorAction SilentlyContinue
        }
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Log "============================================" "INFO"
Write-Log "  zSpace App Manager - Licensing Tests       " "INFO"
Write-Log "  Prototype #4 - POC QA Automation          " "INFO"
Write-Log "============================================" "INFO"

if ($DryRun) {
    Write-Log "*** DRY RUN MODE *** No license or network changes will be made." "DRYRUN"
}

Write-Log "App Executable:       $AppExecutable" "INFO"
Write-Log "License CLI:          $LicenseCliTool" "INFO"
Write-Log "zCentral URL:         $ZCentralUrl" "INFO"
Write-Log "Is Test Machine:      $IsTestMachine" "INFO"
Write-Log "Allow Deactivation:   $AllowDeactivation" "INFO"
Write-Log "Allow Network Changes: $AllowNetworkChanges" "INFO"
Write-Log "Output Path:          $OutputPath" "INFO"
Write-Log "" "INFO"

# Build the list of test functions
$allTests = @(
    @{ Name = "ZCentralActivation";   Func = { Test-ZCentralActivation } },
    @{ Name = "OfflineActivation";    Func = { Test-OfflineActivation } },
    @{ Name = "LicenseDeactivation";  Func = { Test-LicenseDeactivation } },
    @{ Name = "NetworkDisabled";      Func = { Test-NetworkDisabled } },
    @{ Name = "ExpiredLicense";       Func = { Test-ExpiredLicense } }
)

# Apply the test filter
$testsToRun = $allTests | Where-Object { $_.Name -like $TestFilter }

if ($testsToRun.Count -eq 0) {
    Write-Log "No tests match filter: $TestFilter" "WARN"
} else {
    Write-Log "Running $($testsToRun.Count) test(s) matching filter: $TestFilter" "INFO"
    Write-Log "" "INFO"

    foreach ($test in $testsToRun) {
        & $test.Func
        Write-Log "" "INFO"
    }
}

# ============================================================================
# RESULTS OUTPUT
# ============================================================================

$resultsObject = [PSCustomObject]@{
    RunDate       = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss")
    DryRun        = [bool]$DryRun
    IsTestMachine = $IsTestMachine
    TotalDuration = [math]::Round(((Get-Date) - $script:StartTime).TotalSeconds, 2)
    TestResults   = $script:TestResults
}

$resultsObject | ConvertTo-Json -Depth 5 | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Log "Results written to: $OutputPath" "INFO"

# Print summary
Write-Log "" "INFO"
Write-Log "============================================" "INFO"
Write-Log "  TEST SUMMARY" "INFO"
Write-Log "============================================" "INFO"

$passCount = ($script:TestResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($script:TestResults | Where-Object { $_.Status -eq "FAIL" }).Count
$skipCount = ($script:TestResults | Where-Object { $_.Status -eq "SKIP" }).Count
$dryCount  = ($script:TestResults | Where-Object { $_.Status -eq "DRYRUN" }).Count
$partCount = ($script:TestResults | Where-Object { $_.Status -eq "PARTIAL" }).Count

foreach ($result in $script:TestResults) {
    $icon = switch ($result.Status) {
        "PASS"    { "[PASS]   " }
        "FAIL"    { "[FAIL]   " }
        "SKIP"    { "[SKIP]   " }
        "DRYRUN"  { "[DRYRUN] " }
        "PARTIAL" { "[PARTIAL]" }
        default   { "[????]   " }
    }
    $level = if ($result.Status -eq "FAIL") { "FAIL" }
             elseif ($result.Status -eq "PASS") { "PASS" }
             else { "INFO" }
    Write-Log "$icon $($result.TestName) -- $($result.Details)" $level
}

Write-Log "" "INFO"
Write-Log "PASS: $passCount | FAIL: $failCount | PARTIAL: $partCount | SKIP: $skipCount | DRYRUN: $dryCount" "INFO"
Write-Log "Total duration: $($resultsObject.TotalDuration) seconds" "INFO"

if ($failCount -gt 0) {
    exit 1
}
exit 0

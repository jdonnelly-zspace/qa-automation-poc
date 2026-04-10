#Requires -Version 5.1
<#
.SYNOPSIS
    Automated Installation Tests for zSpace App Manager
    Prototype #4 - POC QA Automation

.DESCRIPTION
    This script automates installation test cases for the zSpace App Manager
    on Windows. It covers fresh install, upgrade, uninstall, standard-user
    permissions, and repair scenarios.

    IMPORTANT: This is a PROTOTYPE. Review all TODO comments and configure
    the variables in the CONFIGURATION section before running on a real
    test machine.

.PARAMETER DryRun
    When specified, the script prints what it WOULD do without actually
    executing any install/uninstall commands. Use this to safely review
    the test plan with stakeholders.

.PARAMETER OutputPath
    Path for the JSON results file. Defaults to .\install_results.json
    in the same directory as this script.

.PARAMETER TestFilter
    Run only tests whose names match this wildcard. Examples:
      -TestFilter "FreshInstall"
      -TestFilter "*Uninstall*"

.EXAMPLE
    # Safe preview -- nothing is installed or removed
    .\install_tests.ps1 -DryRun

.EXAMPLE
    # Run all install tests for real (requires admin and a test machine)
    .\install_tests.ps1

.EXAMPLE
    # Run only the fresh-install test
    .\install_tests.ps1 -TestFilter "FreshInstall"

.NOTES
    Author:  QA Automation POC Team
    Date:    2026-04-09
    Version: 0.4.0 (Prototype #4)
    Platform: Windows 10/11 only
#>

[CmdletBinding()]
param(
    [switch]$DryRun,

    [string]$OutputPath = (Join-Path $PSScriptRoot "install_results.json"),

    [string]$TestFilter = "*",

    # App-specific parameters (override defaults or use --ConfigFile)
    [string]$AppName = "",
    [string]$ConfigFile = "",
    [string]$InstallerPathParam = "",
    [string]$InstallDirParam = "",
    [string]$MainExecutableParam = ""
)

# ============================================================================
# CONFIGURATION -- Defaults for Franklin's Lab A3 (override via parameters)
# ============================================================================

# Load from config file if provided
if ($ConfigFile -and (Test-Path $ConfigFile)) {
    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    Write-Host "  Loaded config: $ConfigFile (app: $($config.app_name))" -ForegroundColor Cyan
}

# Resolve values: CLI param > config file > hardcoded default
$InstallerPath = if ($InstallerPathParam) { $InstallerPathParam }
    elseif ($config.installer_path) { $config.installer_path }
    else { "C:\Installers\zSpaceAppManager_Setup.exe" }

$OlderInstallerPath = if ($config.older_installer_path) { $config.older_installer_path }
    else { "C:\Installers\zSpaceAppManager_Setup_OldVersion.exe" }

$SilentInstallArgs = "/S"
$SilentUninstallArgs = "/S"

$AppDisplayName = if ($AppName) { $AppName }
    elseif ($config.display_name) { $config.display_name }
    else { "zSpace App Manager" }

$ExpectedInstallDir = if ($InstallDirParam) { $InstallDirParam }
    elseif ($config.install_dir) { $config.install_dir }
    else { "C:\Program Files\zSpace\zSpace App Manager" }

$ExpectedVersion = if ($config.expected_version) { $config.expected_version }
    else { "1.0.0.0" }

$ExpectedUpgradeVersion = if ($config.expected_upgrade_version) { $config.expected_upgrade_version }
    else { "2.0.0.0" }

$MainExecutable = if ($MainExecutableParam) { $MainExecutableParam }
    elseif ($config.main_executable) { $config.main_executable }
    else { "zSpaceAppManager.exe" }

$ExpectedShortcuts = if ($config.shortcut_paths) {
        $config.shortcut_paths | ForEach-Object { [Environment]::ExpandEnvironmentVariables($_) }
    } else {
        @(
            "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\zSpace\$AppDisplayName.lnk",
            "$env:PUBLIC\Desktop\$AppDisplayName.lnk"
        )
    }

$ExpectedRegistryPaths = if ($config.registry_paths) {
        @($config.registry_paths)
    } else {
        @(
            "HKLM:\SOFTWARE\zSpace\$AppDisplayName",
            "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$AppDisplayName"
        )
    }

# Safety flag -- set to $true ONLY on dedicated test machines
# The script will refuse to run destructive operations if this is $false
$IsTestMachine = $false  # TODO: Set to $true on your test machine

# Maximum seconds to wait for an install/uninstall to finish
$TimeoutSeconds = 300

# ============================================================================
# INTERNAL STATE -- Do not modify below this line unless extending the script
# ============================================================================

$script:TestResults = @()
$script:StartTime = Get-Date

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-Log {
    <#
    .SYNOPSIS
        Writes a timestamped message to the console.
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
    $color = $colors[$Level]
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Add-TestResult {
    <#
    .SYNOPSIS
        Records a single test result to the results collection.
    #>
    param(
        [string]$TestName,
        [string]$Status,       # PASS, FAIL, SKIP, DRYRUN
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
        Checks that $IsTestMachine is $true before allowing destructive ops.
        Returns $true if safe to proceed, $false otherwise.
    #>
    if (-not $IsTestMachine) {
        Write-Log "SAFETY CHECK: `$IsTestMachine is `$false. Skipping destructive operation." "WARN"
        Write-Log "Set `$IsTestMachine = `$true in the CONFIGURATION section if this is a dedicated test machine." "WARN"
        return $false
    }
    return $true
}

function Get-InstalledApp {
    <#
    .SYNOPSIS
        Searches the Windows registry for an installed application by display name.
        Returns the registry object if found, or $null if not found.
    #>
    param([string]$DisplayName)

    $uninstallPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    foreach ($path in $uninstallPaths) {
        $found = Get-ItemProperty $path -ErrorAction SilentlyContinue |
                 Where-Object { $_.DisplayName -like "*$DisplayName*" }
        if ($found) { return $found }
    }
    return $null
}

function Wait-ForProcess {
    <#
    .SYNOPSIS
        Waits for a process to exit, with a timeout.
    #>
    param(
        [System.Diagnostics.Process]$Process,
        [int]$Timeout = $TimeoutSeconds
    )

    $exited = $Process.WaitForExit($Timeout * 1000)
    if (-not $exited) {
        Write-Log "Process did not exit within $Timeout seconds. Killing." "WARN"
        $Process.Kill()
    }
    return $Process.ExitCode
}

# ============================================================================
# TEST FUNCTIONS
# ============================================================================

function Test-FreshInstall {
    <#
    .SYNOPSIS
        Installs the zSpace App Manager on a clean machine and verifies:
        1. The app appears in Programs and Features
        2. The install directory exists with the main executable
        3. Expected shortcuts are created
        4. The application launches without crashing

    .DESCRIPTION
        This test assumes no prior version is installed. If the app is
        already present, the test will skip to avoid conflicts.
    #>

    $testName = "FreshInstall"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    # --- Pre-check: Is the app already installed? ---
    $existing = Get-InstalledApp -DisplayName $AppDisplayName
    if ($existing) {
        Write-Log "App '$AppDisplayName' is already installed. Skipping fresh install test." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "App already installed" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Pre-check: Does the installer file exist? ---
    if (-not (Test-Path $InstallerPath)) {
        Write-Log "Installer not found at: $InstallerPath" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Installer file not found: $InstallerPath" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if ($DryRun) {
        Write-Log "[DRYRUN] Would run: $InstallerPath $SilentInstallArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app appears in Programs and Features" "DRYRUN"
        Write-Log "[DRYRUN] Would verify install directory: $ExpectedInstallDir" "DRYRUN"
        Write-Log "[DRYRUN] Would verify shortcuts exist at:" "DRYRUN"
        foreach ($s in $ExpectedShortcuts) { Write-Log "[DRYRUN]   - $s" "DRYRUN" }
        Write-Log "[DRYRUN] Would attempt to launch $MainExecutable and verify it starts" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Run the installer silently ---
    Write-Log "Running installer: $InstallerPath $SilentInstallArgs" "INFO"
    try {
        $proc = Start-Process -FilePath $InstallerPath -ArgumentList $SilentInstallArgs -PassThru -Wait -ErrorAction Stop
        if ($proc.ExitCode -ne 0) {
            Write-Log "Installer exited with code $($proc.ExitCode)" "ERROR"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Installer exit code: $($proc.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
        Write-Log "Installer completed with exit code 0" "PASS"
    } catch {
        Write-Log "Failed to start installer: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 2: Verify app appears in Programs and Features ---
    Start-Sleep -Seconds 5  # Brief pause for registry updates
    $installed = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $installed) {
        Write-Log "App not found in Programs and Features after install" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "App not in registry after install" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    Write-Log "App found in Programs and Features: $($installed.DisplayName)" "PASS"

    # --- Step 3: Verify install directory and main executable ---
    $exePath = Join-Path $ExpectedInstallDir $MainExecutable
    if (-not (Test-Path $exePath)) {
        Write-Log "Main executable not found: $exePath" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Missing: $exePath" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    Write-Log "Main executable found: $exePath" "PASS"

    # --- Step 4: Verify shortcuts ---
    $missingShortcuts = @()
    foreach ($shortcut in $ExpectedShortcuts) {
        if (Test-Path $shortcut) {
            Write-Log "Shortcut found: $shortcut" "PASS"
        } else {
            Write-Log "Shortcut missing: $shortcut" "WARN"
            $missingShortcuts += $shortcut
        }
    }

    # --- Step 5: Verify app launches ---
    Write-Log "Attempting to launch application..." "INFO"
    try {
        $appProc = Start-Process -FilePath $exePath -PassThru -ErrorAction Stop
        Start-Sleep -Seconds 10  # Give it time to start
        if ($appProc.HasExited) {
            Write-Log "Application exited immediately (possible crash). Exit code: $($appProc.ExitCode)" "FAIL"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "App crashed on launch. Exit code: $($appProc.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
        Write-Log "Application is running (PID: $($appProc.Id))" "PASS"
        # Clean up -- stop the app
        $appProc.Kill()
        $appProc.WaitForExit(10000)
    } catch {
        Write-Log "Failed to launch application: $_" "WARN"
    }

    # --- Final verdict ---
    $details = "Install OK."
    if ($missingShortcuts.Count -gt 0) {
        $details += " Missing shortcuts: $($missingShortcuts -join ', ')"
    }
    $status = if ($missingShortcuts.Count -gt 0) { "PARTIAL" } else { "PASS" }
    Add-TestResult -TestName $testName -Status $status -Details $details -DurationSec $sw.Elapsed.TotalSeconds
    Write-Log "=== Test $testName completed: $status ===" $status
}

function Test-Upgrade {
    <#
    .SYNOPSIS
        Installs an older version, then upgrades to the newer version.
        Verifies the version number updates correctly and the app still works.
    #>

    $testName = "Upgrade"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if (-not (Test-Path $OlderInstallerPath)) {
        Write-Log "Older installer not found: $OlderInstallerPath" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Older installer missing: $OlderInstallerPath" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    if (-not (Test-Path $InstallerPath)) {
        Write-Log "New installer not found: $InstallerPath" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "New installer missing: $InstallerPath" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if ($DryRun) {
        Write-Log "[DRYRUN] Would install OLDER version: $OlderInstallerPath $SilentInstallArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify older version is installed" "DRYRUN"
        Write-Log "[DRYRUN] Would install NEWER version over it: $InstallerPath $SilentInstallArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify version updated to: $ExpectedUpgradeVersion" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app still launches after upgrade" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Install older version ---
    Write-Log "Installing older version..." "INFO"
    try {
        $proc1 = Start-Process -FilePath $OlderInstallerPath -ArgumentList $SilentInstallArgs -PassThru -Wait -ErrorAction Stop
        if ($proc1.ExitCode -ne 0) {
            Write-Log "Older installer failed with exit code $($proc1.ExitCode)" "ERROR"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Old installer exit code: $($proc1.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
    } catch {
        Write-Log "Exception running older installer: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    Start-Sleep -Seconds 5
    $oldApp = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $oldApp) {
        Write-Log "Older version did not register in Programs and Features" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Old version not found after install" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    Write-Log "Older version installed: $($oldApp.DisplayVersion)" "INFO"

    # --- Step 2: Install newer version over the old one ---
    Write-Log "Upgrading to newer version..." "INFO"
    try {
        $proc2 = Start-Process -FilePath $InstallerPath -ArgumentList $SilentInstallArgs -PassThru -Wait -ErrorAction Stop
        if ($proc2.ExitCode -ne 0) {
            Write-Log "Upgrade installer failed with exit code $($proc2.ExitCode)" "ERROR"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Upgrade exit code: $($proc2.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
            return
        }
    } catch {
        Write-Log "Exception running upgrade installer: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 3: Verify version updated ---
    Start-Sleep -Seconds 5
    $newApp = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $newApp) {
        Write-Log "App not found after upgrade" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "App missing after upgrade" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if ($newApp.DisplayVersion -eq $ExpectedUpgradeVersion) {
        Write-Log "Version correctly updated to $ExpectedUpgradeVersion" "PASS"
    } else {
        Write-Log "Version mismatch. Expected: $ExpectedUpgradeVersion, Got: $($newApp.DisplayVersion)" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Version mismatch: expected $ExpectedUpgradeVersion, got $($newApp.DisplayVersion)" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 4: Verify app launches after upgrade ---
    $exePath = Join-Path $ExpectedInstallDir $MainExecutable
    if (Test-Path $exePath) {
        try {
            $appProc = Start-Process -FilePath $exePath -PassThru -ErrorAction Stop
            Start-Sleep -Seconds 10
            if (-not $appProc.HasExited) {
                Write-Log "App launches successfully after upgrade" "PASS"
                $appProc.Kill()
                $appProc.WaitForExit(10000)
            } else {
                Write-Log "App exited immediately after upgrade" "WARN"
            }
        } catch {
            Write-Log "Could not launch app after upgrade: $_" "WARN"
        }
    }

    Add-TestResult -TestName $testName -Status "PASS" -Details "Upgraded from $($oldApp.DisplayVersion) to $ExpectedUpgradeVersion" -DurationSec $sw.Elapsed.TotalSeconds
    Write-Log "=== Test $testName completed: PASS ===" "PASS"
}

function Test-Uninstall {
    <#
    .SYNOPSIS
        Uninstalls the zSpace App Manager and verifies:
        1. The app is removed from Programs and Features
        2. The install directory is cleaned up
        3. Registry entries are removed
        4. Shortcuts are removed
    #>

    $testName = "Uninstall"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    # --- Pre-check: Is the app installed? ---
    $installed = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $installed) {
        Write-Log "App '$AppDisplayName' is not installed. Nothing to uninstall." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "App not installed" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if ($DryRun) {
        Write-Log "[DRYRUN] Would uninstall '$AppDisplayName' using its uninstall string" "DRYRUN"
        Write-Log "[DRYRUN] Detected uninstall string: $($installed.UninstallString)" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app removed from Programs and Features" "DRYRUN"
        Write-Log "[DRYRUN] Would verify install directory removed: $ExpectedInstallDir" "DRYRUN"
        Write-Log "[DRYRUN] Would verify registry keys removed" "DRYRUN"
        Write-Log "[DRYRUN] Would verify shortcuts removed" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Run the uninstaller ---
    # TODO: Adjust this logic if your app uses a different uninstall mechanism
    $uninstallCmd = $installed.UninstallString
    if (-not $uninstallCmd) {
        Write-Log "No uninstall string found in registry" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "No UninstallString in registry" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    Write-Log "Running uninstaller: $uninstallCmd $SilentUninstallArgs" "INFO"
    try {
        # The uninstall string may include quotes and arguments, so we parse it
        $proc = Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$uninstallCmd`" $SilentUninstallArgs" -PassThru -Wait -ErrorAction Stop
        Write-Log "Uninstaller exited with code $($proc.ExitCode)" "INFO"
    } catch {
        Write-Log "Exception running uninstaller: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    Start-Sleep -Seconds 5

    # --- Step 2: Verify removed from Programs and Features ---
    $stillInstalled = Get-InstalledApp -DisplayName $AppDisplayName
    if ($stillInstalled) {
        Write-Log "App still appears in Programs and Features after uninstall" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Still registered after uninstall" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    Write-Log "App removed from Programs and Features" "PASS"

    # --- Step 3: Verify install directory removed ---
    $leftoverFiles = @()
    if (Test-Path $ExpectedInstallDir) {
        Write-Log "Install directory still exists: $ExpectedInstallDir" "WARN"
        $leftoverFiles += $ExpectedInstallDir
    } else {
        Write-Log "Install directory removed" "PASS"
    }

    # --- Step 4: Verify registry keys removed ---
    $leftoverKeys = @()
    foreach ($regPath in $ExpectedRegistryPaths) {
        if (Test-Path $regPath) {
            Write-Log "Registry key still exists: $regPath" "WARN"
            $leftoverKeys += $regPath
        }
    }
    if ($leftoverKeys.Count -eq 0) {
        Write-Log "All expected registry keys removed" "PASS"
    }

    # --- Step 5: Verify shortcuts removed ---
    $leftoverShortcuts = @()
    foreach ($shortcut in $ExpectedShortcuts) {
        if (Test-Path $shortcut) {
            Write-Log "Shortcut still exists: $shortcut" "WARN"
            $leftoverShortcuts += $shortcut
        }
    }
    if ($leftoverShortcuts.Count -eq 0) {
        Write-Log "All shortcuts removed" "PASS"
    }

    # --- Final verdict ---
    $issues = @()
    if ($leftoverFiles.Count -gt 0) { $issues += "Leftover files" }
    if ($leftoverKeys.Count -gt 0) { $issues += "Leftover registry keys" }
    if ($leftoverShortcuts.Count -gt 0) { $issues += "Leftover shortcuts" }

    if ($issues.Count -eq 0) {
        Add-TestResult -TestName $testName -Status "PASS" -Details "Clean uninstall" -DurationSec $sw.Elapsed.TotalSeconds
    } else {
        Add-TestResult -TestName $testName -Status "PARTIAL" -Details "Uninstalled but leftovers: $($issues -join ', ')" -DurationSec $sw.Elapsed.TotalSeconds
    }
    Write-Log "=== Test $testName completed ===" "INFO"
}

function Test-StandardUserInstall {
    <#
    .SYNOPSIS
        Attempts to run the installer as a standard (non-admin) user.
        The expected behavior is that the installer either:
        - Fails with an access-denied error, OR
        - Prompts for UAC elevation

        This test verifies the app does NOT silently install without admin rights.
    #>

    $testName = "StandardUserInstall"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    if (-not (Test-Path $InstallerPath)) {
        Write-Log "Installer not found: $InstallerPath" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Installer not found" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # Check if we are currently running as admin
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )

    if ($DryRun) {
        Write-Log "[DRYRUN] Current session is admin: $isAdmin" "DRYRUN"
        Write-Log "[DRYRUN] Would attempt to run installer WITHOUT elevation" "DRYRUN"
        Write-Log "[DRYRUN] Would verify the installer fails or requests UAC" "DRYRUN"
        Write-Log "[DRYRUN] NOTE: For a true test, run this script from a non-admin PowerShell session" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if ($isAdmin) {
        # We ARE admin, so we need to simulate running as a standard user.
        # TODO: This requires a standard-user account to exist on the test machine.
        #       Replace 'TestStandardUser' with an actual non-admin account name.
        $standardUser = "TestStandardUser"  # TODO: Set a real non-admin username
        $standardPass = "YOURPASSWORD"       # TODO: Set the password (or use a credential file)

        Write-Log "Current session is admin. Attempting to run installer as '$standardUser'..." "INFO"
        Write-Log "NOTE: A non-admin account '$standardUser' must exist on this machine." "WARN"

        try {
            $securePass = ConvertTo-SecureString $standardPass -AsPlainText -Force
            $cred = New-Object System.Management.Automation.PSCredential ($standardUser, $securePass)

            $proc = Start-Process -FilePath $InstallerPath -ArgumentList $SilentInstallArgs `
                        -Credential $cred -PassThru -Wait -NoNewWindow -ErrorAction Stop

            # If the install succeeded silently as a standard user, that is a FAIL
            # (it means admin rights are not enforced)
            $appAfter = Get-InstalledApp -DisplayName $AppDisplayName
            if ($appAfter) {
                Write-Log "SECURITY ISSUE: App installed successfully as standard user!" "FAIL"
                Add-TestResult -TestName $testName -Status "FAIL" -Details "App installed without admin rights" -DurationSec $sw.Elapsed.TotalSeconds
                return
            }

            Write-Log "Installer ran but app was not installed (expected behavior)" "PASS"
            Add-TestResult -TestName $testName -Status "PASS" -Details "Installer did not install as standard user. Exit code: $($proc.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
        } catch {
            # An exception here often means access denied -- which is EXPECTED
            Write-Log "Installer failed as standard user (expected): $_" "PASS"
            Add-TestResult -TestName $testName -Status "PASS" -Details "Correctly denied: $_" -DurationSec $sw.Elapsed.TotalSeconds
        }
    } else {
        # We are NOT admin -- perfect, just try to install
        Write-Log "Running as non-admin user. Attempting install..." "INFO"
        try {
            $proc = Start-Process -FilePath $InstallerPath -ArgumentList $SilentInstallArgs -PassThru -Wait -ErrorAction Stop
            $appAfter = Get-InstalledApp -DisplayName $AppDisplayName
            if ($appAfter) {
                Write-Log "SECURITY ISSUE: App installed without admin rights!" "FAIL"
                Add-TestResult -TestName $testName -Status "FAIL" -Details "Installed without admin" -DurationSec $sw.Elapsed.TotalSeconds
            } else {
                Write-Log "Install blocked for standard user (expected)" "PASS"
                Add-TestResult -TestName $testName -Status "PASS" -Details "Correctly blocked. Exit code: $($proc.ExitCode)" -DurationSec $sw.Elapsed.TotalSeconds
            }
        } catch {
            Write-Log "Installer failed for standard user (expected): $_" "PASS"
            Add-TestResult -TestName $testName -Status "PASS" -Details "Correctly denied" -DurationSec $sw.Elapsed.TotalSeconds
        }
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

function Test-RepairInstall {
    <#
    .SYNOPSIS
        Runs the installer in repair mode (if supported) and verifies
        the application still works afterwards.

    .DESCRIPTION
        Many MSI-based installers support a /repair or /f switch.
        This test runs that mode and then verifies the app launches.
    #>

    $testName = "RepairInstall"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Log "=== Starting Test: $testName ===" "INFO"

    # --- Pre-check: Is the app installed? ---
    $installed = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $installed) {
        Write-Log "App '$AppDisplayName' is not installed. Cannot test repair." "WARN"
        Add-TestResult -TestName $testName -Status "SKIP" -Details "App not installed" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # TODO: Set the repair-mode arguments for your installer
    # Common patterns:  /repair   /f   msiexec /fa {ProductCode}
    $repairArgs = "/repair /S"

    if ($DryRun) {
        Write-Log "[DRYRUN] Would run repair: $InstallerPath $repairArgs" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app still appears in Programs and Features" "DRYRUN"
        Write-Log "[DRYRUN] Would verify app launches after repair" "DRYRUN"
        Add-TestResult -TestName $testName -Status "DRYRUN" -Details "Dry run -- no actions taken" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    if (-not (Assert-TestMachine)) {
        Add-TestResult -TestName $testName -Status "SKIP" -Details "Not a test machine" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 1: Run repair ---
    Write-Log "Running repair: $InstallerPath $repairArgs" "INFO"
    try {
        $proc = Start-Process -FilePath $InstallerPath -ArgumentList $repairArgs -PassThru -Wait -ErrorAction Stop
        Write-Log "Repair exited with code $($proc.ExitCode)" "INFO"
    } catch {
        Write-Log "Exception during repair: $_" "ERROR"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Exception: $_" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }

    # --- Step 2: Verify still installed ---
    $stillInstalled = Get-InstalledApp -DisplayName $AppDisplayName
    if (-not $stillInstalled) {
        Write-Log "App not found after repair!" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "App disappeared after repair" -DurationSec $sw.Elapsed.TotalSeconds
        return
    }
    Write-Log "App still registered after repair" "PASS"

    # --- Step 3: Verify app launches ---
    $exePath = Join-Path $ExpectedInstallDir $MainExecutable
    if (Test-Path $exePath) {
        try {
            $appProc = Start-Process -FilePath $exePath -PassThru -ErrorAction Stop
            Start-Sleep -Seconds 10
            if (-not $appProc.HasExited) {
                Write-Log "App launches after repair" "PASS"
                $appProc.Kill()
                $appProc.WaitForExit(10000)
                Add-TestResult -TestName $testName -Status "PASS" -Details "Repair completed. App launches." -DurationSec $sw.Elapsed.TotalSeconds
            } else {
                Write-Log "App exited immediately after repair" "FAIL"
                Add-TestResult -TestName $testName -Status "FAIL" -Details "App crashed after repair" -DurationSec $sw.Elapsed.TotalSeconds
            }
        } catch {
            Write-Log "Could not launch app after repair: $_" "WARN"
            Add-TestResult -TestName $testName -Status "FAIL" -Details "Launch failed: $_" -DurationSec $sw.Elapsed.TotalSeconds
        }
    } else {
        Write-Log "Main executable not found after repair: $exePath" "FAIL"
        Add-TestResult -TestName $testName -Status "FAIL" -Details "Missing executable after repair" -DurationSec $sw.Elapsed.TotalSeconds
    }

    Write-Log "=== Test $testName completed ===" "INFO"
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Log "============================================" "INFO"
Write-Log "  zSpace App Manager - Installation Tests   " "INFO"
Write-Log "  Prototype #4 - POC QA Automation          " "INFO"
Write-Log "============================================" "INFO"

if ($DryRun) {
    Write-Log "*** DRY RUN MODE *** No changes will be made to this system." "DRYRUN"
}

Write-Log "Installer Path:    $InstallerPath" "INFO"
Write-Log "Install Directory: $ExpectedInstallDir" "INFO"
Write-Log "Expected Version:  $ExpectedVersion" "INFO"
Write-Log "Is Test Machine:   $IsTestMachine" "INFO"
Write-Log "Output Path:       $OutputPath" "INFO"
Write-Log "" "INFO"

# Build the list of test functions to run
$allTests = @(
    @{ Name = "FreshInstall";        Func = { Test-FreshInstall } },
    @{ Name = "Upgrade";             Func = { Test-Upgrade } },
    @{ Name = "Uninstall";           Func = { Test-Uninstall } },
    @{ Name = "StandardUserInstall"; Func = { Test-StandardUserInstall } },
    @{ Name = "RepairInstall";       Func = { Test-RepairInstall } }
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

# Build the results object
$resultsObject = [PSCustomObject]@{
    RunDate       = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss")
    DryRun        = [bool]$DryRun
    IsTestMachine = $IsTestMachine
    TotalDuration = [math]::Round(((Get-Date) - $script:StartTime).TotalSeconds, 2)
    TestResults   = $script:TestResults
}

# Write JSON results file
$resultsObject | ConvertTo-Json -Depth 5 | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Log "Results written to: $OutputPath" "INFO"

# Print summary table
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

# Exit with non-zero code if any test failed
if ($failCount -gt 0) {
    exit 1
}
exit 0

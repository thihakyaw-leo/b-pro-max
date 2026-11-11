# ui-tests.ps1 — WinUI 3 UI Automation test batch for b-pro-max
#
# Requires: WinApp CLI 0.7+ (winapp), Windows desktop with WinRT runtime registered.
#           Live execution is BLOCKED in this container (REGDB_E_CLASSNOTREG).
#           Run on a Windows host: .\ui-tests.ps1 -Target sandbox
#
# AutomationIds referenced here match src\Views\*.xaml and src\MainWindow.xaml.

param(
    [Parameter(Mandatory)][ValidateSet('sandbox', 'local')][string]$Target,
    [int]$AppPid = 0,
    [ValidateSet('sandbox', 'local')][string]$AppScope,
    [ValidateNotNullOrEmpty()][string]$WorkflowId = [guid]::NewGuid().ToString('N'),
    [string]$ArtifactDirectory = (Join-Path '.\ui-test-artifacts' ([guid]::NewGuid().ToString('N')))
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$ScopeArgs = @(if ($Target -eq 'sandbox') { '--on'; 'sandbox' })
$previousWorkflowId = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = $WorkflowId
$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()
$uiStarted = $false

function Invoke-WinAppChecked {
    param([string[]]$Arguments)
    $global:LASTEXITCODE = $null
    $output = @(& winapp @Arguments)
    $code = $global:LASTEXITCODE
    if ($null -eq $code -or $code -ne 0) {
        throw "winapp $($Arguments -join ' ') failed (exit $code). $($output -join "`n")"
    }
    $output
}

function Invoke-Ui {
    param([string[]]$Arguments, [string]$Window)
    $selector = if ($Window) { @('-w', $Window) } else { @('-a', "$AppPid") }
    Invoke-WinAppChecked (@('ui') + $Arguments + $ScopeArgs + $selector)
}

function Test-UI {
    param([string]$Name, [scriptblock]$Script)
    try {
        & $Script | Out-Null
        $results.Add(@{ name = $Name; status = 'PASS' })
    } catch {
        $results.Add(@{ name = $Name; status = 'FAIL'; detail = "$_" })
    }
}

function Save-Screenshot {
    param([string]$Name)
    $path = Join-Path $ArtifactDirectory $Name
    if (Test-Path -LiteralPath $path) { throw "Choose a new evidence path: $path" }
    Invoke-Ui @('screenshot', '--output', $path) -Window $hwnd | Out-Null
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Screenshot was not delivered to the host: $path"
    }
    $screenshots.Add($path)
}

try {
    $ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
    New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
    if ($AppPid -lt 0) { throw 'AppPid must be positive, or zero to launch.' }
    if ($AppPid -gt 0) {
        if ($AppScope -ne $Target) {
            throw 'Reusing a PID requires matching -AppScope and -Target from the same live target.'
        }
    } else {
        $launch = Invoke-WinAppChecked (@('run', '.') + $ScopeArgs + @('--detach', '--json')) |
            ConvertFrom-Json
        if ($launch.Error -or -not $launch.ProcessId -or [int]$launch.ProcessId -le 0) {
            throw "No app ProcessId. For unpackaged guest runs, discover the app in that target and rerun with -AppPid and -AppScope. $($launch.Error)"
        }
        $AppPid = [int]$launch.ProcessId
        if ($Target -eq 'sandbox' -and
            ($launch.ProcessScope -ne 'sandbox' -or $launch.UiTargetArgs -ne "--on sandbox -a $AppPid")) {
            throw 'Launch response is missing the expected Sandbox ProcessScope / UiTargetArgs pair.'
        }
        if ($Target -eq 'local' -and $launch.ProcessScope -and $launch.ProcessScope -ne 'local') {
            throw 'Launch response belongs to a different target.'
        }
    }

    $uiStarted = $true
    $windows = @(Invoke-Ui @('list-windows', '--json') | ConvertFrom-Json)
    $main = $windows | Where-Object { $_.hwnd -and $_.hwnd -ne '0' -and $_.title -ne 'PopupHost' } |
        Select-Object -First 1
    if (-not $main) { throw 'No app window found in the selected target; rediscover after a fresh guest.' }
    $hwnd = [string]$main.hwnd

    Test-UI 'Initial screenshot delivered' { Save-Screenshot '01-initial.png' }

    # Shell / NavigationView
    Test-UI 'NavigationView present' { Invoke-Ui @('wait-for', 'NavView', '-t', '3000') }
    Test-UI 'ContentFrame present' { Invoke-Ui @('wait-for', 'ContentFrame', '-t', '2000') }

    # Login flow
    Test-UI 'Navigate to Login' {
        Invoke-Ui @('invoke', 'NavLogin')
        Invoke-Ui @('wait-for', 'LoginButton', '-t', '3000')
    }
    Test-UI 'Login UI elements present' {
        Invoke-Ui @('wait-for', 'UsernameBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'PasswordBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'LoginButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'StatusMessage', '-t', '2000')
    }
    Test-UI 'Enter credentials and click Login' {
        Invoke-Ui @('set-value', 'UsernameBox', 'testuser')
        Invoke-Ui @('set-value', 'PasswordBox', 'testpass')
        Invoke-Ui @('invoke', 'LoginButton')
        Invoke-Ui @('wait-for', 'StatusMessage', '-t', '3000')
    }

    # Register flow
    Test-UI 'Navigate to Register' {
        Invoke-Ui @('invoke', 'NavRegister')
        Invoke-Ui @('wait-for', 'RegisterButton', '-t', '3000')
    }
    Test-UI 'Register UI elements present' {
        Invoke-Ui @('wait-for', 'LicenseKeyBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'CompanyBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'EmailBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'RegisterButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'BackButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'StatusMessage', '-t', '2000')
    }

    # Agent list flow (requires login; NavAgents starts disabled)
    Test-UI 'Agent List view accessible' {
        Invoke-Ui @('invoke', 'NavAgents')
        Invoke-Ui @('wait-for', 'AgentList', '-t', '3000')
        Invoke-Ui @('wait-for', 'AgentSearchBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'AgentSearchButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'AgentRefreshButton', '-t', '2000')
    }

    # Sales filter flow
    Test-UI 'Sales Filter view accessible' {
        Invoke-Ui @('invoke', 'NavSales')
        Invoke-Ui @('wait-for', 'SalesFilterButton', '-t', '3000')
        Invoke-Ui @('wait-for', 'SalesResetButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'SalesList', '-t', '2000')
    }

    # AutoBuy flow
    Test-UI 'Auto Buy view accessible' {
        Invoke-Ui @('invoke', 'NavAutoBuy')
        Invoke-Ui @('wait-for', 'AutoBuyCreateButton', '-t', '3000')
        Invoke-Ui @('wait-for', 'AutoBuyEnabledCheck', '-t', '2000')
    }

    # Maintenance flow
    Test-UI 'Maintenance view accessible' {
        Invoke-Ui @('invoke', 'NavMaintenance')
        Invoke-Ui @('wait-for', 'MaintenanceCheckStatusButton', '-t', '3000')
        Invoke-Ui @('wait-for', 'MaintenanceMigrateButton', '-t', '2000')
    }

    # Backup flow
    Test-UI 'Backup view accessible' {
        Invoke-Ui @('invoke', 'NavBackup')
        Invoke-Ui @('wait-for', 'BackupButton', '-t', '3000')
        Invoke-Ui @('wait-for', 'BackupRestoreButton', '-t', '2000')
    }

    # Create database flow
    Test-UI 'Create Database view accessible' {
        Invoke-Ui @('invoke', 'NavCreateDb')
        Invoke-Ui @('wait-for', 'CreateDbButton', '-t', '3000')
    }

    # Compact database flow
    Test-UI 'Compact Database view accessible' {
        Invoke-Ui @('invoke', 'NavCompact')
        Invoke-Ui @('wait-for', 'CompactDbButton', '-t', '3000')
    }

    # Sound settings flow
    Test-UI 'Sound Settings view accessible' {
        Invoke-Ui @('invoke', 'NavSound')
        Invoke-Ui @('wait-for', 'SoundEnabledCheck', '-t', '3000')
        Invoke-Ui @('wait-for', 'SoundVolumeSlider', '-t', '2000')
        Invoke-Ui @('wait-for', 'SoundTestAlertButton', '-t', '2000')
    }

    # Time lock flow
    Test-UI 'Time Lock view accessible' {
        Invoke-Ui @('invoke', 'NavTimeLock')
        Invoke-Ui @('wait-for', 'TimeLockEnabledCheck', '-t', '3000')
        Invoke-Ui @('wait-for', 'TimeLockStartBox', '-t', '2000')
        Invoke-Ui @('wait-for', 'TimeLockEndBox', '-t', '2000')
    }

    # Report flow
    Test-UI 'Reports view accessible' {
        Invoke-Ui @('invoke', 'NavReports')
        Invoke-Ui @('wait-for', 'ReportAgentButton', '-t', '3000')
        Invoke-Ui @('wait-for', 'ReportSalesButton', '-t', '2000')
        Invoke-Ui @('wait-for', 'ReportExportButton', '-t', '2000')
    }

    # Agent search flow
    Test-UI 'Agent Search view accessible' {
        Invoke-Ui @('invoke', 'NavSearch')
        Invoke-Ui @('wait-for', 'AgentSearchTermBox', '-t', '3000')
        Invoke-Ui @('wait-for', 'AgentSearchButton', '-t', '2000')
    }

    # Accessibility: every interactive control has an AutomationId
    Test-UI 'App controls have AutomationIds' {
        $inspection = Invoke-Ui @('inspect', '--interactive', '--json') -Window $hwnd | ConvertFrom-Json
        function Get-Elements($nodes) {
            foreach ($node in $nodes) {
                if ($null -ne $node) { $node; Get-Elements $node.children }
            }
        }
        $allElements = @(Get-Elements @($inspection.windows | ForEach-Object { $_.elements }))
        if (-not $allElements.Count) { throw 'Inspection returned no elements; not an accessibility PASS.' }
        $appElements = @($allElements | Where-Object {
            $_.type -match 'Button|TextBox|ComboBox|CheckBox|ToggleSwitch|TabItem|Edit|List|Slider|CalendarDatePicker|PasswordBox' -and
            $_.name -notmatch 'Minimize|Maximize|Close|System' -and
            $_.className -notmatch 'PickerHost|#32770|CabinetWClass'
        })
        if (-not $appElements.Count) { throw 'No app controls were audited; adjust selectors rather than passing.' }
        $missingId = @($appElements | Where-Object { -not $_.automationId })
        if ($missingId.Count) {
            throw "Missing AutomationIds: $(($missingId | ForEach-Object { "$($_.type) '$($_.name)'" }) -join ', ')"
        }
    }

    Test-UI 'Final screenshot delivered' { Save-Screenshot '02-final.png' }
} catch {
    $results.Add(@{ name = 'Target setup / test execution'; status = 'FAIL'; detail = "$_" })
} finally {
    try {
        if ($uiStarted) { Invoke-WinAppChecked (@('ui', 'yield') + $ScopeArgs) | Out-Null }
    } catch {
        $results.Add(@{ name = 'Release UI workflow'; status = 'FAIL'; detail = "$_" })
    } finally {
        $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflowId
    }
}

$failed = @($results | Where-Object { $_.status -eq 'FAIL' }).Count
$report = [ordered]@{
    target = $Target; appPid = $AppPid; workflowId = $WorkflowId
    passed = $results.Count - $failed; failed = $failed
    results = @($results.ToArray()); screenshots = @($screenshots.ToArray())
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath '.\test-results.json' -Encoding utf8
Write-Host "Passed: $($report.passed) | Failed: $failed | Results: .\test-results.json"
if ($failed) { exit 1 }
exit 0

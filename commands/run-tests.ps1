param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,
    [Parameter(Mandatory = $false)]
    [string]$LogName = "tests.log",
    [Parameter(Mandatory = $false)]
    [string]$TestPlatform = "PlayMode",
    [Parameter(Mandatory = $false)]
    [string]$TestFilter = "",
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 900
)

# Runs Unity PlayMode/EditMode tests in batch mode and prints the results.
# Uses the & call operator (Start-Process mangles -projectPath=X into a single
# quoted token Unity can't parse, which crashed the run with 0x40000015).
# Exit code mirrors Unity's: 0 = all pass, 2 = tests failed, 1 = other error.

$ErrorActionPreference = "Stop"
$Unity = "D:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$FullProject = Join-Path $RepoRoot $ProjectPath
$LogFile = Join-Path $RepoRoot "logs\$LogName"
$Results = Join-Path $RepoRoot "logs\test-results.xml"
$Codecs = Join-Path $RepoRoot "logs\codecs"

Write-Host "Project: $FullProject"
Write-Host "Platform: $TestPlatform  Filter: '$TestFilter'"

$job = Start-Job -ScriptBlock {
    param($unity, $proj, $log, $results, $platform, $filter)
    $a = @('-batchmode', '-nographics',
           '-projectPath', $proj,
           '-logFile', $log,
           '-runTests', '-testPlatform', $platform, '-testResults', $results)
    if ($filter) { $a += @('-testFilter', $filter) }
    & $unity @a
    exit $LASTEXITCODE
} -ArgumentList $Unity, $FullProject, $LogFile, $Results, $TestPlatform, $TestFilter

if (Wait-Job $job -Timeout $TimeoutSeconds) {
    $code = Receive-Job $job
    Write-Host "Unity exit: $code (0=pass 2=test-fail 1=error)"
} else {
    Write-Host "TIMEOUT after ${TimeoutSeconds}s — stopping."
    Stop-Job $job -Force
    Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -eq '' } |
        Stop-Process -Force -ErrorAction SilentlyContinue
    $code = 1
}
Remove-Job $job -Force

if (Test-Path $Results) {
    Write-Host "=== test-results.xml head ==="
    Get-Content $Results -TotalCount 4 | ForEach-Object {
        if ($_.Length -gt 240) { $_.Substring(0, 240) } else { $_ }
    }
} else {
    Write-Host "No test-results.xml produced — check logs\$LogName"
}
exit $code

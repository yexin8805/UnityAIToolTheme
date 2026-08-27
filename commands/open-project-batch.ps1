param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,
    [Parameter(Mandatory = $false)]
    [string]$LogName
)

# Runs Unity in batch mode to import a project (generates .meta files).
# Uses the & call operator so each argument is passed as a distinct token —
# Start-Process mangles "-projectPath <path>" into one quoted argument and
# Unity then treats the path as relative.

$ErrorActionPreference = "Stop"
$Unity = "D:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$FullProject = Join-Path $RepoRoot $ProjectPath
$LogFile = Join-Path $RepoRoot "logs\$LogName"

Write-Host "Unity:   $Unity"
Write-Host "Project: $FullProject"
Write-Host "Log:     $LogFile"

& $Unity -batchmode -nographics -quit -projectPath "$FullProject" -logFile "$LogFile"
Write-Host "Unity exit code: $LASTEXITCODE"
exit $LASTEXITCODE

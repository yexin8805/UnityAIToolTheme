param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,
    [Parameter(Mandatory = $false)]
    [string]$LogName,
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 600
)

# Runs Unity in batch mode WITHOUT -quit so EditorApplication.update keeps
# pumping. Waits for the REAL finish line: the Unity-MCP main assemblies
# appearing in Library/ScriptAssemblies — which only happens after the NuGet
# DLLs are restored AND the UNITY_MCP_READY define is set AND a recompile
# completes. Waiting for McpPlugin.dll alone is not enough (the define is set
# after restore, then needs another domain reload to take effect).

$ErrorActionPreference = "Stop"
$Unity = "D:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$FullProject = Join-Path $RepoRoot $ProjectPath
$LogFile = Join-Path $RepoRoot "logs\$LogName"

Write-Host "Project: $FullProject"
Write-Host "Log:     $LogFile"

$proc = Start-Process -FilePath $Unity `
    -ArgumentList '-batchmode', '-nographics', "-projectPath=$FullProject", "-logFile=$LogFile" `
    -PassThru

$marker = Join-Path $FullProject "Library/ScriptAssemblies/com.IvanMurzak.Unity.MCP.Editor.dll"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$phase = ""
while (-not $proc.HasExited -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 15
    if (Test-Path $marker) {
        Write-Host "MCP.Editor.dll compiled — define + recompile complete."
        break
    }
    # progress heartbeat from the log
    if (Test-Path $LogFile) {
        $last = Get-Content $LogFile -Tail 1 -ErrorAction SilentlyContinue
        if ($last -and $last -ne $phase) { $phase = $last; Write-Host "  ... $phase" }
    }
}

if (-not $proc.HasExited) {
    Write-Host "Stopping Unity..."
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    $proc.WaitForExit(15000) | Out-Null
}

Write-Host "=== ScriptAssemblies ==="
Get-ChildItem (Join-Path $FullProject "Library/ScriptAssemblies") -Filter *.dll -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty Name | ForEach-Object { Write-Host "  $_" }

Write-Host "=== DependencyResolver lines ==="
if (Test-Path $LogFile) {
    Select-String -Path $LogFile -Pattern "DependencyResolver" | Select-Object -Last 10 |
        ForEach-Object { Write-Host "  $($_.Line)" }
}
exit 0

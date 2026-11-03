param([string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'), [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/damage-feedback/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Assets/Script/UI/CleaningBar.cs') -Destination (Join-Path $assets 'CleaningBar.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CleaningBarChecks.cs') -Destination (Join-Path $assets 'Editor/CleaningBarChecks.cs') -Force
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.ugui":"2.5.0","com.unity.modules.audio":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../cleaning-bar-$runId.txt"
$log = Join-Path $verificationRoot "../cleaning-bar-$runId.log"
$previousReport = $env:CLEANING_BAR_CHECK_REPORT
try {
    $env:CLEANING_BAR_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','CleaningBarChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Cleaning bar checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:CLEANING_BAR_CHECK_REPORT = $previousReport }

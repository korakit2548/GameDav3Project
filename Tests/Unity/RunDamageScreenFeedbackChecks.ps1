param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/damage-feedback/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DamageScreenFeedbackChecks.cs') -Destination (Join-Path $assets 'Editor/DamageScreenFeedbackChecks.cs') -Force
$source = Join-Path $ProjectRoot 'Assets/Script/DamageScreenFeedback.cs'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $assets 'DamageScreenFeedback.cs') -Force }
$resources = Join-Path $ProjectRoot 'Assets/Resources/DamageFeedback'
if (Test-Path -LiteralPath $resources) {
    $resourceTarget = Join-Path $assets 'Resources/DamageFeedback'
    New-Item -ItemType Directory -Force -Path $resourceTarget | Out-Null
    Get-ChildItem -LiteralPath $resources -File | Copy-Item -Destination $resourceTarget -Force
}
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.ugui":"2.5.0","com.unity.modules.audio":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../damage-feedback-$runId.txt"
$log = Join-Path $verificationRoot "../damage-feedback-$runId.log"
$previousReport = $env:DAMAGE_FEEDBACK_CHECK_REPORT
try {
    $env:DAMAGE_FEEDBACK_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','DamageScreenFeedbackChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Damage feedback checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:DAMAGE_FEEDBACK_CHECK_REPORT = $previousReport }

param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/footsteps/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TrashDustAudioChecks.cs') -Destination (Join-Path $assets 'Editor/TrashDustAudioChecks.cs') -Force
$source = Join-Path $ProjectRoot 'Assets/Script/TrashDustAudio.cs'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $assets 'TrashDustAudio.cs') -Force }
foreach ($script in @('IInteractable.cs','HoldInteractableItem.cs','CleaningTool.cs')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Script/$script") -Destination (Join-Path $assets $script) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/InteractionPlayerFixture.cs') -Destination (Join-Path $assets 'InteractionPlayerFixture.cs') -Force
foreach ($settings in @('ProjectVersion.txt','TagManager.asset')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "ProjectSettings/$settings") -Destination (Join-Path $verificationRoot "ProjectSettings/$settings") -Force
}
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../dust-audio-$runId.txt"
$log = Join-Path $verificationRoot "../dust-audio-$runId.log"
$previousReport = $env:DUST_AUDIO_CHECK_REPORT
try {
    $env:DUST_AUDIO_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','TrashDustAudioChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Dust audio checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:DUST_AUDIO_CHECK_REPORT = $previousReport }

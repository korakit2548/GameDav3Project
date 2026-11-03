param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/single-house-audio/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $assets 'RandomHouseAudio'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RandomHouseAudioChecks.cs') -Destination (Join-Path $assets 'Editor/RandomHouseAudioChecks.cs') -Force
$source = Join-Path $ProjectRoot 'Assets/Script/RandomHouseAudioEffects.cs'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $assets 'RandomHouseAudioEffects.cs') -Force }
$clips = Join-Path $ProjectRoot 'Assets/Atmosphere/HauntedFilm/Audio/RandomEvents'
if (Test-Path -LiteralPath $clips) { Get-ChildItem -LiteralPath $clips -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $assets 'RandomHouseAudio') -Force } }
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../house-audio-$runId.txt"
$log = Join-Path $verificationRoot "../house-audio-$runId.log"
$previousReport = $env:HOUSE_AUDIO_CHECK_REPORT
try {
    $env:HOUSE_AUDIO_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','RandomHouseAudioChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Random house audio checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:HOUSE_AUDIO_CHECK_REPORT = $previousReport }

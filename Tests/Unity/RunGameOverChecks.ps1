param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/damage-feedback/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $assets 'GameOverFonts'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GameOverChecks.cs') -Destination (Join-Path $assets 'Editor/GameOverChecks.cs') -Force
$source = Join-Path $ProjectRoot 'Assets/Script/UI/GameOverSequence.cs'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $assets 'GameOverSequence.cs') -Force }
foreach ($file in @('GameOverScreenView.cs','GameOverScreenView.cs.meta')) {
    $viewFile = Join-Path $ProjectRoot "Assets/Script/UI/$file"
    if (Test-Path -LiteralPath $viewFile) { Copy-Item -LiteralPath $viewFile -Destination (Join-Path $assets $file) -Force }
}
$authorFile = Join-Path $ProjectRoot 'Assets/Editor/GameOverScreenEditor.cs'
if (Test-Path -LiteralPath $authorFile) { Copy-Item -LiteralPath $authorFile -Destination (Join-Path $assets 'Editor/GameOverScreenEditor.cs') -Force }
foreach ($folder in @('Shaders','Resources')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/TextMesh Pro/$folder") -Destination $assets -Recurse -Force
}
foreach ($file in @('Iannnnn-BAT-Regular SDF.asset','Iannnnn-BAT-Regular SDF.asset.meta','Iannnnn-BAT-Regular.otf','Iannnnn-BAT-Regular.otf.meta')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Assets/UI/$file") -Destination (Join-Path $assets 'GameOverFonts') -Force
}
$resources = Join-Path $ProjectRoot 'Assets/Resources/GameOver'
if (Test-Path -LiteralPath $resources) { Copy-Item -LiteralPath $resources -Destination (Join-Path $assets 'Resources') -Recurse -Force }
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.ugui":"2.5.0","com.unity.modules.audio":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../game-over-$runId.txt"
$log = Join-Path $verificationRoot "../game-over-$runId.log"
$previousReport = $env:GAME_OVER_CHECK_REPORT
try {
    $env:GAME_OVER_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','GameOverChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Game over checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:GAME_OVER_CHECK_REPORT = $previousReport }

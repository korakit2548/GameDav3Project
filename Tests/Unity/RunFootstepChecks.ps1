param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/footsteps/UnityCheck'
$runtimeAssets = Join-Path $verificationRoot 'Assets'
$resourceAssets = Join-Path $runtimeAssets 'Resources/PlayerFootsteps'
New-Item -ItemType Directory -Force -Path (Join-Path $runtimeAssets 'Editor'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings'),(Join-Path $resourceAssets 'Audio') | Out-Null
foreach ($script in 'PlayerFootsteps.cs','FootprintTrail.cs') {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Script/$script") -Destination (Join-Path $runtimeAssets $script) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FootstepEffectsChecks.cs') -Destination (Join-Path $runtimeAssets 'Editor/FootstepEffectsChecks.cs') -Force
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/Resources/PlayerFootsteps/Audio') -File) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $resourceAssets 'Audio') -Force
}
foreach ($file in 'Shoeprint.png','Shoeprint.png.meta','Shoeprint.mat','Shoeprint.mat.meta','ShoeprintGlow.shader','ShoeprintGlow.shader.meta') {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Resources/PlayerFootsteps/$file") -Destination $resourceAssets -Force
}
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../checks-$runId.txt"
$log = Join-Path $verificationRoot "../checks-$runId.log"
$previousReport = $env:FOOTSTEP_CHECK_REPORT
try {
    $env:FOOTSTEP_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','FootstepEffectsChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Unity checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "Unity produced no check report; inspect $log" }
}
finally { $env:FOOTSTEP_CHECK_REPORT = $previousReport }

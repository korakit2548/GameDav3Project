param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$verificationRoot = Join-Path $ProjectRoot '.superpowers/footsteps/UnityCheck'
$assets = Join-Path $verificationRoot 'Assets'
New-Item -ItemType Directory -Force -Path (Join-Path $assets 'Editor'),(Join-Path $assets 'Resources/InteractionOutline'),(Join-Path $verificationRoot 'Packages'),(Join-Path $verificationRoot 'ProjectSettings') | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt') -Force
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/TagManager.asset') -Destination (Join-Path $verificationRoot 'ProjectSettings/TagManager.asset') -Force
Set-Content -LiteralPath (Join-Path $verificationRoot 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'InteractionOutlineChecks.cs') -Destination (Join-Path $assets 'Editor/InteractionOutlineChecks.cs') -Force
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Assets/Script/InteractHighlight.cs') -Destination (Join-Path $assets 'InteractHighlight.cs') -Force
foreach ($script in @('InteractHitbox.cs','IInteractable.cs','HoldInteractableItem.cs','CleaningTool.cs')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Script/$script") -Destination (Join-Path $assets $script) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/InteractionPlayerFixture.cs') -Destination (Join-Path $assets 'InteractionPlayerFixture.cs') -Force
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'Assets/Script/UI/Billboard.cs') -Destination (Join-Path $assets 'Billboard.cs') -Force
$shaders = Join-Path $ProjectRoot 'Assets/Resources/InteractionOutline'
if (Test-Path -LiteralPath $shaders) { Get-ChildItem -LiteralPath $shaders -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $assets 'Resources/InteractionOutline') -Force } }
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $verificationRoot "../outline-$runId.txt"
$log = Join-Path $verificationRoot "../outline-$runId.log"
$previousReport = $env:OUTLINE_CHECK_REPORT
try {
    $env:OUTLINE_CHECK_REPORT = $report
    $arguments = @('-batchmode','-nographics','-projectPath',('"{0}"' -f $verificationRoot),'-executeMethod','InteractionOutlineChecks.Run','-logFile',('"{0}"' -f $log))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0) { throw "Interaction outline checks failed (exit $($process.ExitCode)); inspect $log" }
    if (!(Test-Path -LiteralPath $report)) { throw "No check report; inspect $log" }
}
finally { $env:OUTLINE_CHECK_REPORT = $previousReport }

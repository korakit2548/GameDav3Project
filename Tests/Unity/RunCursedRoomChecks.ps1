param([string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),[string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$testProject = Join-Path $ProjectRoot '.superpowers/damage-feedback/UnityCheck'
foreach ($file in @('Room/Room.cs','CursedTrash.cs','CursedTrashFacing.cs','HoldDestroyableObject.cs','HoldInteractableItem.cs','UI/CleaningBar.cs')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "Assets/Script/$file") -Destination (Join-Path $testProject ('Assets/' + [IO.Path]::GetFileName($file))) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/CursedRoomPlayerFixture.cs') -Destination (Join-Path $testProject 'Assets/CursedRoomPlayerFixture.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CursedRoomChecks.cs') -Destination (Join-Path $testProject 'Assets/Editor/CursedRoomChecks.cs') -Force
Set-Content -LiteralPath (Join-Path $testProject 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.ugui":"2.5.0","com.unity.modules.physics":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $testProject "../cursed-room-$runId.txt"
$log = Join-Path $testProject "../cursed-room-$runId.log"
$old = $env:CURSED_ROOM_CHECK_REPORT
try {
    $env:CURSED_ROOM_CHECK_REPORT = $report
    $process = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-nographics','-projectPath',('"{0}"' -f $testProject),'-executeMethod','CursedRoomChecks.Run','-logFile',('"{0}"' -f $log)) -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) { throw "Cursed room checks failed; inspect $log" }
} finally { $env:CURSED_ROOM_CHECK_REPORT = $old }

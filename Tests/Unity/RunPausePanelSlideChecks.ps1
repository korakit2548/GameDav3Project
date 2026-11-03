param([string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),[string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$testProject = Join-Path $ProjectRoot '.superpowers/damage-feedback/UnityCheck'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PausePanelSlideChecks.cs') -Destination (Join-Path $testProject 'Assets/Editor/PausePanelSlideChecks.cs') -Force
$source = Join-Path $ProjectRoot 'Assets/Script/UI/PausePanelSlide.cs'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $testProject 'Assets/PausePanelSlide.cs') -Force }
$soundFolder = Join-Path $ProjectRoot 'Assets/Resources/PausePanel'
if (Test-Path -LiteralPath $soundFolder) { Copy-Item -LiteralPath $soundFolder -Destination (Join-Path $testProject 'Assets/Resources') -Recurse -Force }
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$report = Join-Path $testProject "../pause-slide-$runId.txt"
$log = Join-Path $testProject "../pause-slide-$runId.log"
$oldReport = $env:PAUSE_SLIDE_CHECK_REPORT
try {
    $env:PAUSE_SLIDE_CHECK_REPORT = $report
    $process = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-nographics','-projectPath',('"{0}"' -f $testProject),'-executeMethod','PausePanelSlideChecks.Run','-logFile',('"{0}"' -f $log)) -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) { throw "Pause sign checks failed; inspect $log" }
} finally { $env:PAUSE_SLIDE_CHECK_REPORT = $oldReport }

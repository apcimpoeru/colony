param(
    [Parameter(Mandatory = $true)]
    [string]$UnityEditor,
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$editorPath = (Resolve-Path -LiteralPath $UnityEditor).Path
$runId = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$resultsDirectory = Join-Path $projectRoot "Logs/TestResults/$runId-$Platform"
$testProject = Join-Path $projectRoot "Temp/StandardTests-$runId"
New-Item -ItemType Directory -Path $resultsDirectory, $testProject -Force | Out-Null

# An isolated copy allows batch tests while the working project is open in Unity.
foreach ($directory in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $directory) -Destination $testProject -Recurse
}
$packageCache = Join-Path $projectRoot 'Library/PackageCache'
if (Test-Path -LiteralPath $packageCache) {
    New-Item -ItemType Directory -Path (Join-Path $testProject 'Library') -Force | Out-Null
    Copy-Item -LiteralPath $packageCache -Destination (Join-Path $testProject 'Library') -Recurse
}

$resultsPath = Join-Path $resultsDirectory 'results.xml'
$logPath = Join-Path $resultsDirectory 'unity.log'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $testProject + '"'),
    '-runTests', '-testPlatform', $Platform, '-testResults', ('"' + $resultsPath + '"'),
    '-logFile', ('"' + $logPath + '"'))
# Do not pass -quit: Unity's test runner exits after saving its results.
Write-Output "Running $Platform tests in $testProject"
$process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if (!(Test-Path -LiteralPath $resultsPath)) {
    throw "Unity exited with code $($process.ExitCode) without test results. See $logPath"
}
[xml]$results = Get-Content -LiteralPath $resultsPath -Raw
$run = $results.'test-run'
Write-Output "Result: $($run.result); passed: $($run.passed); failed: $($run.failed); total: $($run.total)"
Write-Output "Results: $resultsPath"
Write-Output "Unity log: $logPath"
if ($process.ExitCode -ne 0 -or $run.result -ne 'Passed' -or [int]$run.total -eq 0) {
    throw 'Unity test run failed. Inspect the saved results and log.'
}

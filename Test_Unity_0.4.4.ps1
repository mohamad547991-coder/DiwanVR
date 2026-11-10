param([switch]$Batch, [int]$TimeoutSeconds=1200, [string]$UnityExecutable="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe")
$ErrorActionPreference='Stop'
$unityExe=$UnityExecutable
if(-not (Test-Path -LiteralPath $unityExe -PathType Leaf)){throw 'Unity executable not found. Pass -UnityExecutable.'}
$projectDirectory=Join-Path $PSScriptRoot 'UnityProject'
$run=[guid]::NewGuid().ToString('N')
$logDirectory=Join-Path $PSScriptRoot 'TestLogs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath=Join-Path $logDirectory "test-$run.log"
$resultPath=Join-Path $logDirectory "test-$run.json"
$arguments=@('-projectPath',('"'+$projectDirectory+'"'),'-executeMethod','DiwanVR.Editor.QuestValidation042.RunAndExit','-logFile',('"'+$logPath+'"'))
if($Batch){$arguments=@('-batchmode')+$arguments}
$process=Start-Process -FilePath $unityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Started Unity validation, process $($process.Id). Log: $logPath"
$watch=[System.Diagnostics.Stopwatch]::StartNew()
$reason=''
while(-not $process.WaitForExit(1000)) {
    if(Test-Path -LiteralPath $logPath) {
        $log=Get-Content -LiteralPath $logPath -Raw -ErrorAction SilentlyContinue
        if($log -match 'Licensing initialization failed'){$reason='Unity could not initialize licensing.';break}
    }
    if($watch.Elapsed.TotalSeconds -ge $TimeoutSeconds){$reason='Unity validation timed out.';break}
}
if($reason){
    # Only the process launched by this invocation is stopped.
    if(-not $process.HasExited){$process.Kill();$process.WaitForExit()}
}
$log=if(Test-Path -LiteralPath $logPath){Get-Content -LiteralPath $logPath -Raw}else{''}
$passed=($reason -eq '' -and $process.ExitCode -eq 0 -and $log -match 'DIWAN_SUITE_PASSED')
if(-not $passed -and -not $reason){$reason='Unity did not report a completed passing validation.'}
$summary=[ordered]@{runId=$run;passed=$passed;reason=$reason;exitCode=$process.ExitCode;log=$logPath;elapsedSeconds=[math]::Round($watch.Elapsed.TotalSeconds,1);headsetTested=$false}
$summary | ConvertTo-Json | Set-Content -LiteralPath $resultPath -Encoding UTF8
$summary | ConvertTo-Json
if(-not $passed){exit 1}

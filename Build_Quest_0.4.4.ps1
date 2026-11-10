param(
    [string]$UnityExecutable="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe",
    [string]$PythonExecutable='python',
    [int]$TimeoutSeconds=1800
)
$ErrorActionPreference='Stop'
if(-not (Test-Path -LiteralPath $UnityExecutable -PathType Leaf)){throw 'Unity 6000.6.4f1 with Android Build Support is required.'}
$projectDirectory=Join-Path $PSScriptRoot 'UnityProject'
$run=[guid]::NewGuid().ToString('N')
$runDirectory=Join-Path $projectDirectory "Builds\Quest\run-$run"
$logDirectory=Join-Path $projectDirectory 'BuildLogs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath=Join-Path $logDirectory "build-$run.log"
$oldRunId=$env:DIWAN_044_BUILD_RUN_ID
try {
    $env:DIWAN_044_BUILD_RUN_ID=$run
    $arguments=@('-batchmode','-quit','-buildTarget','Android','-projectPath',('"'+$projectDirectory+'"'),'-executeMethod','DiwanVR.Editor.OfficeRelease044.BuildQuest','-logFile',('"'+$logPath+'"'))
    $process=Start-Process -FilePath $UnityExecutable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Host "Building 0.4.4. Log: $logPath"
    $watch=[System.Diagnostics.Stopwatch]::StartNew()
    $reason=''
    while(-not $process.WaitForExit(1000)) {
        $log=if(Test-Path -LiteralPath $logPath){Get-Content -LiteralPath $logPath -Raw -ErrorAction SilentlyContinue}else{''}
        if($log -match 'Licensing initialization failed'){$reason='Unity could not initialize licensing.';break}
        if($watch.Elapsed.TotalSeconds -ge $TimeoutSeconds){$reason='Unity build timed out.';break}
    }
    if($reason){if(-not $process.HasExited){$process.Kill();$process.WaitForExit()};throw $reason}
    if($process.ExitCode -ne 0){throw "Unity failed with exit code $($process.ExitCode). See $logPath"}
    $receiptPath=Join-Path $runDirectory 'receipt.json'
    $apkPath=Join-Path $runDirectory 'DiwanVR_0.4.4_Quest.apk'
    $receipt=Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $apk=Get-Item -LiteralPath $apkPath
    $sha=(Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if($receipt.runId -ne $run -or $receipt.result -ne 'Succeeded' -or $receipt.version -ne '0.4.4' -or $receipt.versionCode -ne 8 -or $receipt.bytes -le 0 -or $receipt.bytes -ne $apk.Length -or $receipt.sha256 -ne $sha){throw 'APK does not match this build receipt.'}
    $install=Join-Path $PSScriptRoot 'Install'
    New-Item -ItemType Directory -Path $install -Force | Out-Null
    Copy-Item -LiteralPath $apkPath -Destination (Join-Path $install 'DiwanVR_0.4.4_Quest.apk') -Force
    Copy-Item -LiteralPath $receiptPath -Destination (Join-Path $install 'DiwanVR_0.4.4_receipt.json') -Force
    $unityData=Join-Path (Split-Path -Parent $UnityExecutable) 'Data'
    & $PythonExecutable (Join-Path $PSScriptRoot 'verify_apk.py') '0.4.4' '--unity-data' $unityData
    if($LASTEXITCODE -ne 0){throw 'APK identity/signature verification failed.'}
    & $PythonExecutable (Join-Path $PSScriptRoot 'release_status.py')
    if($LASTEXITCODE -ne 0){throw 'Release evidence verification failed.'}
    Write-Host "Verified APK: $(Join-Path $install 'DiwanVR_0.4.4_Quest.apk')"
    Write-Host 'Still requires an actual Quest 3S test.'
} finally {$env:DIWAN_044_BUILD_RUN_ID=$oldRunId}

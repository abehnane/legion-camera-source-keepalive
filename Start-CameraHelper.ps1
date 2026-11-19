#Requires -Version 5.1
param([ValidateRange(0,86400)][int]$DurationSeconds=0,[switch]$Wait)
$ErrorActionPreference='Stop'
$executable=Join-Path $PSScriptRoot 'mf_probe\bin\Release\net10.0\mf_probe.exe'
$stateFile=Join-Path $PSScriptRoot 'camera_helper_state.json'
if(-not(Test-Path -LiteralPath $executable)){throw 'Build mf_probe first; its executable is missing.'}
$supervisorLease=$null
if($Wait){
    try{$supervisorLease=[IO.File]::Open((Join-Path $PSScriptRoot 'camera_helper.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read)}
    catch [IO.IOException]{Write-Output 'A camera-helper supervisor is already running.';return}
}
try{
if(Test-Path -LiteralPath $stateFile){
    $oldState=Get-Content -LiteralPath $stateFile -Raw|ConvertFrom-Json
    $oldProcess=Get-Process -Id $oldState.processId -ErrorAction SilentlyContinue
    if($oldProcess -and $oldProcess.Path -eq $executable -and $oldProcess.StartTime.ToUniversalTime().ToString('o') -eq $oldState.processStartUtc){
        Write-Output "Camera helper is already running (PID $($oldState.processId))."
        if($Wait){$oldProcess.WaitForExit()}
        return
    }
}
if(-not $Wait){
    $supervisorArgs='-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "'+$PSCommandPath+'" -Wait -DurationSeconds '+$DurationSeconds
    Start-Process powershell.exe -ArgumentList $supervisorArgs -WindowStyle Hidden|Out-Null
    Write-Output 'Camera-helper supervisor launched. Stop it with Stop-CameraHelper.ps1.'
    return
}
$logDirectory=Join-Path $PSScriptRoot 'helper_logs'
New-Item -ItemType Directory -Path $logDirectory -Force|Out-Null
$stamp=Get-Date -Format yyyyMMdd_HHmmss
$stdout=Join-Path $logDirectory ($stamp+'.jsonl')
$stderr=Join-Path $logDirectory ($stamp+'.stderr.txt')
$process=Start-Process -FilePath $executable -ArgumentList @('keep',$DurationSeconds) -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
@{processId=$process.Id;supervisorId=$PID;executable=$executable;processStartUtc=$process.StartTime.ToUniversalTime().ToString('o');log=$stdout;durationSeconds=$DurationSeconds;recordsVideo=$false}|ConvertTo-Json|Set-Content -LiteralPath $stateFile -Encoding UTF8
Write-Output "Camera helper started (PID $($process.Id)). It holds the integrated-camera source open and records no video."
Write-Output 'Stop it with Stop-CameraHelper.ps1. Duration 0 means until stopped, sign-out or shutdown.'
if($Wait){$process.WaitForExit()}
}finally{if($supervisorLease){$supervisorLease.Dispose()}}

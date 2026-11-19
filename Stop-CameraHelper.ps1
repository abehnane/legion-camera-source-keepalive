#Requires -Version 5.1
$ErrorActionPreference='Stop'
$startupTask=Get-ScheduledTask -TaskName 'IntegratedCameraSourceKeepAlive' -ErrorAction SilentlyContinue
if($startupTask){
    $expectedScript=Join-Path $PSScriptRoot 'Start-CameraHelper.ps1'
    if(@($startupTask.Actions|Where-Object {$_.Arguments -like ('*'+$expectedScript+'*')}).Count){Stop-ScheduledTask -TaskName $startupTask.TaskName -ErrorAction SilentlyContinue}
}
$stateFile=Join-Path $PSScriptRoot 'camera_helper_state.json'
if(-not(Test-Path -LiteralPath $stateFile)){Write-Output 'No camera helper state file exists.';return}
$state=Get-Content -LiteralPath $stateFile -Raw|ConvertFrom-Json
$expected=Join-Path $PSScriptRoot 'mf_probe\bin\Release\net10.0\mf_probe.exe'
$process=Get-Process -Id $state.processId -ErrorAction SilentlyContinue
if($process){
    if($process.Path -ne $expected -or $process.StartTime.ToUniversalTime().ToString('o') -ne $state.processStartUtc){throw 'PID identity differs from the saved helper; no process was stopped.'}
    Stop-Process -Id $process.Id
    Write-Output "Camera helper stopped (PID $($state.processId))."
}else{Write-Output 'Camera helper has already exited.'}

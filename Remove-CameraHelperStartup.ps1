#Requires -Version 5.1
$ErrorActionPreference='Stop'
$admin=([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if(-not $admin){Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'"');return}
$task=Get-ScheduledTask -TaskName 'IntegratedCameraSourceKeepAlive' -ErrorAction SilentlyContinue
if($task){
    $expected=Join-Path $PSScriptRoot 'Start-CameraHelper.ps1'
    if(-not @($task.Actions|Where-Object {$_.Arguments -like ('*'+$expected+'*')}).Count){throw 'Task action does not match this helper; no task was removed.'}
    Stop-ScheduledTask -TaskName $task.TaskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $task.TaskName -Confirm:$false
}
& (Join-Path $PSScriptRoot 'Stop-CameraHelper.ps1')

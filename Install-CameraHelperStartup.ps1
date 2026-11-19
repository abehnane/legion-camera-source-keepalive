#Requires -Version 5.1
#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
$taskName='IntegratedCameraSourceKeepAlive'
$script=Join-Path $PSScriptRoot 'Start-CameraHelper.ps1'
$user=[Security.Principal.WindowsIdentity]::GetCurrent().Name
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($existing -and -not @($existing.Actions|Where-Object {$_.Arguments -like ('*'+$script+'*')}).Count){throw 'An unrelated task already uses this name; no task was changed.'}
$trigger=New-ScheduledTaskTrigger -AtLogOn -User $user
$trigger.Delay='PT5S'
$action=New-ScheduledTaskAction -Execute (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -Argument ('-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "'+$script+'" -Wait') -WorkingDirectory $PSScriptRoot
$principal=New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew -StartWhenAvailable -Hidden
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Keep the integrated camera available by holding a Media Foundation source open. No video is recorded. Runs as the signed-in user; stop with Stop-CameraHelper.ps1.' -Force|Out-Null
New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'helper_logs') -Force|Out-Null
Export-ScheduledTask -TaskName $taskName|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'helper_logs\startup_task.xml') -Encoding UTF8
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 3
Get-ScheduledTask -TaskName $taskName|Select-Object TaskName,State

#Requires -Version 5.1
# Read-only checks: no capture, service restart, or driver changes.
$ErrorActionPreference='Stop'
$task=Get-ScheduledTask -TaskName 'IntegratedCameraSourceKeepAlive' -ErrorAction SilentlyContinue
$expectedScript=Join-Path $PSScriptRoot 'Start-CameraHelper.ps1'
$ownsTask=[bool]@($task.Actions|Where-Object {$_.Arguments -like ('*'+$expectedScript+'*')}).Count
$workerVerified=$false
$statePath=Join-Path $PSScriptRoot 'camera_helper_state.json'
if(Test-Path -LiteralPath $statePath){
    $state=Get-Content -LiteralPath $statePath -Raw|ConvertFrom-Json
    $worker=Get-Process -Id $state.processId -ErrorAction SilentlyContinue
    $expectedExe=Join-Path $PSScriptRoot 'mf_probe\bin\Release\net10.0\mf_probe.exe'
    if($worker){$workerVerified=$worker.Path -eq $expectedExe -and $worker.StartTime.ToUniversalTime().ToString('o') -eq $state.processStartUtc}
}
$cameras=@(Get-PnpDevice -PresentOnly -Class Camera -ErrorAction SilentlyContinue|Where-Object InstanceId -match '^USB\\VID_13D3&PID_56FF&MI_00\\')
$drivers=@(foreach($camera in $cameras){
    $properties=@(Get-PnpDeviceProperty -InstanceId $camera.InstanceId -KeyName DEVPKEY_Device_DriverProvider,DEVPKEY_Device_DriverInfPath,DEVPKEY_Device_DriverVersion)
    [pscustomobject]@{Provider=($properties|Where-Object KeyName -eq 'DEVPKEY_Device_DriverProvider').Data;Inf=($properties|Where-Object KeyName -eq 'DEVPKEY_Device_DriverInfPath').Data;Version=($properties|Where-Object KeyName -eq 'DEVPKEY_Device_DriverVersion').Data}
})
[pscustomobject]@{
    TaskExists=[bool]$task
    TaskRunning=([bool]$task -and $task.State -eq 'Running')
    TaskTargetsThisCheckout=$ownsTask
    KeeperIdentityVerified=$workerVerified
    MatchingPhysicalCamerasPresent=$cameras.Count
    DriverSummary=$drivers
    UsesTestedMicrosoftDriver=($drivers.Count -gt 0 -and @($drivers|Where-Object {$_.Provider -ne 'Microsoft' -or $_.Inf -ne 'usbvideo.inf'}).Count -eq 0)
    VisualPreviewCheck='Confirm a normal live image manually; these checks cannot certify image quality or permanent recovery.'
}|Format-List

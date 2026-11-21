#Requires -Version 5.1
$ErrorActionPreference='Stop'
function Test-RunningInstalledHelper {
    # A preview launched from a second checkout should reuse the installed keeper.
    try{
        $task=Get-ScheduledTask -TaskName IntegratedCameraSourceKeepAlive -ErrorAction Stop
        if($task.State -ne 'Running'){return $false}
        foreach($action in $task.Actions){
            if($action.Arguments -notmatch '(?i)-File\s+"([^"]+\\Start-CameraHelper\.ps1)"'){continue}
            $folder=Split-Path -Parent $Matches[1]
            $state=Get-Content -LiteralPath (Join-Path $folder 'camera_helper_state.json') -Raw -ErrorAction Stop|ConvertFrom-Json
            $worker=Get-Process -Id $state.processId -ErrorAction Stop
            $expected=Join-Path $folder 'mf_probe\bin\Release\net10.0\mf_probe.exe'
            if($worker.Path -eq $expected -and $worker.StartTime.ToUniversalTime().ToString('o') -eq $state.processStartUtc){return $true}
        }
    }catch{}
    return $false
}
if(Test-RunningInstalledHelper){Write-Output 'Using the verified camera helper already running from the installed checkout.'}
else{& (Join-Path $PSScriptRoot 'Start-CameraHelper.ps1')}
try{
    foreach($name in @('WindowsCamera','CamoStudio','camera_preview')){
        foreach($process in @(Get-Process -Name $name -ErrorAction SilentlyContinue)){
            Stop-Process -Id $process.Id -ErrorAction Stop
            [void]$process.WaitForExit(5000)
        }
    }
}catch{
    $admin=([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if(-not $admin){Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'"');return}
    throw
}
Start-Sleep -Milliseconds 700
Start-Process -FilePath (Join-Path $PSScriptRoot 'camera_preview\bin\Release\net10.0-windows\camera_preview.exe')

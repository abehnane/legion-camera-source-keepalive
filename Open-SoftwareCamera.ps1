#Requires -Version 5.1
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Start-CameraHelper.ps1')
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

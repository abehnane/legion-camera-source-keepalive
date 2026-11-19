#Requires -Version 5.1
$ErrorActionPreference='Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.build\dotnet-home'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.build\nuget'
$env:NUGET_HTTP_CACHE_PATH=Join-Path $PSScriptRoot '.build\nuget-http'
foreach($project in @('mf_probe\mf_probe.csproj','camera_preview\camera_preview.csproj')){
    & dotnet build (Join-Path $PSScriptRoot $project) -c Release --nologo
    if($LASTEXITCODE -ne 0){throw "Build failed: $project"}
}
Write-Output 'Built the source holder and CPU preview. Build does not change drivers or install startup tasks.'

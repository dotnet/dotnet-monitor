[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $BarId,
    [Parameter(Mandatory=$true)][string] $MaestroToken,
    [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro-prod.westus2.cloudapp.azure.com',
    [Parameter(Mandatory=$false)][string] $MaestroApiVersion = '2020-02-20',
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null,
    [Parameter(Mandatory=$false)][switch] $IncludeV
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

[array]$releaseData = Invoke-RestMethod `
    -Uri "$MaestroApiEndPoint/api/assets?buildId=$BarId&name=dotnet-monitor&api-version=$MaestroApiVersion" `
    -Method 'GET' `
    -Headers @{ 'accept' = 'application/json'; 'Authorization' = "Bearer $MaestroToken" }

Write-Verbose 'ReleaseData:'
$releaseDataJson = $releaseData | ConvertTo-Json
Write-Verbose $releaseDataJson

if ($releaseData.Length -ne 1) {
    Write-Error 'Unable to obtain release version'
}

$version = $releaseData[0].Version
if ($IncludeV) {
    $version = "v$version"
}

Write-Verbose "Release Version: $version"

if ($TaskVariableName) {
    & $PSScriptRoot\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $version
}

Write-Output $version
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $BarId,
    [Parameter(Mandatory=$true)][string] $MaestroToken,
    [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro-prod.westus2.cloudapp.azure.com',
    [Parameter(Mandatory=$false)][string] $MaestroApiVersion = '2020-02-20',
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

[array]$releaseData = Invoke-RestMethod `
    -Uri "$MaestroApiEndPoint/api/assets?buildId=$BarId&api-version=$MaestroApiVersion" `
    -Method 'GET' `
    -Headers @{ 'accept' = 'application/json'; 'Authorization' = "Bearer $MaestroToken" }

Write-Verbose 'ReleaseData:'
$releaseDataJson = $releaseData | ConvertTo-Json
Write-Verbose $releaseDataJson

[array]$matchingData = $releaseData | Where-Object { $_.name -match 'MergedManifest.xml$' -and $_.nonShipping -ieq 'true' }

if ($matchingData.Length -ne 1) {
    Write-Error 'Unable to obtain build version.'
}

$version = $matchingData[0].Version

Write-Verbose "Build Version: $version"

if ($TaskVariableName) {
    & $PSScriptRoot\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $version
}

Write-Output $version
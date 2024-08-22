[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $BarId,
    [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro.dot.net',
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null,
    [Parameter(Mandatory=$false)][string] $DarcVersion = $null,
    [Parameter(Mandatory=$false)][switch] $IncludeV
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$buildData = & $PSScriptRoot\GetDarcBuild.ps1 `
    -BarId $BarId `
    -MaestroApiEndPoint $MaestroApiEndPoint `
    -DarcVersion $DarcVersion

[array]$matchingData = $buildData.assets | Where-Object { $_.name -match '^dotnet-monitor$' }

if (!$matchingData -or $matchingData.Length -ne 1) {
    Write-Error 'Unable to obtain release version'
}

$version = $matchingData[0].version
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
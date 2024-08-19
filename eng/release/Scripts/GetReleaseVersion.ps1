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

$ci = $true
$darc = $null
try {
    $darc = (Get-Command darc).Source
}
catch {
    . $PSScriptRoot\..\..\common\tools.ps1
    $darc = Get-Darc $DarcVersion
}

[string]$buildDataJson = & $darc get-build `
    --id "$BarId" `
    --extended `
    --output-format json `
    --bar-uri "$MaestroApiEndPoint" `
    --ci

Write-Verbose 'BuildData:'
Write-Verbose $buildDataJson
$buildData = $buildDataJson | ConvertFrom-Json

if ($buildData.Length -ne 1) {
    Write-Error 'Unable to obtain build data.'
}

[array]$matchingData = $buildData[0].assets | Where-Object { $_.name -match '^dotnet-monitor$' }

if ($matchingData.Length -ne 1) {
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
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $BarId,
    [Parameter(Mandatory=$false)][string] $MaestroApiEndPoint = 'https://maestro.dot.net',
    [Parameter(Mandatory=$false)][string] $DarcVersion = $null
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

if (!$buildData -or $buildData.Length -ne 1) {
    Write-Error 'Unable to obtain build data.'
}

Write-Output  $buildData[0]
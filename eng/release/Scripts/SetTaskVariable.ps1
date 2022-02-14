param(
    [Parameter(Mandatory=$true)][string] $Name,
    [Parameter(Mandatory=$false)][string] $Value
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'
Set-StrictMode -Version 2.0

Write-Host "##vso[task.setvariable variable=$Name]$Value"
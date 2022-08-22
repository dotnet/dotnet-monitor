[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $ToolsDirectory,
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$url = 'https://github.com/microsoft/vswhere/releases/download/3.0.3/vswhere.exe'
$basePath = Join-Path $ToolsDirectory 'vswhere'
$vsWherePath = Join-Path $basePath 'vswhere.exe'

if (Test-Path $vsWherePath) {
    Write-Verbose 'Already installed'
} else {
    if (!(Test-Path $basePath)) {
        New-Item -ItemType 'Directory' -Path $basePath | Out-Null
    }

    Write-Verbose 'Fetching...'
    Invoke-WebRequest -Uri $url -OutFile $vsWherePath

    Write-Verbose 'Finished'
}

if ($TaskVariableName) {
    & $PSScriptRoot\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $vsWherePath
}

Write-Output $vsWherePath
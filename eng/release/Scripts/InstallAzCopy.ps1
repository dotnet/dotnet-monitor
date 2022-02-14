[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $ToolsDirectory,
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$url = 'https://aka.ms/downloadazcopy-v10-windows'
$basePath = Join-Path $ToolsDirectory 'azcopy'

$zipPath = Join-Path $basePath 'azcopy.zip'
$toolDirPath = Join-Path $basePath 'azcopy'
$azCopyPath = Join-Path $toolDirPath 'azcopy.exe'

if (Test-Path $azCopyPath) {
    Write-Verbose 'Already installed'
} else {
    if (!(Test-Path $basePath)) {
        New-Item -ItemType 'Directory' -Path $basePath | Out-Null
    }

    Write-Verbose 'Fetching...'
    Invoke-WebRequest -Uri $url -OutFile $zipPath

    Write-Verbose 'Unzipping...'
    Expand-Archive -LiteralPath $zipPath -Force -DestinationPath $basePath

    # There should only be one directory that is named like 'azcopy_windows_amd64_<version>'
    Write-Verbose 'Renaming...'
    $unpackDirName = Get-ChildItem -Path $basePath -Directory -Name
    $unpackDirPath = Join-Path $basePath $unpackDirName
    Rename-Item -Path $unpackDirPath -NewName 'azcopy'

    # Delete zip
    Remove-Item -Path $zipPath

    Write-Verbose 'Finished'
}

if ($TaskVariableName) {
    & $PSScriptRoot\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $azCopyPath
}

Write-Output $azCopyPath
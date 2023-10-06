[CmdletBinding(PositionalBinding=$false)]
Param(
    [Parameter(Mandatory=$true)][ValidateSet("x86","x64","arm64")][string]$Architecture,
    [Parameter(Mandatory=$true)][string] $Version,
    [Parameter(Mandatory=$false)][string] $Mirror = "https://nodejs.org/dist",
    [Parameter(Mandatory=$true)][string] $DestinationFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($IsMacOS) {
    $nodePlatform = "darwin"
    $nodeExt = "tar.gz"
} elseif ($IsLinux) {
    $nodePlatform = "linux"
    $nodeExt = "tar.gz"
} elseif ($IsWindows) {
    $nodePlatform = "win"
    $nodeExt = "zip"
} else {
    Write-Error "Unknown OS"
    exit 1;
}

if (Test-Path -Path $DestinationFolder) {
    Write-Output "$DestinationFolder already exists, skipping"
    exit 0
}

$nodeName = "node-$Version-$nodePlatform-$Architecture"
$nodeUrl = "$Mirror/$Version/$nodeName.$nodeExt"
Write-Verbose "URL: $nodeUrl"

$tempFolderPath = Join-Path $([System.IO.Path]::GetTempPath()) $(New-Guid)
Write-Verbose "Temp path: $tempFolderPath"

New-Item -ItemType Directory -Force -Path $tempFolderPath

$archiveFile = Join-Path $tempFolderPath "$nodeName.$nodeExt"
Write-Verbose "Archive file: $archiveFile"

Invoke-WebRequest $nodeUrl -OutFile $archiveFile -MaximumRetryCount 5
if ($IsWindows) {
    Expand-Archive $archiveFile -DestinationPath $tempFolderPath
    Move-Item -Path $(Join-Path $tempFolderPath $nodeName) -Destination $DestinationFolder
} else {
    New-Item -ItemType Directory -Force -Path $DestinationFolder
    & tar --strip-components 1 -xzf "$archiveFile" --no-same-owner --directory "$DestinationFolder"
}

Remove-Item -Recurse -Force $tempFolderPath
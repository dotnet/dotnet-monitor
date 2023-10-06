[CmdletBinding(PositionalBinding=$false)]
Param(
    [Parameter(Mandatory=$true)][int] $MajorVersion,
    [Parameter(Mandatory=$false)][string] $Mirror = "https://nodejs.org/dist",
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$availableVersions = Invoke-WebRequest "$Mirror/index.json" -MaximumRetryCount 5 | ConvertFrom-Json
$latestMatchingVersion = $null
foreach ($entry in $availableVersions) {
    if ($entry.version.StartsWith("v$MajorVersion.")) {
        $latestMatchingVersion=$entry.version
        break
    }
}

if ($latestMatchingVersion -eq $null) {
    Write-Error "Could not find matching version"
    exit 1;
}

if ($TaskVariableName) {
    & $PSScriptRoot\..\release\Scripts\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $latestMatchingVersion
}

Write-Output $latestMatchingVersion
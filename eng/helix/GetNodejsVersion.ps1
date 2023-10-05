[CmdletBinding(PositionalBinding=$false)]
Param(
    [ValidateSet("x86","x64","arm64")][string][Alias('a', "platform")]$architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant(),
    [int] $MajorVersion,
    [string] $Mirror = "https://nodejs.org/dist",
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$availableVersion = Invoke-WebRequest "$Mirror/index.json" -MaximumRetryCount 5 | ConvertFrom-Json
$latestMatchingVersion = $null
foreach ($verObj in $availableVersion) {
    if ($verObj.version.StartsWith("v$MajorVersion.")) {
        $latestMatchingVersion=$verObj.version
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
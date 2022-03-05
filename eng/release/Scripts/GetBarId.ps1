[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)][string] $BuildId,
    [Parameter(Mandatory=$false)][string] $TaskVariableName = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

if ([String]::IsNullOrEmpty($env:System_AccessToken)) {
    Write-Error 'System access token missing, this script needs access.'
}

$tagsUri = "${env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI}${env:SYSTEM_TEAMPROJECT}/_apis/build/builds/$BuildId/tags?api-version=6.0"
$buildData = Invoke-RestMethod `
    -Uri $tagsUri `
    -Method 'GET' `
    -Headers @{ 'accept' = 'application/json'; 'Authorization' = "Bearer ${env:System_AccessToken}" }

Write-Verbose 'BuildData:'
$buildDataJson = $buildData | ConvertTo-Json
Write-Verbose $buildDataJson

$barId = -1;
$buildData.Value | Foreach-Object {
    if ($_.StartsWith('BAR ID - ')) {
        if ($barId -ne -1) {
            Write-Error 'Multiple BAR IDs found in tags.'
        }
        $barId = $_.SubString(9)
    }
}

if ($barId -eq -1) {
    Write-Error 'Failed to get BAR ID from tags.'
}

Write-Verbose "BAR ID: $barId"

if ($TaskVariableName) {
    & $PSScriptRoot\SetTaskVariable.ps1 `
        -Name $TaskVariableName `
        -Value $barId
}

Write-Output $barId
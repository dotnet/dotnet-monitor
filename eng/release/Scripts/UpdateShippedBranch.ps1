param(
  [Parameter(Mandatory=$true)][string] $ReleaseVersion,
  [Parameter(Mandatory=$true)][string] $ReleaseCommit
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0


if ($ReleaseVersion -notmatch '^(?<majorMinorVersion>v[0-9]+\.[0-9]+)\.+') {
    Write-Host "Error: unexpected release version: $ReleaseVersion"
    exit 1
}

$majorMinorVersion = $Matches.majorMinorVersion
$branchName = "shipped/$majorMinorVersion"

git push --force origin "$ReleaseCommit`:refs/heads/$branchName"

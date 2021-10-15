[CmdletBinding()]
param
(
    [ValidateNotNullOrEmpty()]
    [string]
    $DiagRepoRoot = "$PSScriptRoot\..\diagnostics"
)

$ErrorActionPreference = 'Stop'

$resolvedPath = Resolve-Path $DiagRepoRoot
$env:DIAGNOSTICS_REPO_ROOT=$resolvedPath

#Generates a solution that spans both the diagnostics and the dotnet-monitor repo.
#This can be used to build both projects in VS.

$devSln = "$PSScriptRoot\dotnet-monitor.dev.sln"

Copy-Item "$PSScriptRoot\dotnet-monitor.sln" $devSln -Force
& dotnet sln $devSln add "$env:DIAGNOSTICS_REPO_ROOT\src\Microsoft.Diagnostics.Monitoring\Microsoft.Diagnostics.Monitoring.csproj"
if ($LASTEXITCODE -gt 0)
{
    exit $LASTEXITCODE
}
& dotnet sln $devSln add "$env:DIAGNOSTICS_REPO_ROOT\src\Microsoft.Diagnostics.Monitoring.EventPipe\Microsoft.Diagnostics.Monitoring.EventPipe.csproj"
if ($LASTEXITCODE -gt 0)
{
    exit $LASTEXITCODE
}
& dotnet sln $devSln add "$env:DIAGNOSTICS_REPO_ROOT\src\Microsoft.Diagnostics.NETCore.Client\Microsoft.Diagnostics.NETCore.Client.csproj"
if ($LASTEXITCODE -gt 0)
{
    exit $LASTEXITCODE
}
$slnFile = Get-Content $devSln

#dotnet sln uses an older ProjectType Guid
$slnFile -replace 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC', '9A19103F-16F7-4668-BE54-9A1E7A4F7556' | Out-File $devSln

$devenvPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -property productPath
& $devenvPath $PSScriptRoot\dotnet-monitor.dev.sln
[CmdletBinding(PositionalBinding=$false)]
Param(
    [ValidateSet("x86","x64","arm","arm64")][string][Alias('a', "platform")]$architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant(),
    [ValidateSet("Debug","Release")][string][Alias('c')] $configuration = "Debug",
    [string][Alias('v')] $verbosity = "minimal",
    [switch][Alias('t')] $test,
    [string] $testgroup = 'All',
    [switch] $ci,
    [switch] $skipmanaged,
    [switch] $skipnative,
    [switch] $archive,
    [string] $runtimesourcefeed = '',
    [string] $runtimesourcefeedkey = '',
    [Parameter(ValueFromRemainingArguments=$true)][String[]] $remainingargs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$crossbuild = $false
if (($architecture -eq "arm") -or ($architecture -eq "arm64")) {
    $processor = @([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant())
    if ($architecture -ne $processor) {
        $crossbuild = $true
    }
}

switch ($configuration.ToLower()) {
    { $_ -eq "debug" } { $configuration = "Debug" }
    { $_ -eq "release" } { $configuration = "Release" }
}

$reporoot = Join-Path $PSScriptRoot ".."
$engroot = Join-Path $reporoot "eng"
$artifactsdir = Join-Path $reporoot "artifacts"
$logdir = Join-Path $artifactsdir "log"
$logdir = Join-Path $logdir $configuration

if ($ci) {
    $remainingargs = "-ci " + $remainingargs
}

$managedArgs = "/p:PackageRid=win-$architecture"
if ($runtimesourcefeed) {
    $managedArgs = $managedArgs + " /p:DotNetRuntimeSourceFeed=$runtimesourcefeed"
}
if ($runtimesourcefeedkey) {
    $managedArgs = $managedArgs + " /p:DotNetRuntimeSourceFeedKey=$runtimesourcefeedkey"
}

# Build native components
if (-not $skipnative) {
    Invoke-Expression "& `"$engroot\Build-Native.cmd`" -architecture $architecture -configuration $configuration -verbosity $verbosity $remainingargs"
    if ($lastExitCode -ne 0) {
        exit $lastExitCode
    }
}

# Install sdk for building, restore and build managed components.
if (-not $skipmanaged) {
    Invoke-Expression "& `"$engroot\common\build.ps1`" -build -configuration $configuration -verbosity $verbosity /p:BuildArch=$architecture $managedArgs $remainingargs"
    if ($lastExitCode -ne 0) {
        exit $lastExitCode
    }
}

# Create archives
if ($archive) {
    Invoke-Expression "& `"$engroot\common\build.ps1`" -build -configuration $configuration -verbosity $verbosity /p:BuildArch=$architecture -nobl /bl:$logDir\Archive.binlog /p:CreateArchives=true $managedArgs $remainingargs"
    if ($lastExitCode -ne 0) {
        exit $lastExitCode
    }
}

# Run the xunit tests
if ($test) {
    Invoke-Expression "& `"$engroot\common\build.ps1`" -test -configuration $configuration -verbosity $verbosity /p:BuildArch=$architecture -nobl /bl:$logdir\Test.binlog /p:TestGroup=$testgroup $managedArgs $remainingargs"
    if ($lastExitCode -ne 0) {
        exit $lastExitCode
    }
}

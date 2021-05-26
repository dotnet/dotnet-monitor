# Official Build Instructions

> *WARNING*: These instructions will only work internally at Microsoft.

This signs and publishes the following packages to the tools feed (https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json):
 - dotnet-monitor
 - Microsoft.Diagnostics.NETCore.Client

## To release the latest tools:

1. Merge the desired commits for this release from the master branch to the release branch.
2. Kick off an official build in the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) for the desired branch after the changes have been properly mirrored.
3. Change all the package version references as needed in any documentation if needed.
4. Download the above packages from the successful official build under "Artifacts" -> "PackageArtifacts".
5. Upload these packages to NuGet.org.
6. Create a new "release" in the [releases](https://github.com/dotnet/dotnet-monitor/releases) dotnet-monitor repo release tab with the package version (not the official build id) as the "tag". Add any release notes about known issues, issues fixed and new features.

# Official Build Instructions

> **Warning**: These instructions will only work internally at Microsoft.

To produce an official build, invoke the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954).

This signs and publishes the following packages to the [dotnet-tools](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json) feed:
 - dotnet-monitor

The packages are only published to the feed from builds of the `main` and `release/*` branches.

The release process is documented at [Release Process](./release-process.md).

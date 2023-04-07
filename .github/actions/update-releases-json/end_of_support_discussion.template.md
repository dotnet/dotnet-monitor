On ${endOfSupportDate}, the ${majorMinorVersion}.X versions of `dotnet monitor` will be out of support. After this date, we will no longer provide any bug or security fixes for these versions.

Please refer to [Releases](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/releases.md) to see the currently supported versions.

Please update to a newer version in order to remain in support using one of the following:

## .NET Tool

This will update the installation to the most recent release of `dotnet monitor`:

```
dotnet tool update -g dotnet-monitor
```

## Docker Image

Use the Docker image name without specifying a tag to get the most recent release of `dotnet monitor`:

```
docker pull mcr.microsoft.com/dotnet/monitor
```
# Setup

`dotnet monitor` is available via two different distribution mechanisms:

- As a .NET Core global tool; and
- As a container image available via the Microsoft Container Registry (MCR)

### .NET Core global tool

The `dotnet monitor` global tool requires a .NET 3.1 or newer SDK installed as a pre-requisite. If you do not have a new enough SDK, you can install a new one from the [Download .NET webpage](https://dotnet.microsoft.com/download).

You can download the latest version using the following command:

```cmd
dotnet tool install -g dotnet-monitor
```

If you already have `dotnet monitor` installed and want to update:

```cmd
dotnet tool update -g dotnet-monitor
```

### Container image

You can pull the latest image using the following command:

```cmd
docker pull mcr.microsoft.com/dotnet/monitor
```

### Working with CI builds

In addition to public previews, we also publish last-known-good (LKG) builds for the next release of `dotnet monitor`.

The LKG build of `dotnet monitor` is available on a private package feed. You can download the latest prerelease version of the .NET global tool using the following command:

```cmd
dotnet tool install -g dotnet-monitor --add-source https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet-tools/nuget/v3/index.json --prerelease
```

If you already have `dotnet monitor` installed and want to update:

```cmd
dotnet tool update -g dotnet-monitor --add-source https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet-tools/nuget/v3/index.json --prerelease
```

The LKG build of `dotnet monitor` is also available as a container image. You can pull the image using the following command:

```cmd
docker pull mcr.microsoft.com/dotnet/nightly/monitor
```

### Official Releases and Previews

The official releases and previews are:
- Documented on the [Releases](./releases.md) page.
- Announced on the GitHub [Releases](https://github.com/dotnet/dotnet-monitor/releases) tab.
- Published to [nuget.org](https://www.nuget.org/packages/dotnet-monitor/).
- Published to the Microsoft Container Registry ([tag list](https://mcr.microsoft.com/v2/dotnet/monitor/tags/list)).

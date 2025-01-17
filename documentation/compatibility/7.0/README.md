# Breaking Changes in 7.0

If you are migrating your usage to `dotnet monitor` 7.0, the following changes might affect you. Changes are grouped together by areas within the tool.

## Changes

| Area | Title | Introduced |
|--|--|--|
| Deployment | [The tool will not run on .NET Core 3.1 or .NET 5 due to removal of `netcoreapp3.1` target framework](#deployment-removal-of-netcoreapp31-tfm); **Note**: The tool will still be able to monitor applications running these .NET versions. | Preview 1 |
| Docker | [Docker container entrypoint has been split among entrypoint and cmd instructions](#docker-container-entrypoint-split) | Preview 3 |
| Egress | [Built-in metadata keys for Azure Blob egress now prefixed with `DotnetMonitor_`](#egress-built-in-metadata-is-prefixed-for-azure-blob-egress) | Preview 8 |

## Details

### Deployment: Removal of `netcoreapp3.1` TFM

The `dotnet monitor` tool no longer runs on .NET Core 3.1 or .NET 5 due to the removal of the `netcoreapp3.1` target framework. The tool requires a shared SDK installation of either .NET 6 SDK or .NET 7 SDK to install and a shared framework installation of either ASP.NET 6 or ASP.NET 7 in order to run. However, it will still be able to monitor applications that run on .NET Core 3.1, .NET 5, .NET 6, and .NET 7.

### Docker: Container Entrypoint Split

In `dotnet monitor` 6, the Docker containers have the following execution instructions:

```docker
ENTRYPOINT [ "dotnet-monitor", "collect", "--urls", "https://+:52323", "--metricUrls", "http://+:52325" ]
```

While these settings made it easy to apply additional command line arguments via `CMD` (such as `--no-auth`), it made it difficult to override the existing command line arguments without respecifying the `ENTRYPOINT` instruction.

In `dotnet monitor` 7, the executable arguments were moved to the `CMD` instruction; the Docker containers have the following execution instructions:

```docker
ENTRYPOINT [ "dotnet-monitor" ]
CMD [ "collect", "--urls", "https://+:52323", "--metricUrls", "http://+:52325" ]
```

When overriding the `CMD` instruction, the existing arguments will need to be repeated or replaced with the desired values in addition to any additional arguments that need to be passed.

### Egress: Built-In Metadata Is Prefixed for Azure Blob Egress

When egressing an artifact to Azure blob storage, several metadata keys and values are applied to the Azure blob, such as `dotnet monitor` activity information, collection rule execution information, and process information. Starting in 7.0, these metadata keys are now prefixed with `DotnetMonitor_`. This change was made in order to avoid potential collisions with metadata acquired from the `metadata` description on the [Azure blob storage](../../configuration/egress-configuration.md#azure-blob-storage-egress-provider) egress provider. The names are changed as follows:

- `ParentId` -> `DotnetMonitor_ParentId`
- `SpanId` -> `DotnetMonitor_SpanId`
- `TraceId` -> `DotnetMonitor_TraceId`
- `CollectionRuleName` -> `DotnetMonitor_CollectionRuleName`
- `ActionListIndex` -> `DotnetMonitor_ActionListIndex`
- `ActionName` -> `DotnetMonitor_ActionName`
- `ArtifactSource_ProcessId` -> `DotnetMonitor_ArtifactSource_ProcessId`
- `ArtifactSource_RuntimeInstanceCookie` -> `DotnetMonitor_ArtifactSource_RuntimeInstanceCookie`

> [!NOTE]
> The custom metadata keys as specified in the [Azure blob storage](../../configuration/egress-configuration.md#azure-blob-storage-egress-provider) egress provider are not prefixed.

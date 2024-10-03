# Egress Providers

`dotnet monitor` supports configuration of [egress providers](./configuration/egress-configuration.md) that can be used to egress artifacts externally, instead of to the client. This is supported for dumps, gcdumps, traces, logs, and live metrics. Currently supported providers are Azure blob storage and filesystem.

Egress providers must first be named and configured in `dotnet monitor` configuration. They can then be referenced from a request, and will cause an egress based on the provider configuration, rather than directly back to the client.

Egress providers use [operations](./api/operations.md) to provide status.

> [!NOTE]
> The filesystem provider can be used to egress to [kubernetes volumes](https://kubernetes.io/docs/concepts/storage/volumes/).

### Disabling HTTP Egress

The `--no-http-egress` flag requires users to specify an egress provider by preventing the default HTTP response for logs, traces, dumps, gcdumps, and live metrics.

## Examples of Egressing a dump to blob storage

### Sample Request
```http
GET /dump?egressProvider=monitorBlob HTTP/1.1
```

### Sample Response
```http
HTTP/1.1 202 Accepted
Location: https://localhost:52323/operations/26e74e52-0a16-4e84-84bb-27f904bfaf85
```

## Egress Extensibility (8.0+)

Starting with `dotnet monitor` 8, the tool includes an egress extensibility model that allows additional egress providers to be discovered and usable by a `dotnet monitor` installation. The existing `AzureBlobStorage` egress provider has been moved to this model and remains as an available egress provider in the .NET Tool and `mcr.microsoft.com/dotnet/monitor` image offerings.

In addition to the current `dotnet monitor` offerings, a `monitor-base` image is now available; this image does not include egress providers (with the exception of `FileSystem` egress), allowing users to only include their preferred egress providers. For convenience, the `monitor` image and the nuget package will include all of `dotnet monitor`'s supported extensions.

### Manually Installing Supported Extensions

Users using the `monitor-base` image can manually install supported extensions via Multi-Stage Docker Builds, creating their own image that includes any desired egress providers.

For an example of using Multi-Stage Docker Builds, see the [Dockerfile](https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/8.0/ubuntu-chiseled/amd64/Dockerfile) that the `dotnet monitor` team uses to construct the `amd64` `monitor` image.

To directly access archives for one of `dotnet monitor`'s supported extensions, these are available using the following link (this example is specifically for the `linux-x64` archive): `https://dotnetbuilds.azureedge.net/public/diagnostics/monitor/$dotnet_monitor_extension_version/dotnet-monitor-egress-azureblobstorage-$dotnet_monitor_extension_version-linux-x64.tar.gz`.
> [!NOTE]
> The versions in this link will change in response to new `dotnet monitor` releases; as a result, the link will need to be changed to reflect the most recent version when updating your extensions.

#### Example Of Manually Installing AzureBlobStorage Extension Locally (Version Numbers May Vary)

1. Download the archive: `curl -fSL --output dotnet-monitor-egress-azureblobstorage.tar.gz https://dotnetbuilds.azureedge.net/public/diagnostics/monitor/8.0.0-preview.4.23260.4/dotnet-monitor-egress-azureblobstorage-8.0.0-preview.4.23260.4-linux-x64.tar.gz`
2. Extract the contents of `dotnet-monitor-egress-azureblobstorage.tar.gz`
   * Example (Linux): `tar -xvzf dotnet-monitor-egress-azureblobstorage.tar.gz`
4. Place the `AzureBlobStorage` directory found inside the archive in one of `dotnet monitor`'s designated extension [locations](./learningPath/egress.md#well-known-egress-provider-locations).
   * Example (Linux): Place `AzureBlobStorage` at `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`.
5. `dotnet monitor` will now automatically detect the extension - ensure you have added the necessary configuration for the extension (configuration for extensions is written alongside the rest of `dotnet monitor` configuration).

> `dotnet monitor` may expand the acquisition model for extensions in the future; if you have a scenario that requires additional installation options, please let us know by creating a [discussion](https://github.com/dotnet/dotnet-monitor/discussions).

### Third-Party Egress Extensions

The extensibility model allows users to utilize third-party egress providers with `dotnet monitor`. **Note that `dotnet monitor` does not endorse or guarantee any third-party extensions - users should always exercise caution when providing sensitive authentication information (such as passwords) to untrusted extensions.** Third-party egress extensions are responsible for their own distribution, and are not included with `dotnet monitor`.

For more information on how to create your own egress extension, see our [learning path](../documentation/learningPath/).

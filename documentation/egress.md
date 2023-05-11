
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fegress)

# Egress Providers

`dotnet monitor` supports configuration of [egress providers](./configuration/egress-configuration.md) that can be used to egress artifacts externally, instead of to the client. This is supported for dumps, gcdumps, traces, logs, and live metrics. Currently supported providers are Azure blob storage and filesystem. 

Egress providers must first be named and configured in `dotnet monitor` configuration. They can then be referenced from a request, and will cause an egress based on the provider configuration, rather than directly back to the client.

Egress providers use [operations](./api/operations.md) to provide status.

> **Note**: The filesystem provider can be used to egress to [kubernetes volumes](https://kubernetes.io/docs/concepts/storage/volumes/).

### Disabling HTTP Egress

The `--no-http-egress` flag requires users to specify an egress provider by preventing the default HTTP response for logs, traces, dumps, gcdumps, and live metrics.

## Egress Extensibility (8.0+)

The `dotnet monitor` tool has transitioned to an extensible egress model. **This should not be a breaking change - by default, users migrating from `dotnet monitor` 6/7 should see no difference in `dotnet monitor`'s behavior**. 

In addition to the current `dotnet monitor` offerings, a `monitor-base` image is now available; this image does not include egress providers (with the exception of `FileSystem` egress), allowing users to only include their preferred egress providers. For convenience, the `monitor` image and the nuget package will include all of `dotnet monitor`'s supported extensions.

### Manually Installing Supported Extensions

Users using the `monitor-base` image can manually install supported extensions via Multi-Stage Docker Builds, creating their own image that includes any desired egress providers. 


> `dotnet monitor` may expand the acquisition model for extensions in the future; if you have a scenario that requires additional installation options, please let us know by creating a [discussion](https://github.com/dotnet/dotnet-monitor/discussions).

### Third-Party Egress Extensions

The extensibility model allows users to utilize third-party egress providers with `dotnet monitor`. **Note that `dotnet monitor` does not endorse or guarantee any third-party extensions - users should always exercise caution when providing sensitive authentication information (such as passwords) to untrusted extensions.** Third-party egress extensions are responsible for their own distribution, and are not included with `dotnet monitor`.

For more information on how to create your own egress extension, see our [learning path](../documentation/learningPath/).

## Examples of Egressing a dump to blob storage

### Sample Request
```http
GET /dump?egressProvider=monitorBlob HTTP/1.1
```

### Sample Response
```http
HTTP/1.1 202 Accepted
Location: https://localhost:52323/operations/26e74e52-0a16-4e84-84bb-27f904bfaf85

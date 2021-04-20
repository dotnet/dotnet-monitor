# Egress Providers

Dotnet-monitor supports configuration of [egress providers](./configuration.md#EgressConfiguration) that can be used to egress artifacts externally, instead of to the client. This is supported for dumps, gcdumps, traces, and logs. Currently supported providers are Azure blob storage and filesystem. 

Egress providers must first be named and configured in dotnet-monitor configuration. They can then be referenced from a request, and will cause an egress based on the provider configuration, rather than directly back to the client.

> **NOTE:** The filesystem provider can be used to egress to [kubernetes volumes](https://kubernetes.io/docs/concepts/storage/volumes/).

## Examples of Egressing a dump to blob storage

### Sample Request
```http
GET /dump/?egressProvider=monitorBlob HTTP/1.1
```

### Sample Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
{"uri":"https://examplestorage.blob.core.windows.net/dotnet-monitor/artifacts%2Fcore_20210419_231615"}

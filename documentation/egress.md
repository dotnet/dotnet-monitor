
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fegress)

# Egress Providers

`dotnet monitor` supports configuration of [egress providers](./configuration/egress-configuration.md) that can be used to egress artifacts externally, instead of to the client. This is supported for dumps, gcdumps, traces, logs, and live metrics. Currently supported providers are Azure blob storage and filesystem. 

Egress providers must first be named and configured in `dotnet monitor` configuration. They can then be referenced from a request, and will cause an egress based on the provider configuration, rather than directly back to the client.

Egress providers use [operations](./api/operations.md) to provide status.

> **Note**: The filesystem provider can be used to egress to [kubernetes volumes](https://kubernetes.io/docs/concepts/storage/volumes/).

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

testing!

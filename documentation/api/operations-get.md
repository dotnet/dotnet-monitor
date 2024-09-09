# Operations - Get

Gets detailed information about a specific operation.

## HTTP Route

```http
GET /operations/{operationId} HTTP/1.1
```

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [OperationStatus](./definitions.md#operationstatus) | Detailed status of the operation | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](./definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /operations/67f07e40-5cca-4709-9062-26302c484f18 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "resourceLocation": "https://example.blob.core.windows.net/dotnet-monitor/artifacts%2Fcore_20210721_062115",
    "error": null,
    "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
    "createdDateTime": "2021-07-21T06:21:15.315861Z",
    "status": "Succeeded",
    "egressProviderName": "monitorBlob",
    "isStoppable": true,
    "process":
    {
        "pid":1,
        "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
        "name":"dotnet-monitor-demo"
    },
    "tags": []
}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

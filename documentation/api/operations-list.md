# Operations - List

Lists all operations that have been created, as well as their status.

## HTTP Route

```http
GET /operations HTTP/1.1
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
| 200 OK | [OperationSummary](./definitions.md#operationsummary)[] | An array of operation objects. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](./definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /operations HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
    {
        "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
        "createdDateTime": "2021-07-21T06:21:15.315861Z",
        "status": "Succeeded", 
        "process":
        {
            "pid":1,
            "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
            "name":"dotnet-monitor-demo"
        }
    },
    {
        "operationId": "26e74e52-0a16-4e84-84bb-27f904bfaf85",
        "createdDateTime": "2021-07-21T23:30:22.3058272Z",
        "status": "Failed", 
        "process":
        {
            "pid":11782,
            "uid":"23c289b3-b5ce-428a-aaa8-c864b3766bc2",
            "name":"dotnet-monitor-demo2"
        }
    }
]
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |
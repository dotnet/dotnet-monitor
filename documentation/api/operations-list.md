# Operations - List

Lists all operations that have been created, as well as their status.

## HTTP Route

```http
GET /operations?pid={pid}&uid={uid}&name={name}&tags={tags} HTTP/1.1
```

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | query | false | int | (6.3+) The ID of the process. |
| `uid` | query | false | guid | (6.3+) A value that uniquely identifies a runtime instance within a process. |
| `name` | query | false | string | (6.3+) The name of the process. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, all operations will be listed.

> [!NOTE]
> If multiple processes match the provided parameters (e.g., two processes named "MyProcess"), the operations for all matching processes will be listed.

> [!NOTE]
> An operation must include all of the provided tags to be shown in the results (e.g., tags=tag1,tag2 only includes operations with tag1 and tag2, not one or the other).

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

### Sample Request 1

```http
GET /operations HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response 1

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
    {
        "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
        "createdDateTime": "2021-07-21T06:21:15.315861Z",
        "status": "Succeeded",
        "egressProviderName": "monitorBlob",
        "isStoppable": false,
        "process":
        {
            "pid":1,
            "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
            "name":"dotnet-monitor-demo"
        },
        "tags": []
    },
    {
        "operationId": "06ac07e2-f7cd-45ad-80c6-e38160bc5881",
        "createdDateTime": "2021-07-21T20:22:15.315861Z",
        "status": "Stopping",
        "egressProviderName": null,
        "isStoppable": false,
        "process":
        {
            "pid":1,
            "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
            "name":"dotnet-monitor-demo"
        },
        "tags": [
            "tag1",
            "tag2"
        ]
    },
    {
        "operationId": "26e74e52-0a16-4e84-84bb-27f904bfaf85",
        "createdDateTime": "2021-07-21T23:30:22.3058272Z",
        "status": "Failed",
        "egressProviderName": "monitorBlob",
        "isStoppable": false,
        "process":
        {
            "pid":11782,
            "uid":"23c289b3-b5ce-428a-aaa8-c864b3766bc2",
            "name":"dotnet-monitor-demo2"
        },
        "tags": []
    }
]
```

### Sample Request 2

```http
GET /operations?name=dotnet-monitor-demo HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response 2

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
    {
        "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
        "createdDateTime": "2021-07-21T06:21:15.315861Z",
        "status": "Succeeded",
        "egressProviderName": "monitorBlob",
        "isStoppable": false,
        "process":
        {
            "pid":1,
            "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
            "name":"dotnet-monitor-demo"
        },
        "tags": []
    }
]
```

### Sample Request 3

```http
GET /operations?tags=tag1 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response 3

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
    {
        "operationId": "06ac07e2-f7cd-45ad-80c6-e38160bc5881",
        "createdDateTime": "2021-07-21T20:22:15.315861Z",
        "status": "Stopping",
        "egressProviderName": null,
        "isStoppable": false,
        "process":
        {
            "pid":1,
            "uid":"95b0202a-4ed3-44a6-98f1-767d270ec783",
            "name":"dotnet-monitor-demo"
        },
        "tags": [
            "tag1",
            "tag2"
        ]
    }
]
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

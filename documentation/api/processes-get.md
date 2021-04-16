# Processes - Get

Gets detailed information about a specified process.

```http
GET /processes/{pid} HTTP/1.1
```

or 

```http
GET /processes/{uid} HTTP/1.1
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | path | true | int | The ID of the process. |
| `uid` | path | true | guid | A value that uniquely identifies a runtime instance within a process. |

See [ProcessIdentifier](definitions.md#ProcessIdentifier) for more details about these parameters.

Either `pid` or `uid` are required, but not both.

## Authentication

Allowed schemes:
- `MonitorApiKey`
- `Negotiate` (Windows only, running as unelevated)

See [Authentication](./../authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [ProcessInfo](definitions.md#ProcessInfo) | The detailed information about the specified process. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /processes/21632 HTTP/1.1
Host: localhost:52323
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET /processes/cd4da319-fa9e-4987-ac4e-e57b2aac248b HTTP/1.1
Host: localhost:52323
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "pid": 21632,
    "uid": "cd4da319-fa9e-4987-ac4e-e57b2aac248b",
    "name": "dotnet",
    "commandLine": "\"C:\\Program Files\\dotnet\\dotnet.exe\" ConsoleApp1.dll",
    "operatingSystem": "Windows",
    "processArchitecture": "x64"
}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.
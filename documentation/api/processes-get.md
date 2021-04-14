# Processes - Get

Gets detailed information about a specified process.

```http
GET https://localhost:52323/processes/{pid}
```

or 

```http
GET https://localhost:52323/processes/{uid}
```

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

See [Authentication](authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [ProcessInfo](definitions.md#ProcessInfo) | The detailed information about the specified process. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET https://localhost:52323/processes/21632
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET https://localhost:52323/processes/cd4da319-fa9e-4987-ac4e-e57b2aac248b
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

```json
{
    "pid": 21632,
    "uid": "cd4da319-fa9e-4987-ac4e-e57b2aac248b",
    "name": "dotnet",
    "commandLine": "\"C:\\Program Files\\dotnet\\dotnet.exe\" ConsoleApp1.dll",
    "operatingSystem": "Windows",
    "processArchitecture": "x64"
}
```
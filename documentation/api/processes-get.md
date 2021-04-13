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

See [ProcessIdentifier](processes-list.md#ProcessIdentifier) for more details about these parameters.

Either `pid` or `uid` are required, but not both.

## Responses

| Name | Type | Description |
|---|---|---|
| 200 OK | [ProcessInfo](#ProcessInfo) | The process information |
| 400 Bad Request | ValidationProblemDetails |  |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](authentication.md) for further information. |

## Examples

### Sample Request

```http
GET https://localhost:52323/processes/21632
```

or

```http
GET https://localhost:52323/processes/cd4da319-fa9e-4987-ac4e-e57b2aac248b
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

## Definitions

### ProcessInfo

Object with detailed information about the specified process.

Some properties will have non-null values for procesess that are running on .NET 5 or newer (denoted with `.NET 5+`). These properties will be null for runtime versions prior to .NET 5.

| Name | Type | Description |
|---|---|---|
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` A 'null' value: `00000000-0000-0000-0000-000000000000` |
| `name` | string | The name of the process. |
| `commandLine` | string | The command line of the process (includes process name and arguments) |
| `operatingSystem` | string | `.NET 5+` The operating system on which the process is running (e.g. `windows`, `linux`, `macos`).<br/>`.NET Core 3.1` A value of `null`. |
| `processArchitecture` | string | `.NET 5+` The architecture of the process (e.g. `x64`, `x86`).<br/>`.NET Core 3.1` A value of `null`. |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).
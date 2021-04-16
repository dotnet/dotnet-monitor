# Processes - Get Environment

Gets the environment block of a specified process.

```http
GET /processes/{pid}/env HTTP/1.1
```

or 

```http
GET /processes/{uid}/env HTTP/1.1
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
| 200 OK | map (of string) | The environment block of the specified process. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /processes/21632/env HTTP/1.1
Host: localhost:52323
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET /processes/cd4da319-fa9e-4987-ac4e-e57b2aac248b/env HTTP/1.1
Host: localhost:52323
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "ALLUSERSPROFILE": "C:\\ProgramData",
    "APPDATA": "C:\\Users\\user\\AppData\\Roaming",
    "CommonProgramFiles": "C:\\Program Files\\Common Files",
    "CommonProgramFiles(x86)": "C:\\Program Files (x86)\\Common Files",
    "CommonProgramW6432": "C:\\Program Files\\Common Files",
    "ComSpec": "C:\\WINDOWS\\system32\\cmd.exe",
    "DriverData": "C:\\Windows\\System32\\Drivers\\DriverData",
    "HOMEDRIVE": "C:",
    "HOMEPATH": "\\Users\\user",
    "LOCALAPPDATA": "C:\\Users\\user\\AppData\\Local",
    "NUMBER_OF_PROCESSORS": "8",
    "OS": "Windows_NT",
    "Path": "...",
    "PROCESSOR_ARCHITECTURE": "AMD64",
    "ProgramData": "C:\\ProgramData",
    "ProgramFiles": "C:\\Program Files",
    "ProgramFiles(x86)": "C:\\Program Files (x86)",
    "ProgramW6432": "C:\\Program Files",
    "PUBLIC": "C:\\Users\\Public",
    "SESSIONNAME": "Console",
    "SystemDrive": "C:",
    "SystemRoot": "C:\\WINDOWS",
    "TEMP": "C:\\Users\\user\\AppData\\Local\\Temp",
    "TMP": "C:\\Users\\user\\AppData\\Local\\Temp",
    "USERNAME": "user",
    "USERPROFILE": "C:\\Users\\user",
    "windir": "C:\\WINDOWS"
}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 5+ |
| Linux | .NET 5+ |
| MacOS | .NET 5+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.
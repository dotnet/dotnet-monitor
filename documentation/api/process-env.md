# Processes - Get Environment

Gets the environment block of a specified process.

## HTTP Route

```http
GET /env?pid={pid}&uid={uid}&name={name} HTTP/1.1
```

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | query | false | int | The ID of the process. |
| `uid` | query | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `name` | query | false | string | The name of the process. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, the environment block of the [default process](defaultprocess.md) will be provided. Attempting to get the environment block of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | map (of string) | The environment block of the specified process. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /env?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

or

```http
GET /env?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
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

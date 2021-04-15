# Dump - Get

Captures a managed dump of a specified process without using a debugger.

## HTTP Route

```http
GET https://localhost:52323/dump/{pid}?type={type}
```

or 

```http
GET https://localhost:52323/dump/{uid}?type={type}
```

or

```http
GET https://localhost:52323/dump?type={type}
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | path | false | int | The ID of the process. |
| `uid` | path | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `type` | query | false | [DumpType](definitions.md#DumpType) | The type of dump to capture. Default value is `WithHeap` |

See [ProcessIdentifier](definitions.md#ProcessIdentifier) for more details about these parameters.

If neither `pid` nor `uid` are specified, a dump of the [default process](defaultprocess.md) will be captured. Attempting to capture a dump of the default process when the default process cannot be resolved will fail.

## Authentication

Allowed schemes:
- `MonitorApiKey`
- `Negotiate` (Windows only, running as unelevated)

See [Authentication](./../authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | stream | A managed dump of the process. | `application/octet-stream` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many dump requests at this time. Try to request a dump at a later time. | |

## Examples

### Sample Request

```http
GET https://localhost:52323/dump/21632?type=Full
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET https://localhost:52323/dump/cd4da319-fa9e-4987-ac4e-e57b2aac248b?type=Full
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

The managed dump containing all memory of the process is returned as the response body.

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET 5+ |

> **NOTE:** For .NET 5, only ELF core dumps are supported on MacOS and require setting an environment variable in the application. Starting in .NET 6, dumps will be in the MachO format, and this environment variable is deprecated. See [Minidump Generation on OS X](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x) for further details.

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

### View the collected dump file

Dump files collected from this route can be analyzed using tools such as [dotnet-dump](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-dump) or Visual Studio.
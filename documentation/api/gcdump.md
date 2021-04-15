# GC Dump - Get

Captures a GC dump of a specified process. These dumps are useful for several scenarios:

- comparing the number of objects on the heap at several points in time
- analyzing roots of objects (answering questions like, "what still has a reference to this type?")
- collecting general statistics about the counts of objects on the heap.

> **WARNING:** To walk the GC heap, this route triggers a generation 2 (full) garbage collection, which can suspend the runtime for a long time, especially when the GC heap is large. Don't use this route in performance-sensitive environments when the GC heap is large.

## HTTP Route

```http
GET https://localhost:52323/gcdump/{pid}
```

or 

```http
GET https://localhost:52323/gcdump/{uid}
```

or

```http
GET https://localhost:52323/gcdump
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | path | false | int | The ID of the process. |
| `uid` | path | false | guid | A value that uniquely identifies a runtime instance within a process. |

See [ProcessIdentifier](definitions.md#ProcessIdentifier) for more details about these parameters.

If neither `pid` nor `uid` are specified, a GC dump of the [default process](defaultprocess.md) will be captured. Attempting to capture a GC dump of the default process when the default process cannot be resolved will fail.

## Authentication

Allowed schemes:
- `MonitorApiKey`
- `Negotiate` (Windows only, running as unelevated)

See [Authentication](./../authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | stream | A GC dump of the process. | `application/octet-stream` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many GC dump requests at this time. Try to request a GC dump at a later time. | |

## Examples

### Sample Request

```http
GET https://localhost:52323/gcdump/21632
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET https://localhost:52323/gcdump/cd4da319-fa9e-4987-ac4e-e57b2aac248b
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

The GC dump is returned as the response body.

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

### View the collected `.gcdump` file

On Windows, `.gcdump` files can be viewed in [PerfView](https://github.com/microsoft/perfview) for analysis or in Visual Studio. Currently, There is no way of opening a .gcdump on non-Windows platforms.

You can collect multiple `.gcdump`s and open them simultaneously in Visual Studio to get a comparison experience.

Reports can be generated from `.gcdump` files using the [dotnet-gcdump](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-gcdump) tool.
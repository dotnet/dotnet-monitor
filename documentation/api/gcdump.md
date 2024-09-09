# GC Dump - Get

Captures a GC dump of a specified process. These dumps are useful for several scenarios:

- comparing the number of objects on the heap at several points in time
- analyzing roots of objects (answering questions like, "what still has a reference to this type?")
- collecting general statistics about the counts of objects on the heap.

> **WARNING:** To walk the GC heap, this route triggers a generation 2 (full) garbage collection, which can suspend the runtime for a long time, especially when the GC heap is large. Don't use this route in performance-sensitive environments when the GC heap is large.

## HTTP Route

```http
GET /gcdump?pid={pid}&uid={uid}&name={name}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected GC dump. When not specified, the GC dump is written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, a GC dump of the [default process](defaultprocess.md) will be captured. Attempting to capture a GC dump of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | stream | A GC dump of the process when no egress provider is specified. | `application/octet-stream` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many GC dump requests at this time. Try to request a GC dump at a later time. | `application/problem+json` |

> [!NOTE]
> **(7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### Sample Request

```http
GET /gcdump?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

or

```http
GET /gcdump?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

The GC dump, chunk encoded, is returned as the response body.

```http
HTTP/1.1 200 OK
Content-Type: application/octet-stream
Transfer-Encoding: chunked
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18
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

### View the collected `.gcdump` file

On Windows, `.gcdump` files can be viewed in [PerfView](https://github.com/microsoft/perfview) for analysis or in Visual Studio. Currently, there is no way of opening a `.gcdump` on non-Windows platforms.

You can collect multiple `.gcdump`s and open them simultaneously in Visual Studio to get a comparison experience.

Reports can be generated from `.gcdump` files using the [dotnet-gcdump](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-gcdump) tool.

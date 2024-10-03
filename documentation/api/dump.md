# Dump - Get

Captures a managed dump of a specified process without using a debugger.

> **Warning**: Capturing a dump of the process suspends the entire process while the dump is collected.

## HTTP Route

```http
GET /dump?pid={pid}&uid={uid}&name={name}&type={type}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `type` | query | false | [DumpType](definitions.md#dumptype) | The type of dump to capture. Default value is `WithHeap` |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected dump. When not specified, the dump is written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, a dump of the [default process](defaultprocess.md) will be captured. Attempting to capture a dump of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | stream | A managed dump of the process when no egress provider is specified. | `application/octet-stream` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many dump requests at this time. Try to request a dump at a later time. | |

> [!NOTE]
> **(7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### Sample Request

```http
GET /dump?pid=21632&type=Full HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

or

```http
GET /dump?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b&type=Full HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

The managed dump containing all memory of the process, chunk encoded, is returned as the response body.

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
| MacOS | .NET 5+ |

> [!NOTE]
> For .NET 5, only ELF core dumps are supported on MacOS and require setting an environment variable in the application. Starting in .NET 6, dumps will be in the MachO format, and this environment variable is deprecated. See [Minidump Generation on OS X](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x) for further details.

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

### View the collected dump file

Dump files collected from this route can be analyzed using tools such as [dotnet-dump](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-dump) or Visual Studio.

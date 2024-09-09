# Logs - Get Custom

Captures log statements that are logged to the [ILogger<> infrastructure](https://docs.microsoft.com/aspnet/core/fundamentals/logging) within a specified process, as described in the settings specified in the request body. By default, logs are collected at the levels as specified by the [application-defined configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/#configure-logging).

> [!NOTE]
> The [`LoggingEventSource`](https://docs.microsoft.com/aspnet/core/fundamentals/logging#event-source) provider must be enabled in the process in order to capture logs.

## HTTP Route

```http
POST /logs?pid={pid}&uid={uid}&name={name}&durationSeconds={durationSeconds}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `durationSeconds` | query | false | int | The duration of the log collection operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected logs. When not specified, the logs are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, logs for the [default process](defaultprocess.md) will be captured. Attempting to capture logs of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Request Body

A request body of type [LogsConfiguration](definitions.md#logsconfiguration) is required.

The expected content type is `application/json`.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | | The logs from the process formatted as [newline delimited JSON](https://github.com/ndjson/ndjson-spec). Each JSON object is a [LogEntry](definitions.md#logentry) | `application/x-ndjson` |
| 200 OK | | The logs from the process formatted as plain text, similar to the output of the JSON console formatter. | `text/plain` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many logs requests at this time. Try to request logs at a later time. | `application/problem+json` |

> [!NOTE]
> **(7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### Sample Request

```http
POST /logs?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

{
    "filterSpecs": {
        "Microsoft.AspNetCore.Hosting": "Information"
    },
    "useAppFilters": false
}

```

or

```http
POST /logs?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

{
    "filterSpecs": {
        "Microsoft.AspNetCore.Hosting": "Information"
    },
    "useAppFilters": false
}
```

### Sample Response

The log statements logged at the Information level or higher for 1 minute is returned as the response body.

```http
HTTP/1.1 200 OK
Content-Type: text/plain
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

2021-05-13 18:06:41Z info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      => RequestId:0HM8M726ENU3K:0000002B, RequestPath:/, SpanId:|4791a4a7-433aa59a9e362743., TraceId:4791a4a7-433aa59a9e362743, ParentId:
      Request starting HTTP/1.1 GET http://localhost:5000/

2021-05-13 18:06:41Z info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      => RequestId:0HM8M726ENU3K:0000002B, RequestPath:/, SpanId:|4791a4a7-433aa59a9e362743., TraceId:4791a4a7-433aa59a9e362743, ParentId:
      Request finished in 6.8026ms 200 text/html; charset=utf-8
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

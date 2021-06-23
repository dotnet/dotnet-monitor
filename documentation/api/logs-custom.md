# Logs - Get Custom

Captures log statements that are logged to the [ILogger<> infrastructure](https://docs.microsoft.com/aspnet/core/fundamentals/logging) within a specified process, as described in the settings specified in the request body. By default, logs are collected at the levels as specified by the [application-defined configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/#configure-logging).

> **NOTE:** The [`LoggingEventSource`](https://docs.microsoft.com/aspnet/core/fundamentals/logging#event-source) provider must be enabled in the process in order to capture logs.

## HTTP Route

```http
POST /logs?pid={pid}&durationSeconds={durationSeconds}&egressProvider={egressProvider} HTTP/1.1
```

or 

```http
POST /logs?uid={uid}&durationSeconds={durationSeconds}&egressProvider={egressProvider} HTTP/1.1
```

or

```http
POST /logs?name={name}&durationSeconds={durationSeconds}&egressProvider={egressProvider} HTTP/1.1
```

or

```http
POST /logs?durationSeconds={durationSeconds}&egressProvider={egressProvider} HTTP/1.1
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | path | false | int | The ID of the process. |
| `uid` | path | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `name` | path | false | string | The name of the process. |
| `durationSeconds` | query | false | int | The duration of the log collection operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected logs. When not specified, the logs are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |

See [ProcessIdentifier](definitions.md#ProcessIdentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, logs for the [default process](defaultprocess.md) will be captured. Attempting to capture logs of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `MonitorApiKey`
- `Negotiate` (Windows only, running as unelevated)

## Request Body

A request body of type [LogsConfiguration](definitions.md#LogsConfiguration) is required.

The expected content type is `application/json`.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | | The logs from the process formatted as [newline delimited JSON](https://github.com/ndjson/ndjson-spec). Each JSON object is a [LogEntry](definitions.md#LogEntry) | `application/x-ndjson` |
| 200 OK | | The logs from the process formatted as [server-sent events](https://www.w3.org/TR/eventsource). | `text/event-stream` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many logs requests at this time. Try to request logs at a later time. | |

## Examples

### Sample Request

```http
POST /logs?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: MonitorApiKey fffffffffffffffffffffffffffffffffffffffffff=

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
Authorization: MonitorApiKey fffffffffffffffffffffffffffffffffffffffffff=

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
Content-Type: text/event-stream

data: Information Microsoft.AspNetCore.Hosting.Diagnostics[1]
data: 2021-05-13 18:06:41Z
data: Request starting HTTP/1.1 GET http://localhost:5000/  
data: => RequestId:0HM8M726ENU3K:0000002B, RequestPath:/, SpanId:|4791a4a7-433aa59a9e362743., TraceId:4791a4a7-433aa59a9e362743, ParentId:

data: Information Microsoft.AspNetCore.Hosting.Diagnostics[2]
data: 2021-05-13 18:06:41Z
data: Request finished in 6.8026ms 200 text/html; charset=utf-8
data: => RequestId:0HM8M726ENU3K:0000002B, RequestPath:/, SpanId:|4791a4a7-433aa59a9e362743., TraceId:4791a4a7-433aa59a9e362743, ParentId:
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

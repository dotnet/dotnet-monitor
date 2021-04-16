# Trace - Get

Captures a diagnostic trace of a process based on a predefined set of trace profiles.

## HTTP Route

```http
GET https://localhost:52323/trace/{pid}?profile={profile}&durationSeconds={durationSeconds}&metricsIntervalSeconds={metricsIntervalSeconds}
```

or 

```http
GET https://localhost:52323/trace/{uid}?profile={profile}&durationSeconds={durationSeconds}&metricsIntervalSeconds={metricsIntervalSeconds}
```

or

```http
GET https://localhost:52323/trace?profile={profile}&durationSeconds={durationSeconds}&metricsIntervalSeconds={metricsIntervalSeconds}
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | path | false | int | The ID of the process. |
| `uid` | path | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `profile` | query | false | [TraceProfile](definitions.md#TraceProfile) | The name of the profile(s) used to collect events. See [TraceProfile](definitions.md#TraceProfile) for details on the list of event providers, levels, and keywords each profile represents. Multiple profiles may be specified by separating them with commas. Default is `Cpu,Http,Metrics` |
| `durationSeconds` | query | false | int | The duration of the trace operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
| `metricsIntervalSeconds` | query | false | int | The interval (in seconds) at which metrics are collected. Only applicable for the `Metrics` profile. Default is `1`. Min is `1`. Max is `2147483647`. |

See [ProcessIdentifier](definitions.md#ProcessIdentifier) for more details about these parameters.

If neither `pid` nor `uid` are specified, a trace of the [default process](defaultprocess.md) will be captured. Attempting to capture a trace of the default process when the default process cannot be resolved will fail.

## Authentication

Allowed schemes:
- `MonitorApiKey`
- `Negotiate` (Windows only, running as unelevated)

See [Authentication](./../authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | stream | A trace of the process. | `application/octet-stream` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many trace requests at this time. Try to request a trace at a later time. | |

> **NOTE:** After the expiration of the trace duration, completing the request may take a long time (up to several minutes) for large applications. The runtime needs to send over the type cache for all managed code that was captured in the trace, known as rundown events. Thus, the length of time of the request may take significantly longer than the requested duration.

## Examples

### Sample Request

```http
GET https://localhost:52323/trace/21632?profile=http,metrics&durationSeconds=60&metricsIntervalSeconds=5
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

or

```http
GET https://localhost:52323/trace/cd4da319-fa9e-4987-ac4e-e57b2aac248b?profile=http,metrics&durationSeconds=60&metricsIntervalSeconds=5
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

The 1 minute trace with http request handling and metric information is returned as the response body.

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

### View the collected `.nettrace` file

On Windows, `.nettrace` files can be viewed in [PerfView](https://github.com/microsoft/perfview) for analysis or in Visual Studio. 

A `.nettrace` files can be converted to another format (e.g. SpeedScope or Chromium) using the [dotnet-trace](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-trace) tool.
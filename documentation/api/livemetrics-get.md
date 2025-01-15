# Livemetrics - Get

Captures metrics for a chosen process for a duration of time.

> [!NOTE]
> Starting in 8.0, the [metrics configuration](../configuration/metrics-configuration.md#metrics-configuration) is used as the collection specification. Prior to 8.0, the collection specification only included the [default providers](../configuration/metrics-configuration.md#default-providers) and could not be changed.

> [!NOTE]
> For Prometheus style metrics, use the [metrics](./metrics.md) endpoint.

## HTTP Route

```http
GET /livemetrics?pid={pid}&uid={uid}&name={name}&durationSeconds={durationSeconds}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `durationSeconds` | query | false | int | The duration of the metrics operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected metrics. When not specified, the metrics are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, artifacts for the [default process](defaultprocess.md) will be captured. Attempting to capture artifacts of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [Metric](./definitions.md#metric) | The metrics from the process formatted as json sequence. | `application/json-seq` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many requests at this time. Try to request metrics at a later time. | `application/problem+json` |

> [!NOTE]
> **(7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### Sample Request

```http
GET /livemetrics?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json-seq
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

{
    "timestamp": "2021-08-31T16:58:39.7514031+00:00",
    "provider": "System.Runtime",
    "name": "cpu-usage",
    "displayName": "CPU Usage",
    "unit": "%",
    "counterType": "Metric",
    "value": 3
}
{
    "timestamp": "2021-08-31T16:58:39.7515128+00:00",
    "provider": "System.Runtime",
    "name": "working-set",
    "displayName": "Working Set",
    "unit": "MB",
    "counterType": "Metric",
    "value": 126
}
{
    "timestamp": "2021-08-31T16:58:39.7515232+00:00",
    "provider": "System.Runtime",
    "name": "gc-heap-size",
    "displayName": "GC Heap Size",
    "unit": "MB",
    "counterType": "Metric",
    "value": 16
}
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

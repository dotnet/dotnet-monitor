# Livemetrics - Get Custom

Captures metrics for a process, with the ability to specify custom metrics.

## HTTP Route

```http
POST /livemetrics?pid={pid}&uid={uid}&name={name}&durationSeconds={durationSeconds}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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

## Request Body

A request body of type [EventMetricsConfiguration](definitions.md#eventmetricsconfiguration) is required.

The expected content type is `application/json`.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [Metric](./definitions.md#metric) | The metrics from the process formatted as json sequence. Each JSON object is a [metrics object](./definitions.md#metric)| `application/json-seq` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many requests at this time. Try to request metrics at a later time. | `application/problem+json` |

> [!NOTE]
> **(7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### EventCounter

#### Sample Request

```http
POST /livemetrics?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

{
    "includeDefaultProviders": false,
    "providers": [
        {
            "providerName": "CustomProvider",
            "counterNames": [
                "counter1",
                "counter2"
            ]
        }
    ]
}
```

#### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json-seq
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

{
    "timestamp": "2021-08-31T16:58:39.7514031+00:00",
    "provider": "CustomProvider",
    "name": "counter1",
    "displayName": "Counter 1",
    "unit": "B",
    "counterType": "Metric",
    "value": 3,
    "metadata": {
        "MyKey 1": "MyValue 1",
        "MyKey 2": "MyValue 2"
    }
}
{
    "timestamp": "2021-08-31T16:58:39.7515128+00:00",
    "provider": "CustomProvider",
    "name": "counter2",
    "displayName": "Counter 2",
    "unit": "MB",
    "counterType": "Metric",
    "value": 126,
    "metadata": {}
}
```

### System.Diagnostics.Metrics (8.0+)

#### Sample Request

```http
GET /livemetrics?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

{
    "includeDefaultProviders": false,
    "meters": [
        {
            "meterName": "CustomMeter",
            "instrumentNames": [
                "myHistogram"
            ]
        }
    ]
}
```

#### Sample Histogram Response

```http
HTTP/1.1 200 OK
Content-Type: application/json-seq
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

{
    "timestamp": "2021-08-31T16:58:39.7514031+00:00",
    "provider": "CustomMeter",
    "name": "myHistogram",
    "displayName": "myHistogram",
    "unit": null,
    "counterType": "Metric",
    "tags": "Percentile=50",
    "value": 2292
}
{
    "timestamp": "2021-08-31T16:58:39.7514031+00:00",
    "provider": "CustomMeter",
    "name": "myHistogram",
    "displayName": "myHistogram",
    "unit": null,
    "counterType": "Metric",
    "tags": "Percentile=95",
    "value": 4616
}
{
    "timestamp": "2021-08-31T16:58:39.7514031+00:00",
    "provider": "CustomMeter",
    "name": "myHistogram",
    "displayName": "myHistogram",
    "unit": null,
    "counterType": "Metric",
    "tags": "Percentile=99",
    "value": 4960
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

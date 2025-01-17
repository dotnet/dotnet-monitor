# Parameters (experimental feature)

Captures parameters for one or more methods each time they are called.

> [!IMPORTANT]
> This feature is not enabled by default and requires configuration to be enabled. See [Enabling](#enabling) for more information.

## Enabling

This feature is currently marked as experimental and so needs to be explicitly enabled. The following configuration must be set for the feature to be used.

```json
"InProcessFeatures": {
    "ParameterCapturing": {
        "Enabled": true
    }
}
```

## HTTP Route

```http
POST /parameters?pid={pid}&uid={uid}&name={name}&durationSeconds={durationSeconds}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `durationSeconds` | query | false | int | The duration of the parameters operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the captured parameters. When not specified, the parameters are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, parameters from the [default process](defaultprocess.md) will be captured. Attempting to capture parameters from the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Request Body

A request body of type [CaptureParametersConfiguration](definitions.md#captureparametersconfiguration) is required.

The expected content type is `application/json`.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [CapturedMethod](definitions.md#capturedmethod)[] | Separator-delimited JSON representation of the captured parameters. | application/json-seq |
| 200 OK | [CapturedMethod](definitions.md#capturedmethod)[] | Newline-delimited JSON representation of the captured parameters. | application/x-ndjson |
| 200 OK | text | Text representation of the captured parameters. | text/plain |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many parameters requests at this time. Try to request parameters at a later time. | `application/problem+json` |

## Logger Categories

The following logger categories are used inside the target application when capturing parameters:
| Category Name | Description |
| -- | -- |
| `DotnetMonitor.ParameterCapture.Service` | Diagnostic messages by `dotnet-monitor`, such as when parameter capturing starts, stops, or is unable to find a requested method. |

## Examples

### Sample Request

```http
POST /parameters?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: application/x-ndjson

{
    "methods": [
        {
            "moduleName": "SampleWebApp.dll",
            "typeName": "SampleWebApp.Controllers.HomeController",
            "methodName": "Index"
        },
        {
            "moduleName": "System.Private.CoreLib.dll",
            "typeName": "System.String",
            "methodName": "Concat"
        }
    ],
    "captureLimit": 2
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/x-ndjson

{"activityId":"00-aeacc84edb640c5dc72477747729a975-42f991d5c79b02d8-00","activityIdFormat":"W3C","threadId":13,"timestamp":"2024-04-18T08:49:54.1957076-04:00","moduleName":"System.Private.CoreLib.dll","typeName":"System.String","methodName":"Concat","parameters":[{"parameterName":"str0","typeName":"System.String","moduleName":"System.Private.CoreLib.dll","value":"\u0027localhost\u0027"},{"parameterName":"str1","typeName":"System.String","moduleName":"System.Private.CoreLib.dll","value":"\u0027:\u0027"},{"parameterName":"str2","typeName":"System.String","moduleName":"System.Private.CoreLib.dll","value":"\u00277290\u0027"}]}
{"activityId":"00-aeacc84edb640c5dc72477747729a975-42f991d5c79b02d8-00","activityIdFormat":"W3C","threadId":13,"timestamp":"2024-04-18T08:49:54.196018-04:00","moduleName":"System.Private.CoreLib.dll","typeName":"System.String","methodName":"Concat","parameters":[{"parameterName":"str0","typeName":"System.String","moduleName":"System.Private.CoreLib.dll","value":"\u0027\u0027"},{"parameterName":"str1","typeName":"System.String","moduleName":"System.Private.CoreLib.dll","value":"\u0027/Account/SignIn\u0027"}]}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 7+ |
| Linux | .NET 7+ |
| MacOS | .NET 7+ |

## Additional Requirements

- The target application cannot have [Hot Reload](https://learn.microsoft.com/visualstudio/debugger/hot-reload) enabled.
- `dotnet-monitor` must be set to `Listen` mode. See [diagnostic port configuration](../configuration/diagnostic-port-configuration.md) for information on how to do this.
- If the target application is using .NET 7 then the dotnet-monitor startup hook must be manually configured and the target application must start suspended. In .NET 8+ this is not a requirement.
- This feature relies on a [ICorProfilerCallback](https://docs.microsoft.com/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-interface) implementation. If the target application is already using an `ICorProfiler` that isn't notify-only, this feature will not be available.

## Additional Notes

### Unsupported Parameters

Currently some types of parameters are unable to be captured. When a method contains one of these unsupported types, the parameter's value will be represented as `<unsupported>`. Other parameters in the method will still be captured so long as they are supported. The most common unsupported types are listed below.

| Parameter Type | Example |
| -- | -- |
| Parameters with pass-by-reference modifiers ([`in`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/in-parameter-modifier), [`out`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/out-parameter-modifier), and [`ref`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/ref)) | `ref int i` |
| [Pointers](https://learn.microsoft.com/dotnet/csharp/language-reference/unsafe-code#pointer-types) | `void*` |

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.


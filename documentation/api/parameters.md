
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Fparameters)

# Parameters - Post (experimental feature)

Captures parameters for one or more methods each time they are called. Parameters are logged inside the target application using its [`ILogger`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger).

>**Note**: Unlike other artifacts, parameters do **not** support being sent to egress provider.

>**Note**: This feature is not enabled by default and requires configuration to be enabled. See [Enabling](#enabling) for more information.

## Enabling

This feature is currently marked as experimental and so needs to be explicitly enabled. The following configuration must be set for the feature to be used.

```json
"InProcessFeatures": {
    "Enabled": true,
    "ParameterCapturing": {
        "Enabled": true
    }
}
```

## HTTP Route

```http
POST /parameters?pid={pid}&uid={uid}&name={name}&durationSeconds={durationSeconds}&tags={tags} HTTP/1.1
```

> **Note**: Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | query | false | int | The ID of the process. |
| `uid` | query | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `name` | query | false | string | The name of the process. |
| `durationSeconds` | query | false | int | The duration of the parameters operation in seconds. Default is `30`. Min is `-1` (indefinite duration). Max is `2147483647`. |
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
| 202 Accepted | | The artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many parameters requests at this time. Try to request parameters at a later time. | `application/problem+json` |

## `UserCode` vs `SystemCode`

Methods that belong to any of the following namespaces are considered `SystemCode`:
- `Microsoft.*`
- `System.*`

All other methods are considered `UserCode`. `UserCode` methods will have their parameters captured inline, meaning that the added log statements are performed synchronously inside your method, preserving the logger's scope.

`SystemCode` methods will have their parameters captured asynchronously and without scope information.

The [examples](#examples) include a mixture of `UserCode` and `SystemCode` to help showcase the difference.

## Logger Categories

The following logger categories are used inside the target application when capturing parameters:
| Category Name | Description |
| -- | -- |
| `DotnetMonitor.ParameterCapture.UserCode` | Parameters captured in methods considered `UserCode`. |
| `DotnetMonitor.ParameterCapture.SystemCode` | Parameters captured in methods considered `SystemCode`. |
| `DotnetMonitor.ParameterCapture.Service` | Diagnostic messages by `dotnet-monitor`, such as when parameter capturing starts, stops, or is unable to find a requested method. |

## Examples

### Sample Request

```http
POST /parameters?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

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
    ]
}
```

### Sample Response

```http
HTTP/1.1 202 Accepted
Content-Type: application/json
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18
```

### Sample Output (Target Application)

```
info: DotnetMonitor.ParameterCapture.UserCode[0]
      => SpanId:e40e62cffe1cf1cb, TraceId:c76b911969aa8abcf335907e96c62b33, ParentId:0000000000000000 => ConnectionId:0HMT2D6L8GT2Q => RequestPath:/ RequestId:0HMT2D6L8GT2Q:00000003 => SampleWebApp.Controllers.HomeController.Index (SampleWebApp)
      SampleWebApp.Controllers.HomeController.Index(
        this: 'SampleWebApp.Controllers.HomeController',
        number: 10)
info: DotnetMonitor.ParameterCapture.SystemCode[0]
      System.String.Concat(
        str0: 'firstString',
        str1: '.secondString')
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 7+ |
| Linux | .NET 7+ |
| MacOS | .NET 7+ |

## Additional Requirements

- The target application must use ASP.NET Core.
- `dotnet-monitor` must be set to `Listen` mode, and the target application must start suspended. See [diagnostic port configuration](../configuration/diagnostic-port-configuration.md) for information on how to do this.
- The target application must have [`ILogger`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger) available via [ASP.NET Core's dependency injection](https://learn.microsoft.com/aspnet/core/mvc/controllers/dependency-injection).
- This feature relies on a hosting startup assembly. If the target application [disabled automatic loading](https://learn.microsoft.com/aspnet/core/fundamentals/host/platform-specific-configuration#disable-automatic-loading-of-hosting-startup-assemblies) of these, this feature will not be available.
- This feature relies on a [ICorProfilerCallback](https://docs.microsoft.com/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-interface) implementation. If the target application is already using an `ICorProfiler` that isn't notify-only, this feature will not be available.
- If a target application is using .NET 7 then the `dotnet-monitor` startup hook must be configured. This is automatically done in .NET 8+.

## Additional Notes

### Unsupported Parameters

Currently some types of parameters are unable to be captured. When a method contains one of these unsupported types, the parameter's value will be represented as `<unsupported>`. Other parameters in the method will still be captured so long as they are supported.

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

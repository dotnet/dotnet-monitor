
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Fparameters)

# Parameters - Post

Captures parameters for one or more methods each time they are called.

>**Note**: This feature is not enabled by default and requires configuration to be enabled.

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

## Examples

### Sample Request

```http
POST /parameters?pid=21632&durationSeconds=60 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=

{
    "methods": [
        {
            "assemblyName": "SampleWebApp",
            "typeName": "SampleWebApp.Controllers.HomeController",
            "methodName": "Index"
        },
        {
            "assemblyName": "System.Private.CoreLib",
            "typeName": "System.String",
            "methodName": "Concat"
        }
    ]
}
```

### Sample Response

```http
HTTP/1.1 202 OK
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
        str0: 'InvokeStub_',
        str1: 'HtmlHelper`1.',
        str2: '.ctor')
```



## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 7+ |
| Linux | .NET 7+ |
| MacOS | .NET 7+ |


## Additional Requirements
- The process must use aspnetcore.
- The process must connect 
- An `ILogger` factory must be registered.
- This feature uses a hosting startup assembly behind the scenes. If you have [disabled automatic loading](https://learn.microsoft.com/aspnet/core/fundamentals/host/platform-specific-configuration#disable-automatic-loading-of-hosting-startup-assemblies) of these, this feature will not be available.

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.


### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Fstacks)

# Stacks - Get

Captures the call stacks of a target process. Note that only managed frames are collected.

>**Note**: This feature is not enabled by default and requires configuration to be enabled. The [in-process features](./../configuration/in-process-features-configuration.md) must be enabled since the call stacks feature uses shared libraries loaded into the target application for collecting the call stack information.

## HTTP Route

```http
GET /stacks?pid={pid}&uid={uid}&name={name}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected stacks. When not specified, the stacks are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, a stack of the [default process](defaultprocess.md) will be captured. Attempting to capture a stack of the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [CallStackResult](definitions.md#callstackresult) | Callstacks for all managed threads in the process. | `application/json` |
| 200 OK | text | Text representation of callstacks in the process. | `text/plain` |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many stack requests at this time. Try to request a stack at a later time. | `application/problem+json` |

> **NOTE: (7.1+)** Regardless if an egress provider is specified if the request was successful (response codes 200 or 202), the Location header contains the URI of the operation. This can be used to query the status of the operation or change its state.

## Examples

### Sample Request

```http
GET /stack?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: application/json
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

{
    "threadId": 30860,
    "threadName" : "Worker Thread"
    "frames": [
        {
            "methodName": "GetQueuedCompletionStatus",
            "className": "Interop\u002BKernel32",
            "moduleName": "System.Private.CoreLib.dll",
        },
        {
            "methodName": "WaitForSignal",
            "className": "System.Threading.LowLevelLifoSemaphore",
            "moduleName": "System.Private.CoreLib.dll",
        },
        {
            "methodName": "Wait",
            "className": "System.Threading.LowLevelLifoSemaphore",
            "moduleName": "System.Private.CoreLib.dll",
        }
}
```

### Sample Request

```http
GET /stack?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain
Location: localhost:52323/operations/67f07e40-5cca-4709-9062-26302c484f18

Thread: (0x68C0)
  System.Private.CoreLib.dll!System.Threading.Monitor.Wait
  System.Private.CoreLib.dll!System.Threading.ManualResetEventSlim.Wait
  System.Private.CoreLib.dll!System.Threading.Tasks.Task.SpinThenBlockingWait
  System.Private.CoreLib.dll!System.Threading.Tasks.Task.InternalWaitCore
  System.Private.CoreLib.dll!System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification
  System.Private.CoreLib.dll!System.Runtime.CompilerServices.TaskAwaiter.GetResult
  Microsoft.Extensions.Hosting.Abstractions.dll!Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Run
  WebApplication5.dll!WebApplication5.Program.Main
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 6+ |
| Linux | .NET 6+ |
| MacOS | .NET 6+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

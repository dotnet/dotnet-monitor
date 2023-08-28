
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%exceptions)

# Exceptions History - Get

Captures a history of first chance exceptions that were thrown in the specified process.

>**Note**: This feature is not enabled by default and requires configuration to be enabled. The [in-process features](./../configuration/in-process-features-configuration.md) must be enabled since the exceptions history feature uses shared libraries loaded into the target application for collecting the exception information.

## HTTP Route

```http
GET /exceptions?pid={pid}&uid={uid}&name={name}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
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
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the collected exceptions. When not specified, the exceptions are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | (7.1+) A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, exceptions from the [default process](defaultprocess.md) will be captured. Attempting to capture exceptions from the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [ExceptionInstance](definitions.md#exceptioninstance)[] | Newline-delimited JSON representation of first chance exceptions from the target process. | `application/x-ndjson` |
| 200 OK | [ExceptionInstance](definitions.md#exceptioninstance)[] | Separator-delimited JSON representation of first chance exceptions from the target process. | `application/json-seq` |
| 200 OK | text | Text representation of first chance exceptions from the target process. | `text/plain` |
| 202 Accepted | | When an egress provider is specified. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

Collect exceptions from a specific process in plain text format:

```http
GET /exceptions?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
```

or

```http
GET /exceptions?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain

First chance exception at 2023-07-13T21:45:11.8056355Z
System.InvalidOperationException: Operation is not valid due to the current state of the object.
   at WebApplication3.Pages.IndexModel+<GetData>d__3.MoveNext()
   at System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext,System.Threading.ContextCallback,System.Object)
   at System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TStateMachine].MoveNext(System.Threading.Thread)
   at System.Runtime.CompilerServices.YieldAwaitable+YieldAwaiter+<>c.<OutputCorrelationEtwEvent>b__6_0(System.Action,System.Threading.Tasks.Task)
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()

First chance exception at 2023-07-13T21:46:18.7530773Z
System.ObjectDisposedException: Cannot access a disposed object.
Object name: 'System.Net.Sockets.NetworkStream'.
   at System.ThrowHelper.ThrowObjectDisposedException(System.Object)
   at System.ObjectDisposedException.ThrowIf(System.Boolean,System.Object)
   at System.Net.Sockets.NetworkStream.ReadAsync(System.Memory`1[[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]][System.Byte],System.Threading.CancellationToken)
   at System.Net.Http.HttpConnection+<<EnsureReadAheadTaskHasStarted>g__ReadAheadWithZeroByteReadAsync|43_0>d.MoveNext()
   at System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TStateMachine].ExecutionContextCallback(System.Object)
   at System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext,System.Threading.ContextCallback,System.Object)
   at System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TStateMachine].MoveNext(System.Threading.Thread)
   at System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1+TStateMachine].MoveNext()
   at System.Net.Sockets.SocketAsyncEventArgs+<>c.<.cctor>b__176_0(System.UInt32,System.UInt32,System.Threading.NativeOverlapped*)
   at System.Threading.PortableThreadPool+IOCompletionPoller+Callback.Invoke(System.Threading.PortableThreadPool+IOCompletionPoller+Event)
   at System.Threading.ThreadPoolTypedWorkItemQueue`2[System.Threading.ThreadPoolTypedWorkItemQueue`2+T,System.Threading.ThreadPoolTypedWorkItemQueue`2+TCallback].System.Threading.IThreadPoolWorkItem.Execute()
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()
```

### Sample Request

Collect exceptions from the default process in newline-delimited JSON format:

```http
GET /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: application/x-ndjson
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/x-ndjson

{"id":2,"timestamp":"2023-07-13T21:45:11.8056355Z","typeName":"System.InvalidOperationException","moduleName":"System.Private.CoreLib.dll","message":"Operation is not valid due to the current state of the object.","innerExceptions":[],"callStack":{"threadId":4768,"threadName":null,"frames":[{"methodName":"MoveNext","parameterTypes":[],"className":"WebApplication3.Pages.IndexModel\u002B\u003CGetData\u003Ed__3","moduleName":"WebApplication3.dll"},{"methodName":"RunInternal","parameterTypes":["System.Threading.ExecutionContext","System.Threading.ContextCallback","System.Object"],"className":"System.Threading.ExecutionContext","moduleName":"System.Private.CoreLib.dll"},{"methodName":"MoveNext","parameterTypes":["System.Threading.Thread"],"className":"System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601[System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTStateMachine]","moduleName":"System.Private.CoreLib.dll"},{"methodName":"\u003COutputCorrelationEtwEvent\u003Eb__6_0","parameterTypes":["System.Action","System.Threading.Tasks.Task"],"className":"System.Runtime.CompilerServices.YieldAwaitable\u002BYieldAwaiter\u002B\u003C\u003Ec","moduleName":"System.Private.CoreLib.dll"},{"methodName":"Dispatch","parameterTypes":[],"className":"System.Threading.ThreadPoolWorkQueue","moduleName":"System.Private.CoreLib.dll"},{"methodName":"WorkerThreadStart","parameterTypes":[],"className":"System.Threading.PortableThreadPool\u002BWorkerThread","moduleName":"System.Private.CoreLib.dll"}]}}
{"id":3,"timestamp":"2023-07-13T21:46:18.7530773Z","typeName":"System.ObjectDisposedException","moduleName":"System.Private.CoreLib.dll","message":"Cannot access a disposed object.\r\nObject name: \u0027System.Net.Sockets.NetworkStream\u0027.","innerExceptions":[],"callStack":{"threadId":15912,"threadName":null,"frames":[{"methodName":"ThrowObjectDisposedException","parameterTypes":["System.Object"],"className":"System.ThrowHelper","moduleName":"System.Private.CoreLib.dll"},{"methodName":"ThrowIf","parameterTypes":["System.Boolean","System.Object"],"className":"System.ObjectDisposedException","moduleName":"System.Private.CoreLib.dll"},{"methodName":"ReadAsync","parameterTypes":["System.Memory\u00601[[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]][System.Byte]","System.Threading.CancellationToken"],"className":"System.Net.Sockets.NetworkStream","moduleName":"System.Net.Sockets.dll"},{"methodName":"MoveNext","parameterTypes":[],"className":"System.Net.Http.HttpConnection\u002B\u003C\u003CEnsureReadAheadTaskHasStarted\u003Eg__ReadAheadWithZeroByteReadAsync|43_0\u003Ed","moduleName":"System.Net.Http.dll"},{"methodName":"ExecutionContextCallback","parameterTypes":["System.Object"],"className":"System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601[System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTStateMachine]","moduleName":"System.Private.CoreLib.dll"},{"methodName":"RunInternal","parameterTypes":["System.Threading.ExecutionContext","System.Threading.ContextCallback","System.Object"],"className":"System.Threading.ExecutionContext","moduleName":"System.Private.CoreLib.dll"},{"methodName":"MoveNext","parameterTypes":["System.Threading.Thread"],"className":"System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601[System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTStateMachine]","moduleName":"System.Private.CoreLib.dll"},{"methodName":"MoveNext","parameterTypes":[],"className":"System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601[System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTResult,System.Runtime.CompilerServices.AsyncTaskMethodBuilder\u00601\u002BAsyncStateMachineBox\u00601\u002BTStateMachine]","moduleName":"System.Private.CoreLib.dll"},{"methodName":"\u003C.cctor\u003Eb__176_0","parameterTypes":["System.UInt32","System.UInt32","System.Threading.NativeOverlapped*"],"className":"System.Net.Sockets.SocketAsyncEventArgs\u002B\u003C\u003Ec","moduleName":"System.Net.Sockets.dll"},{"methodName":"Invoke","parameterTypes":["System.Threading.PortableThreadPool\u002BIOCompletionPoller\u002BEvent"],"className":"System.Threading.PortableThreadPool\u002BIOCompletionPoller\u002BCallback","moduleName":"System.Private.CoreLib.dll"},{"methodName":"System.Threading.IThreadPoolWorkItem.Execute","parameterTypes":[],"className":"System.Threading.ThreadPoolTypedWorkItemQueue\u00602[System.Threading.ThreadPoolTypedWorkItemQueue\u00602\u002BT,System.Threading.ThreadPoolTypedWorkItemQueue\u00602\u002BTCallback]","moduleName":"System.Private.CoreLib.dll"},{"methodName":"Dispatch","parameterTypes":[],"className":"System.Threading.ThreadPoolWorkQueue","moduleName":"System.Private.CoreLib.dll"},{"methodName":"WorkerThreadStart","parameterTypes":[],"className":"System.Threading.PortableThreadPool\u002BWorkerThread","moduleName":"System.Private.CoreLib.dll"}]}}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 6+ |
| Linux | .NET 6+ |
| MacOS | .NET 6+ |

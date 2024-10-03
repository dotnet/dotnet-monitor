# Exceptions History - Custom

Captures a history of first chance exceptions that were thrown in the specified process, with the ability to filter which exceptions are included in the response.

> [!NOTE]
> This feature is not enabled by default and requires configuration to be enabled. The [in-process features](./../configuration/in-process-features-configuration.md) must be enabled since the exceptions history feature uses shared libraries loaded into the target application for collecting the exception information.

## HTTP Route

```http
POST /exceptions?pid={pid}&uid={uid}&name={name}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
```

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

## Request Body

A request body of type [ExceptionsConfiguration](definitions.md#exceptionsconfiguration) is required.

The expected content type is `application/json`.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [ExceptionInstance](definitions.md#exceptioninstance)[] | JSON representation of first chance exceptions from the default process. | `application/x-ndjson` |
| 200 OK | text | Text representation of first chance exceptions from the default process. | `text/plain` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
POST /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
{
    "Exclude": [
        {
            "exceptionType": "System.InvalidOperationException"
        }
    ]
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain

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

```http
POST /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
{
    "Include": [
        {
            "methodName": "MyExceptionMethod"
        }
    ]
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain

First chance exception at 2023-08-08T14:50:34.1039177Z
System.InvalidOperationException: There was an invalid operation!
   at MyApp.MyClass.MyExceptionMethod()
   at MyApp.MyClass.CallingMethod(System.String)
   at MyApp.MyClass.Main(System.String[])
```

### Sample Request

```http
POST /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
{
    "Include": [
        {
            "methodName": "MyExceptionMethod1"
        },
        {
            "methodName": "MyExceptionMethod2"
        }
    ]
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain

First chance exception at 2023-08-08T14:50:34.1039177Z
System.InvalidOperationException: There was an invalid operation!
   at MyApp.MyClass.MyExceptionMethod1()
   at MyApp.MyClass.CallingMethod(System.String)
   at MyApp.MyClass.Main(System.String[])

First chance exception at 2023-08-08T14:50:34.1039177Z
System.DivideByZeroException: You tried to divide by zero!
   at MyApp.MyClass.MyExceptionMethod2()
   at MyApp.MyClass.CallingMethod(System.String)
   at MyApp.MyClass.Main(System.String[])
```


### Sample Request

```http
POST /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: text/plain
{
    "Include": [
        {
            "methodName": "MyExceptionMethod",
            "typeName": "MyApp.MyClass"
            "moduleName": "MyApp.dll"
            "exceptionType": "System.InvalidOperationException"
        }
    ]
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain

First chance exception at 2023-08-08T14:50:34.1039177Z
System.InvalidOperationException: There was an invalid operation!
   at MyApp.MyClass.MyExceptionMethod()
   at MyApp.MyClass.CallingMethod(System.String)
   at MyApp.MyClass.Main(System.String[])
```

### Sample Request

```http
POST /exceptions HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: application/x-ndjson
{
    "Include": [
        {
            "typeName": "MyClass"
        }
    ],
    "Exclude": [
        {
            "methodName": "MyExceptionMethod"
        }
    ],
}
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/x-ndjson

{"id":4,"timestamp":"2023-08-08T15:42:05.4014435Z","typeName":"System.DivideByZeroException","moduleName":"System.Private.CoreLib.dll","message":"Something was divided by zero!","stack":{"threadId":30448,"threadName":null,"innerExceptions":[],"frames":[{"methodName":"MyExceptionMethod2","parameterTypes":[],"typeName":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"Main","parameterTypes":["System.String[]"],"typeName":"MyApp.MyClass","moduleName":"MyApp.dll"}]}}
{"id":6,"timestamp":"2023-08-08T15:42:06.411379Z","typeName":"System.Exception","moduleName":"System.Private.CoreLib.dll","message":"There was an exception!","stack":{"threadId":30448,"threadName":null,"innerExceptions":[],"frames":[{"methodName":"MyExceptionMethod3","parameterTypes":[],"typeName":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"MyExceptionMethod4","parameterTypes":[],"typeName":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"Main","parameterTypes":["System.String[]"],"typeName":"MyApp.MyClass","moduleName":"MyApp.dll"}]}}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 6+ |
| Linux | .NET 6+ |
| MacOS | .NET 6+ |

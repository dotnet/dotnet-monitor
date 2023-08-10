### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%exceptions-custom)

# Exceptions History - Custom

Captures a history of first chance exceptions that were thrown in the specified process, with the ability to filter which exceptions are included in the response.

>**Note**: This feature is not enabled by default and requires configuration to be enabled. The [in-process features](./../configuration/in-process-features-configuration.md) must be enabled since the exceptions history feature uses shared libraries loaded into the target application for collecting the exception information.

## HTTP Route

```http
POST /exceptions HTTP/1.1
```

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

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
Accept: application/x-ndjson
{
    "Include": [
        {
            "className": "MyClass"
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

{"id":4,"timestamp":"2023-08-08T15:42:05.4014435Z","typeName":"System.DivideByZeroException","moduleName":"System.Private.CoreLib.dll","message":"Something was divided by zero!","callStack":{"threadId":30448,"threadName":null,"innerExceptions":[],"frames":[{"methodName":"MyExceptionMethod2","parameterTypes":[],"className":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"Main","parameterTypes":["System.String[]"],"className":"MyApp.MyClass","moduleName":"MyApp.dll"}]}}
{"id":6,"timestamp":"2023-08-08T15:42:06.411379Z","typeName":"System.Exception","moduleName":"System.Private.CoreLib.dll","message":"There was an exception!","callStack":{"threadId":30448,"threadName":null,"innerExceptions":[],"frames":[{"methodName":"MyExceptionMethod3","parameterTypes":[],"className":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"RandomGeneric","parameterTypes":[],"className":"MyApp.MyClass","moduleName":"MyApp.dll"},{"methodName":"Main","parameterTypes":["System.String[]"],"className":"MyApp.MyClass","moduleName":"MyApp.dll"}]}}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 6+ |
| Linux | .NET 6+ |
| MacOS | .NET 6+ |

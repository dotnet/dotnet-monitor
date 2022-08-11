# Definitions

## CollectionRuleDescription

Object describing the basic state of a collection rule for the executing instance of `dotnet monitor`.

| Name | Type | Description |
|---|---|---|
| State | [CollectionRuleState](#collectionrulestate) | Indicates what state the collection rule is in for the current process. |
| StateReason | string | Human-readable explanation for the current state of the collection rule. |

## CollectionRuleDetailedDescription

Object describing the detailed state of a collection rule for the executing instance of `dotnet monitor`.

| Name | Type | Description |
|---|---|---|
| State | [CollectionRuleState](#collectionrulestate) | Indicates what state the collection rule is in for the current process. |
| StateReason | string | Human-readable explanation for the current state of the collection rule. |
| LifetimeOccurrences | int | The number of times the trigger has executed for a process in its lifetime. |
| SlidingWindowOccurrences | int | The number of times the trigger has executed within the current sliding window. |
| ActionCountLimit | int | The number of times the action list may be executed before being throttled. |
| ActionCountSlidingWindowDurationLimit | TimeSpan? | The sliding window of time to consider whether the action list should be throttled based on the number of times the action list was executed. Executions that fall outside the window will not count toward the limit specified in the ActionCount setting. If not specified, all action list executions will be counted for the entire duration of the rule. |
| SlidingWindowDurationCountdown | TimeSpan? | The amount of time remaining before the collection rule will no longer be throttled. |
| RuleFinishedCountdown | TimeSpan? | The amount of time remaining before the rule will stop monitoring a process after it has been applied to a process. If not specified, the rule will monitor the process with the trigger indefinitely. |

## CollectionRuleState

Enumeration that describes the current state of the collection rule.

| Name | Description |
|---|---|
| `Running` | Indicates that the collection rule is active and waiting for its triggering conditions to be satisfied. |
| `ActionExecuting` | Indicates that the collection has had its triggering conditions satisfied and is currently executing its action list. |
| `Throttled` | Indicates that the collection rule is temporarily throttled because the ActionCountLimit has been reached within the ActionCountSlidingWindowDuration. |
| `Finished` | Indicates that the collection rule has completed and will no longer trigger. |

## DotnetMonitorInfo

Object describing diagnostic/automation information about the executing instance of `dotnet monitor`.

| Name | Type | Description |
|---|---|---|
| Version | string | The current version of `dotnet monitor`. |
| RuntimeVersion | string | The version of the dotnet runtime. |
| DiagnosticPortMode | DiagnosticPortConnectionMode | Indicates whether `dotnet monitor` is in `connect` mode or `listen` mode. |
| DiagnosticPortName | string | The name of the named pipe or unix domain socket to use for connecting to the diagnostic server. |

## DumpType

Enumeration that describes the type of information to capture in a managed dump.

| Name | Description |
|---|---|
| `Mini` | A small dump containing module lists, thread lists, exception information, and all stacks. |
| `Full` | The largest dump containing all memory including the module images. |
| `Triage` | A small dump containing only stacks for each thread. |
| `WithHeap` | A large and relatively comprehensive dump containing module lists, thread lists, all stacks, exception information, handle information, and all memory except for mapped images. |

## EventMetricsConfiguration

Describes custom metrics.

| Name | Type | Description |
|---|---|---|
| `includeDefaultProviders` | bool | Determines if the default counter providers should be used (such as System.Runtime). |
| `providers` | [EventMetricsProvider](#eventmetricsprovider)[] | Array of providers for metrics to collect. |

## EventMetricsProvider

| Name | Type | Description |
|---|---|---|
| `providerName` | string | The name of the metric provider. Note this is case-insensitive. |
| `counterNames` | string[] | Array of providers for metrics to collect. These are case-sensitive. |

## EventProvider

Object describing which events to capture from a single event provider with keywords and event levels.

| Name | Type | Description |
|---|---|---|
| Name | string | The name of the provider from which to capture events. See [Well-known Event Providers](https://docs.microsoft.com/dotnet/core/diagnostics/well-known-event-providers) for commonly used event providers. |
| Keywords | string | Keyword flags used to enable groups of events. Keyword flags are provider specific. May be specified as a 'stringified' integer or a hexadecimal-encoded integer starting with `0x`. |
| EventLevel | [EventLevel](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventlevel) | The level of the events to collect. |
| Arguments | map (of string) | Additional arguments to the event provider. Names and values are event provider specific. |

## EventProvidersConfiguration

Object describing the list of event providers, keywords, event levels, and additional parameters for capturing a trace.

| Name | Type | Description |
|---|---|---|
| Providers | [EventProvider](#eventprovider)[] | List of event providers from which to capture events. At least one event provider must be specified. |
| RequestRundown | bool | The runtime may provide additional type information for certain types of events after the trace session is ended. This additional information is known as rundown events. Without this information, some events may not be parsable into useful information. Default is `true`. |
| BufferSizeInMB | int | The size (in megabytes) of the event buffer used in the runtime. If the event buffer is filled, events produced by event providers may be dropped until the buffer is cleared. Increase the buffer size to mitigate this or pair down the list of event providers, keywords, and level to filter out extraneous events. Default is `256`. Min is `1`. Max is `1024`. |

### Example

```json
{
    "Providers": [{
        "Name": "Microsoft-DotNETCore-SampleProfiler",
        "EventLevel": "Informational"
    },{
        "Name": "Microsoft-Windows-DotNETRuntime",
        "EventLevel": "Informational",
        "Keywords": "0x14C14FCCBD"
    }],
    "BufferSizeInMB": 1024
}
```

## LogEntry

Object describing a log entry from a target process.

| Name | Type | Description |
|---|---|---|
| `Arguments` | map (of object) | The arguments of the format string of the log entry, including an entry for the original format string. |
| `Category` | string | The category of the log entry. |
| `EventId` | string | The event name of the EventId of the log entry. |
| `Exception` | string | If an exception is logged, this property contains the formatted message of the log entry. |
| `LogLevel` | string | The [LogLevel](#loglevel) of the log entry. |
| `Message` | string | If an exception is NOT logged, this property contains the formatted message of the log entry. |
| `Scopes` | map (of object) | The scope information associated with the log entry. |

### Example

If an application logged the following message:

```cs
ILogger<MyNamespace.MyClass> logger = loggerFactory.CreateLogger<MyNamespace.MyClass>();
logger.LogError(new EventId(7, "FailedAfterRetries"), "Failed to get resource after {attempts} attempts.", 5);
```

The above message would be reported as:

```json
{
    "LogLevel": "Error",
    "EventId": "FailedAfterRetries",
    "Category": "MyNamespace.MyClass",
    "Message": "Failed to get resource after 5 attempts.",
    "Scopes": {
        "RequestId": "8000000a-0004-fc00-b63f-84710c7967bb",
        "RequestPath": "/",
        "ActionId": "9524e18b-1bac-4d4c-88d7-68a753258b1c",
        "ActionName": "/Index"
    },
    "Arguments": {
        "attempts": "5",
        "{OriginalFormat}": "Failed to get resource after {attempts} attempts."
    }
}
```

## LogFormat

Enumeration that describes the format to use when outputting logs.

| Name |
|---|
| `JsonSequence` |
| `NewlineDelimitedJson` |
| `PlainText` |

## LogLevel

Enumeration that defines logging severity levels.

See [LogLevel](https://docs.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel) documentation.

## LogsConfiguration

Object describing the default log level and filtering specifications for collecting logs.

| Name | Type | Description |
|---|---|---|
| `logLevel` | [LogLevel](#loglevel) | The default log level at which logs are collected. Default is `Warning`. |
| `filterSpecs` | map (of [LogLevel](#loglevel) or `null`) | A mapping of logger categories and the levels at which those categories should be collected. If level is set to `null`, collect category at the default level set in the `logLevel` property. |
| `useAppFilters` | bool | Collect logs for the categories and at the levels as specified by the [application-defined configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/#configure-logging). Default is `true`. |

### Example

The following configuration will collect logs for the Microsoft.AspNetCore.Hosting category at the Information level or higher.

```json
{
    "filterSpecs": {
        "Microsoft.AspNetCore.Hosting": "Information"
    },
    "useAppFilters": false
}
```

## Metric

Object describing a metric from the application.

| Name | Type | Description |
|---|---|---|
| `name`| string | The unique name of this metric. |
| `displayName` | string | Friendly name for the metric. |
| `timestamp` | DateTime | Time when the metric was collected. |
| `provider` | string | The provider name for the metric. |
| `unit` | string | The unit for the metric. Can be null. |
| `counterType` | string | The type of metric. This is typically `Rate` or `Metric`. |
| `value` | double | The value of the metric. |

## OperationError

| Name | Type | Description |
|---|---|---|
| `code` | string | Error code representing the failure. |
| `message` | string | Detailed error message. |

## OperationState

Status of the egress operation.

| Name | Description |
|---|---|
| `Running` | Operation has been started. This is the initial state. |
| `Cancelled` | The operation was cancelled by the user. |
| `Succeeded` | Egress operation has been successful. Querying the operation will return the location of the egressed artifact. |
| `Failed` | Egress operation failed. Querying the operation will return detailed error information. |

## OperationStatus

Detailed information about an operation.

| Name | Type | Description |
|---|---|---|
| `resourceLocation` | string | Resource location of the egressed artifact. This can be Uri or path depending on the egress provider. |
| `error` | [OperationError](#operationerror) | Detailed error message if the operation is in a `Failed` state. |
| `operationId` | guid | Unique identifier for the operation. |
| `createdDateTime` | datetime string | UTC DateTime string of when the operation was created. |
| `status` | [OperationState](#operationstate) | The current status of operation. |

### Example

```json
{
    "resourceLocation": "https://example.blob.core.windows.net/dotnet-monitor/artifacts%2Fcore_20210721_062115",
    "error": null,
    "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
    "createdDateTime": "2021-07-21T06:21:15.315861Z",
    "status": "Succeeded"
}
```

## OperationSummary

Summary state of an operation.

| Name | Type | Description |
|---|---|---|
| `operationId` | guid | Unique identifier for the operation. |
| `createdDateTime` | datetime string | UTC DateTime string of when the operation was created. |
| `status` | [OperationState](#operationstate) | The current status of operation. |

### Example

```json
{
    "operationId": "67f07e40-5cca-4709-9062-26302c484f18",
    "createdDateTime": "2021-07-21T06:21:15.315861Z",
    "status": "Succeeded"
}
```

## ProcessIdentifier

Object with process identifying information. The properties on this object describe indentifying aspects for a found process; these values can be used in other API calls to perform operations on specific processes.

| Name | Type | Description |
|---|---|---|
| `name` | string | The name of the process. |
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` An empty value: `00000000-0000-0000-0000-000000000000` |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).

The `name` property may not be a unique identifier if the application was built as a [framework-dependent executable](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-framework-dependent). In this case, the name of the process is likely to be either `dotnet.exe` (Windows) or `dotnet` (non-Windows). Framework-dependent executables rely on a shared framework installation, which uses the `dotnet.exe` or `dotnet` executable to run the application.

### Example

```json
{
    "pid": 21632,
    "uid": "cd4da319-fa9e-4987-ac4e-e57b2aac248b"
}
```

## ProcessInfo

Object with detailed information about a specific process.

Some properties will have non-null values for procesess that are running on .NET 5 or newer (denoted with `.NET 5+`). These properties will be null for runtime versions prior to .NET 5.

| Name | Type | Description |
|---|---|---|
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` An empty value: `00000000-0000-0000-0000-000000000000` |
| `name` | string | The name of the process. |
| `commandLine` | string | The command line of the process (includes process path and arguments) |
| `operatingSystem` | string | `.NET 5+` The operating system on which the process is running (e.g. `windows`, `linux`, `macos`).<br/>`.NET Core 3.1` A value of `null`. |
| `processArchitecture` | string | `.NET 5+` The architecture of the process (e.g. `x64`, `x86`).<br/>`.NET Core 3.1` A value of `null`. |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).

### Example

```json
{
    "pid": 21632,
    "uid": "cd4da319-fa9e-4987-ac4e-e57b2aac248b",
    "name": "dotnet",
    "commandLine": "\"C:\\Program Files\\dotnet\\dotnet.exe\" ConsoleApp1.dll",
    "operatingSystem": "Windows",
    "processArchitecture": "x64"
}
```

## TraceProfile

Enumeration that describes the type of diagnostic trace to capture. Each profile represents a list of event providers, event levels, and keywords.

| Name | Description |
|---|---|
| `Cpu` | Tracks CPU usage and general .NET runtime information. |
| `Http` | Tracks ASP[]().NET request handling and HttpClient requests. |
| `Logs` | Tracks log events emitted at the `Debug` [log level](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventlevel) or higher. |
| `Metrics` | Tracks [event counters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters) from the `System.Runtime`, `Microsoft.AspNetCore.Hosting`, and `Grpc.AspNetCore.Server` event sources. |

## ValidationProblemDetails

Object for specifying errors and validation results based on https://tools.ietf.org/html/rfc7807

| Name | Type | Description |
|---|---|---|
| `detail` | string | An explanation specific to this occurrence of the problem. |
| `errors` | map (of string[]) | (Optional) The validation errors related to the request. |
| `extensions` | map (of object) | (Optional) Extension members containing additional information about the problem. |
| `instance` | string | (Optional) A URI reference that identifies the specific occurrence of the problem. |
| `status` | int | The HTTP status code generated by the origin server for this occurrence of the problem. |
| `title` | string | (Optional) A short summary of the problem type. |
| `type` | string | (Optional) A URI reference that identifies the problem type. |

### Example

```json
{
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "Providers": [
            "The Providers field is required."
        ]
    }
}
```

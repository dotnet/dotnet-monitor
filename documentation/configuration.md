# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Ordinarily, you are not required to specify most of this configuration and only exists if you wish to change the default behavior in `dotnet monitor`.

## Configuration Sources

`dotnet monitor` can read and combine configuration from multiple sources. The configuration sources are listed below in the order in which they are read (Environment variables are highest precedence) :

- Command line parameters
- User settings path
  - On Windows, `%USERPROFILE%\.dotnet-monitor\settings.json`
  - On \*nix, `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`
  -  If `$XDG_CONFIG_HOME` isn't defined, we fall back to ` $HOME/.config/dotnet-monitor/settings.json`
- [Key-per-file](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/#key-per-file-configuration-provider) in the shared settings path
    - On Windows, `%ProgramData%\dotnet-monitor`
    - On \*nix, `/etc/dotnet-monitor`

- Environment variables

### Translating configuration between providers

While the rest of this document will showcase configuration examples in a json format, the same configuration can be expressed via any of the other configuration sources. For example, the API Key configuration can be expressed via shown below:

```json
{
  "ApiAuthentication": {
    "ApiKeyHash": "CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683",
    "ApiKeyHashType": "SHA256"
  }
}
```

The same configuration can be expressed via environment variables using the `DotnetMonitor_` prefix and using `__`(double underscore) as the hierarchical separator

```bash
export DotnetMonitor_ApiAuthentication__ApiKeyHash="CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683"
export DotnetMonitor_ApiAuthentication__ApiKeyHashType="SHA256"
```

#### Kubernetes

When running in Kubernetes, you are able to specify the same configuration via Kubernetes secrets.

```bash
kubectl create secret generic apikey \
  --from-literal=ApiAuthentication__ApiKeyHash=$hash \
  --from-literal=ApiAuthentication__ApiKeyHashType=SHA256 \
  --dry-run=client -o yaml \
  | kubectl apply -f -
```

You can then use a Kubernetes volume mount to supply the secret to the container at runtime.

```yaml 
spec:
  volumes:
  - name: config
    secret:
      secretName: apikey
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/dotnet-monitor:6.0.0-preview.8
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

Alternatively, you can also use configuration maps to specify configuration to the container at runtime.

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: my-configmap
data:
  Metrics__MetricCount: "6"
```

You can then use a Kubernetes volume mount to supply the configuration map to the container at runtime

```yaml 
spec:
  volumes:
  - name: config
    configmap:
      name: my-configmap
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/dotnet-monitor:6.0.0-preview.8
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

If using multiple configuration maps, secrets, or some combination of both, you need to use a [projected volume](https://kubernetes.io/docs/concepts/storage/volumes/#projected) to map serveral volume sources into a single directory

```yaml 
spec:
  volumes:
  - name: config
    projected:
      sources:
        - secret:
            name: apiKey
        - configMap:
            name: my-configmap
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/dotnet-monitor:6.0.0-preview.8
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```


## Configuration Schema

`dotnet monitor`'s various configuration knobs have been documented via JSON schema. Using a modern editor like VS or VS Code that supports JSON Schema makes it trivial to author complex configuration objects with support for completions and rich descriptions via tooltips.

To get completion support in your editor, simply add the `$schema` property to the root JSON object as shown below:

```json
{
  "$schema": "https://aka.ms/dotnet-monitor-schema"
}
```

Once you've added the `$schema` property, you should have support for completions in your editor.

![completions](https://user-images.githubusercontent.com/4734691/115377729-bf2bb600-a184-11eb-9b8e-50f361c112f0.gif)

## View Merged Configuration

`dotnet monitor` includes a diagnostic command that allows you to output the resulting configuration after merging the configuration from all the various sources.

To view the merged configuration, run the following command:

```cmd
dotnet monitor config show
```
The output of the command should resemble the following JSON object:

```json
{
  "urls": "https://localhost:52323",
  "Kestrel": ":NOT PRESENT:",
  "CorsConfiguration": ":NOT PRESENT:",
  "DiagnosticPort": {
    "ConnectionMode": "Connect",
    "EndpointName": null
  },
  "Metrics": {
    "Enabled": "True",
    "Endpoints": "http://*:52325",
    "IncludeDefaultProviders": "True",
    "MetricCount": "3",
    "Providers": {
      "0": {
        "CounterNames": {
          "0": "connections-per-second",
          "1": "total-connections"
        },
        "ProviderName": "Microsoft-AspNetCore-Server-Kestrel"
      }
    },
  },
  "Storage": {
    "DumpTempFolder": "C:\\Users\\shirh\\AppData\\Local\\Temp\\"
  },
  "ApiAuthentication": {
    "ApiKeyHash": ":REDACTED:",
    "ApiKeyHashType": "SHA256"
  },
  "Egress": ":NOT PRESENT:"
}
```

To view the loaded configuration providers, run the following command:

```cmd
dotnet monitor config show --show-sources
```

## Diagnostic Port Configuration

`dotnet monitor` communicates via .NET processes through their diagnostic port. In the default configuration, .NET processes listen on a platform native transport (named pipes on Windows/Unix-domain sockets on \*nix) in a well-known location.

### Connection Mode

It is possible to change this behavior and have .NET processes connect to `dotnet monitor`. This allow you to monitor a process from start and collect traces for events such as assembly load events that primarily occur at process startup and weren't possible to collect previously.

```json
  "DiagnosticPort": "\\\\.\\pipe\\dotnet-monitor-pipe"
```

Alternatively, `dotnet monitor` can be set to `Listen` mode using the expanded format. In the event of conflicting configuration, the simplified format will take priority over the expanded format.

```json
  "DiagnosticPort": {
    "ConnectionMode": "Listen",
    "EndpointName": "\\\\.\\pipe\\dotnet-monitor-pipe"
  }
```

When `dotnet monitor` is in `Listen` mode, you have to configure .NET processes to connect to `dotnet monitor`. You can do so by specifying the appropriate environment variable on your .NET process

```powershell
$env:DOTNET_DiagnosticPorts="dotnet-monitor-pipe,suspend"
```

#### Maximum connection

When operating in `Listen` mode, you can also specify the maximum number of incoming connections for `dotnet monitor` to accept via the following configuration:

```json
  "DiagnosticPort": {
    "MaxConnections": "10"
  }
```

## Kestrel Configuration

// TODO

## Storage Configuration

Unlike the other diagnostic artifacts (for example, traces), memory dumps aren't streamed back from the target process to `dotnet monitor`. Instead, they are written directly to disk by the runtime. After successful collection of a process dump, `dotnet monitor` will read the process dump directly from disk. In the default configuration, the directory that the runtime writes its process dump to is the temp directory (`%TMP%` on Windows, `/tmp` on \*nix). It is possible to change to the ephemeral directory that these dump files get written to via the following configuration:

```json
{
  "Storage": {
    "DumpTempFolder": "/ephemeral-directory/"
  }
}
```

## Default Process Configuration

Default process configuration is used to determine which process is used for metrics and in situations where the process is not specified in the query to retrieve an artifact. A process must match all the specified filters.

| Name | Type | Description |
|---|---|---|
| Key | string | Specifies which criteria to match on the process. Can be `ProcessId`, `ProcessName`, `CommandLine`. |
| Value | string | The value to match against the process. |
| MatchType | string | The type of match to perform. Can be `Exact` or `Contains` for sub-string matching. Both are case-insensitive.|

### Examples

Match the iisexpress process by name

```json
{
  "DefaultProcess": {
    "Filters": [{
      "Key": "ProcessName",
      "Value": "iisexpress"
    }]
  },
}
```

Match pid 1
```json
{
  "DefaultProcess": {
    "Filters": [{
      "Key": "ProcessId",
      "Value": "1",
    }]
  },
}
```

## Cross-Origin Resource Sharing (CORS) Configuration

// TODO

## Metrics Configuration

### Global Counter Interval

Due to limitations in event counters, `dotnet-monitor` supports only **one** refresh interval when collecting metrics. This interval is used for
Prometheus metrics, livemetrics, triggers, traces, and trigger actions that collect traces. The default interval is 5 seconds, but can be changed in configuration.

```json
{
    "GlobalCounter": {
      "IntervalSeconds": 10
    }
}
```

### Metrics Urls

In addition to the ordinary diagnostics urls that `dotnet monitor` binds to, it also binds to metric urls that only expose the `/metrics` endpoint. Unlike the other endpoints, the metrics urls do not require authentication. Unless you enable collection of custom providers that may contain sensitive business logic, it is generally considered safe to expose metrics endpoints. 

Metrics urls can be configured via the command line:

```cmd
dotnet monitor collect --metricUrls http://*:52325/
```

Or configured via a configuration file:

```json
{
  "Metrics": {
    "Endpoints": "http://localhost:52325"
  }
}
```

### Customize collection interval and counts

In the default configuration, `dotnet monitor` requests that the connected runtime provides updated counter values every 5 seconds and will retain 3 data points for every collected metric. When using a collection tool like Prometheus, it is recommended that you set your scrape interval to `MetricCount` * `GlobalCounter:IntervalSeconds`. In the default configuration, we recommend you scrape `dotnet monitor` for metrics every 15 seconds.

You can customize the number of data points stored per metric via the following configuration:

```json
{
  "Metrics": {
    "MetricCount": 3,
  }
}
```

See [Global Counter Interval](#Global-Counter-Interval) to change the metrics frequency.

### Custom Metrics

Additional metrics providers and counter names to return from this route can be specified via configuration. 

```json
{
  "Metrics": {
    "Providers": [
      {
        "ProviderName": "Microsoft-AspNetCore-Server-Kestrel",
        "CounterNames": [
          "connections-per-second",
          "total-connections"
        ]
      }
    ]
  }
}
```

> **Warning:** In the default configuration, custom metrics will be exposed along with all other metrics on an unauthenticated endpoint. If your metrics contains sensitive information, we recommend disabling the [metrics urls](#metrics-urls) and consuming metrics from the authenticated endpoint (`--urls`) instead.

When `CounterNames` are not specified, all the counters associated with the `ProviderName` are collected.

### Disable default providers

In addition to enabling custom providers, `dotnet monitor` also allows you to disable collection of the default providers. You can do so via the following configuration:

```json
{
  "Metrics": {
    "IncludeDefaultProviders": false
  }
}
```

## Egress Configuration

### Azure blob storage egress provider

| Name | Type | Description |
|---|---|---|
| accountUri | string | The URI of the Azure blob storage account.|
| containerName | string | The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.|
| blobPrefix | string | Optional path prefix for the artifacts to egress.|
| copyBufferSize | string | The buffer size to use when copying data from the original artifact to the blob stream.|
| accountKey | string | The account key used to access the Azure blob storage account.|
| sharedAccessSignature | string | The shared access signature (SAS) used to access the azure blob storage account.|
| accountKeyName | string | Name of the property in the Properties section that will contain the account key.|
| sharedAccessSignatureName | string | Name of the property in the Properties section that will contain the SAS token.|

### Example azureBlobStorage provider

```json
{
    "Egress": {
        "AzureBlobStorage": {
            "monitorBlob": {
                "accountUri": "https://exampleaccount.blob.core.windows.net",
                "containerName": "dotnet-monitor",
                "blobPrefix": "artifacts",
                "accountKeyName": "MonitorBlobAccountKey"
            }
        },
        "Properties": {
            "MonitorBlobAccountKey": "accountKey"
        }
    }
}
```

### Filesystem egress provider

| Name | Type | Description |
|---|---|---|
| directoryPath | string | The directory path to which the stream data will be egressed.|
| intermediateDirectoryPath | string | The directory path to which the stream data will initially be written, if specified; the file will then be moved/renamed to the directory specified in 'directoryPath'.|

### Example fileSystem provider

```json
{
    "Egress": {
        "FileSystem": {
            "monitorFile": {
                "directoryPath": "/artifacts",
                "intermediateDirectoryPath": "/intermediateArtifacts"
            }
        }
    }
}
```

## Collection Rule Configuration

Collection rules are specified in configuration as a named item under the `CollectionRules` property at the root of the configuration. Each collection rule has four properties for describing the behavior of the rule: `Filters`, `Trigger`, `Actions`, and `Limits`.

### Example

The following is a collection rule that collects a 1 minute CPU trace after it has detected high CPU usage for 10 seconds. The rule only applies to processes named "dotnet" and only collects at most 2 traces per 1 hour sliding time window.

```json
{
    "CollectionRules": {
        "HighCpuRule": {
            "Filters": [{
                "Key": "ProcessName",
                "Value": "dotnet",
                "MatchType": "Exact"
            }],
            "Trigger": {
                "Type": "EventCounter",
                "Settings": {
                    "ProviderName": "System.Runtime",
                    "CounterName": "cpu-usage",
                    "GreaterThan": 70,
                    "SlidingWindowDuration": "00:00:10"
                }
            },
            "Actions": [{
                "Type": "CollectTrace",
                "Settings": {
                    "Profile": "Cpu",
                    "Duration": "00:01:00"
                }
            }],
            "Limits": {
                "ActionCount": 2,
                "ActionCountSlidingWindowDuration": "1:00:00"
            }
        }
    }
}
```

### Filters

Each collection rule can specify a set of process filters to select which processes the rule should be applied. The filter criteria are the same as those used for the [default process](#Default-Process-Configuration) configuration.

#### Example

The following example shows the `Filters` portion of a collection rule that has the rule only apply to processes named "dotnet" and whose command line contains "myapp.dll".

```json
{
    "Filters": [{
        "Key": "ProcessName",
        "Value": "dotnet",
        "MatchType": "Exact"
    },{
        "Key": "CommandLine",
        "Value": "myapp.dll",
        "MatchType": "Contains"
    }]
}
```

### Triggers

#### `AspNetRequestCount` Trigger

A trigger that has its condition satisfied when the number of HTTP requests is above the described threshold level for a sliding window of time. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of requests that start within the sliding window of time. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when request count is higher than 500 requests during a 1 minute period for all paths under the `/api` route:

```json
{
  "RequestCount": 500,
  "SlidingWindowDuration": "00:01:00",
  "IncludePaths": [ "/api/**/*" ]
}
```

#### `AspNetRequestDuration` Trigger

A trigger that has its condition satisfied when the number of HTTP requests have response times longer than the threshold duration for a sliding window of time. Long running requests (ones that do not send a complete response within the threshold duration) are included in the count. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of slow requests that start within the sliding window of time. | | | |
| `RequestDuration` | Timespan? | false | The threshold of the amount of time in which a request is considered to be slow. | `"00:00:05"` (5 seconds) | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the the number of slow requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when 10 requests take longer than 3 seconds during a 1 minute period for all paths under the `/api` route:

```json
{
  "RequestCount": 10,
  "RequestDuration": "00:00:03",
  "SlidingWindowDuration": "00:01:00",
  "IncludePaths": [ "/api/**/*" ]
}
```

#### `AspNetResponseStatus` Trigger

A trigger that has its condition satisfied when the number of HTTP responses that have status codes matching the pattern list is above the specified threshold for a sliding window of time. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `StatusCodes` | string[] | true | The list of HTTP response status codes to monitor. Each item of the list can be a single code or a range of codes (e.g. `"400-499"`). | | | |
| `RequestCount` | int | true | The threshold number of responses with matching status codes. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of responses with matching status codes must occur. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when 10 requests respond with a 5XX status code during a 1 minute period for all paths under the `/api` route:

```json
{
  "StatusCodes": [ "500-599" ],
  "RequestCount": 10,
  "SlidingWindowDuration": "00:01:00",
  "IncludePaths": [ "/api/**/*" ]
}
```

#### `EventCounter` Trigger

A trigger that has its condition satisfied when the value of a counter falls above, below, or between the described threshold values for a duration of time.

See [Well-known Counters in .NET](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters) for a list of known available counters. Custom counters from custom event sources are supported as well.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ProviderName` | string | true | The name of the event source that provides the counter information. | | | |
| `CounterName` | string | true | The name of the counter to monitor. | | | |
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

##### Example

Usage that is satisfied when the CPU usage of the application is higher than 70% for a 10 second window.

```json
{
  "ProviderName": "System.Runtime",
  "CounterName": "cpu-usage",
  "GreaterThan": 70,
  "SlidingWindowDuration": "00:00:10"
}
```

#### ASP.NET Request Path Wildcards and Globbing

The `IncludePaths` and `ExcludePaths` properties of the ASP.NET triggers allow for wildcards and globbing so that every included or excluded path does not necessarily need to be explicitly specified. For these triggers, a match with an `ExcludePaths` pattern will supercede a match with an `IncludePaths` pattern.

The globstar `**/` will match zero or more path segments including the forward slash `/` character at the end of the segment.

The wildcard `*` will match zero or more non-forward-slash `/` characters.

##### Examples

| Pattern | Matches | Non-Matches |
|---|---|---|
| `**/*` | All paths | No exclusions |
| `/images/**/*` | `/images/logo.png`, `/images/products/1.png` | `/index/header.png` |
| `**/*.js` | `/script.js`, `/path/script.js`, `/path/sub/script.js` | `/script.js/page.html` |
| `**/sub/*.html` | `/path/sub/page.html`, `/sub/page.html` | `/sub/script.js`, `/path/doc.txt` |

### Actions

#### `CollectDump` Action

An action that collects a dump of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Type` | [DumpType](api/definitions.md#DumpType) | false | The type of dump to collect | `WithHeap` |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected dump. | |

##### Example

Usage that collects a full dump and egresses it to a provider named "AzureBlobDumps".

```json
{
  "Type": "Full",
  "Egress": "AzureBlobDumps"
}
```

#### `CollectGCDump` Action

An action that collects a gcdump of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected gcdump. | |

##### Example

Usage that collects a gcdump and egresses it to a provider named "AzureBlobGCDumps".

```json
{
  "Egress": "AzureBlobGCDumps"
}
```

#### `CollectTrace` Action

An action that collects a trace of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `Profile` | [TraceProfile](api/definitions.md#TraceProfile)? | false | The name of the profile(s) used to collect events. See [TraceProfile](api/definitions.md#TraceProfile) for details on the list of event providers, levels, and keywords each profile represents. Multiple profiles may be specified by separating them with commas. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `Providers` | [EventProvider](api/definitions.md#EventProvider)[] | false | List of event providers from which to capture events. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `RequestRundown` | bool | false | The runtime may provide additional type information for certain types of events after the trace session is ended. This additional information is known as rundown events. Without this information, some events may not be parsable into useful information. Only applies when `Providers` is specified. | `true` | | |
| `BufferSizeMegabytes` | int | false | The size (in megabytes) of the event buffer used in the runtime. If the event buffer is filled, events produced by event providers may be dropped until the buffer is cleared. Increase the buffer size to mitigate this or pair down the list of event providers, keywords, and level to filter out extraneous events. Only applies when `Providers` is specified. | `256` | `1` | `1024` |
| `Duration` | TimeSpan? | false | The duration of the trace operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected trace. | | | |

##### Example

Usage that collects a CPU trace for 30 seconds and egresses it to a provider named "TmpDir".

```json
{
  "Profile": "Cpu",
  "Egress": "TmpDir"
}
```

#### `CollectLogs` Action

An action that collects logs for the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `DefaultLevel` | [LogLevel](api/definitions.md#LogLevel)? | false | The default log level at which logs are collected for entries in the FilterSpecs that do not have a specified LogLevel value. | `LogLevel.Warning` | | |
| `FilterSpecs` | Dictionary<string, [LogLevel](api/definitions.md#LogLevel)?> | false | A custom mapping of logger categories to log levels that describes at what level a log statement that matches one of the given categories should be captured. | `null` | | |
| `UseAppFilters` | bool | false | Specifies whether to capture log statements at the levels as specified in the application-defined filters. | `true` | | |
| `Format` | [LogFormat](api/definitions.md#LogFormat)? | false | The format of the logs artifact. | `PlainText` | | |
| `Duration` | TimeSpan? | false | The duration of the logs operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected logs. | | | |

##### Example

Usage that collects logs at the Information level for 30 seconds and egresses it to a provider named "TmpDir".

```json
{
  "DefaultLevel": "Information",
  "UseAppFilters": false,
  "Egress": "TmpDir"
}
```

#### `Execute` Action

An action that executes an executable found in the file system. Non-zero exit code will fail the action.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path to the executable. | |
| `Arguments` | string | false | The arguments to pass to the executable. | `null` |
| `IgnoreExitCode` | bool? | false | Ignores checking that the exit code is zero. | `false` |

##### Example

Usage that executes a .NET executable named "myapp.dll" using `dotnet`.

```json
{
  "Path": "C:\\Program Files\\dotnet\\dotnet.exe",
  "Arguments": "C:\\Program Files\\MyApp\\myapp.dll"
}
```

#### `LoadProfiler` Action

An action that loads an ICorProfilerCallback implementation into a target process as a startup profiler. This action must be used in a collection rule with a `Startup` trigger.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path of the profiler library to be loaded. This is typically the same value that would be set as the CORECLR_PROFILER_PATH environment variable. | |
| `Clsid` | Guid | true | The class identifier (or CLSID, typically a GUID) of the ICorProfilerCallback implementation. This is typically the same value that would be set as the CORECLR_PROFILER environment variable. | |

##### Outputs

No outputs

##### Example

Usage that loads one of the sample profilers from [`dotnet/runtime`: src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp](https://github.com/dotnet/runtime/blob/9ddd58a58d14a7bec5ed6eb777c6703c48aca15d/src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp).

```json
{
  "Path": "Profilers\\Profiler.dll",
  "Clsid": "55b9554d-6115-45a2-be1e-c80f7fa35369"
}
```

#### `SetEnvironmentVariable` Action

An action that sets an environment variable value in the target process. This action should be used in a collection rule with a `Startup` trigger.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to set. | |
| `Value` | string | false | The value of the environment variable to set. | `null` |

##### Outputs

No outputs

##### Example

Usage that sets a parameter to the profiler you loaded. In this case, your profiler might be looking for an account key defined in `MyProfiler_AccountId` which is used to communicate to some outside system.

```json
{
  "Name": "MyProfiler_AccountId",
  "Value": "8fb138d2c44e4aea8545cc2df541ed4c"
}
```

#### `GetEnvironmentVariable` Action

An action that gets an environment varaible from the target process. Its value is set as the `Value` action output.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to get. | |

##### Outputs

| Name | Description |
|---|---|
| `Value` | The value of the environment variable in the target process. |

##### Example

Usage that gets a token your app has access to and uses it to send a trace.

***Note:*** the example below is of an entire action list to provide context, only the second json entry represents the `GetEnvironmentVariable` Action.

```json
[{
    "Name": "A",
    "Type": "CollectTrace",
    "Settings": {
        "Profile": "Cpu",
        "Egress": "AzureBlob"
    }
},{
    "Name": "GetEnvAction",
    "Type": "GetEnvironmentVariable",
    "Settings": {
       "Name": "Azure_SASToken",
    }
},{
    "Name": "B",
    "Type": "Execute",
    "Settings": {
        "Path": "azcopy",
        "Arguments": "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
    }
}]
```

### Limits

Collection rules have limits that constrain the lifetime of the rule and how often its actions can be run before being throttled.

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ActionCount` | int | false | The number of times the action list may be executed before being throttled. | 5 | | |
| `ActionCountSlidingWindowDuration` | TimeSpan? | false | The sliding window of time to consider whether the action list should be throttled based on the number of times the action list was executed. Executions that fall outside the window will not count toward the limit specified in the ActionCount setting. If not specified, all action list executions will be counted for the entire duration of the rule. | `null` | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `RuleDuration` | TimeSpan? | false | The amount of time before the rule will stop monitoring a process after it has been applied to a process. If not specified, the rule will monitor the process with the trigger indefinitely. | `null` | `"00:00:01"` (1 second) | `"365.00:00:00"` (1 year) |

#### Example

The following example shows the `Limits` portion of a collection rule that has the rule only allow its actions to run 3 times within a 1 hour sliding time window.

```json
{
    "Limits": {
        "ActionCount": 3,
        "ActionCountSlidingWindowDuration": "01:00:00",
    }
}
```

# Collection Rule Configuration

`dotnet monitor` can be configured to automatically collect diagnostic artifacts based on conditions within the discovered processes.

Collection rules are specified in configuration as a named item under the `CollectionRules` property at the root of the configuration. Each collection rule has four properties for describing the behavior of the rule:
- [`Filters`](#filters) - A set of process filters to select which processes the rule should be applied
- [`Trigger`](#triggers) - The metric and it's threshold that will be used to trigger the action
  - [AspNetRequestCount](#aspnetrequestcount-trigger)
  - [AspNetRequestDuration](#aspnetrequestduration-trigger)
  - [AspNetResponseStatus](#aspnetresponsestatus-trigger)
  - [EventCounter](#eventcounter-trigger)
  - [EventMeter](#eventmeter-trigger-80)
  - [Trigger shortcuts](../collectionrules/triggershortcuts.md)
- [`Actions`](#actions) - The action to be be performed
  - [CollectDump](#collectdump-action)
  - [CollectExceptions](#collectexceptions-action)
  - [CollectGCDump](#collectgcdump-action)
  - [CollectTrace](#collecttrace-action)
  - [CollectLiveMetrics](#collectlivemetrics-action)
  - [CollectLogs](#collectlogs-action)
  - [Execute](#execute-action)
  - [CollectStacks](#collectstacks-action)
  - [LoadProfiler](#loadprofiler-action)
  - [SetEnvironmentVariable](#setenvironmentvariable-action)
  - [GetEnvironmentVariable](#getenvironmentvariable-action)
- [`Limits`](#limits) - Constrains the lifetime of the rule and how often its actions can be run before being throttled

## Example

<
The following is a collection rule that collects a 1 minute CPU trace and egresses it to a provider named "TmpDir" after it has detected high CPU usage for 10 seconds. The rule only applies to processes named "dotnet" and only collects at most 2 traces per 1 hour sliding time window.

<details>
  <summary>JSON</summary>

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
                      "Duration": "00:01:00",
                      "Egress": "TmpDir"
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
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__HighCpuRule__Filters__0__Key: "ProcessName"
  CollectionRules__HighCpuRule__Filters__0__Value: "dotnet"
  CollectionRules__HighCpuRule__Filters__0__MatchType: "Exact"
  CollectionRules__HighCpuRule__Trigger__Type: "EventCounter"
  CollectionRules__HighCpuRule__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__HighCpuRule__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__HighCpuRule__Trigger__Settings__GreaterThan: "70"
  CollectionRules__HighCpuRule__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  CollectionRules__HighCpuRule__Actions__0__Type: "CollectTrace"
  CollectionRules__HighCpuRule__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__HighCpuRule__Actions__0__Settings__Duration: "00:01:00"
  CollectionRules__HighCpuRule__Actions__0__Settings__Egress: "TmpDir"
  CollectionRules__HighCpuRule__Limits__ActionCount: "2"
  CollectionRules__HighCpuRule__Limits__ActionCountSlidingWindowDuration: "1:00:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__Value
    value: "dotnet"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__MatchType
    value: "Exact"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__GreaterThan
    value: "70"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Duration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Egress
    value: "TmpDir"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Limits__ActionCount
    value: "2"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Limits__ActionCountSlidingWindowDuration
    value: "1:00:00"
  ```
</details>

## Filters

Each collection rule can specify a set of process filters to select which processes the rule should be applied. The filter criteria are the same as those used for the [default process](./default-process-configuration.md) configuration.

### Example

The following example shows the `Filters` portion of a collection rule that has the rule only apply to processes named `dotnet` and whose command line contains `myapp.dll`.

<details>
  <summary>JSON</summary>

  ```json
  {
      "Filters": [{
          "Key": "ProcessName",
          "Value": "dotnet",
          "MatchType": "Exact"
      },{
          "CommandLine": "myapp.dll",
          "MatchType": "Contains"
      }]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Filters__0__Key: "ProcessName"
  CollectionRules__RuleName__Filters__0__Value: "dotnet"
  CollectionRules__RuleName__Filters__0__MatchType: "Exact"
  CollectionRules__RuleName__Filters__1__CommandLine: "myapp.dll"
  CollectionRules__RuleName__Filters__1__MatchType: "Contains"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__Value
    value: "dotnet"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__MatchType
    value: "Exact"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__1__CommandLine
    value: "myapp.dll"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__1__MatchType
    value: "Contains"
  ```
</details>

## Triggers

A trigger will monitor for a specific condition in the target application and raise a notification when that condition has been observed.

### `AspNetRequestCount` Trigger

A trigger that has its condition satisfied when the number of HTTP requests is above the described threshold level for a sliding window of time. The request paths can be filtered according to the described patterns.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of requests that start within the sliding window of time. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

#### Example

Usage that is satisfied when request count is higher than 500 requests during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "RequestCount": 500,
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "500"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "500"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

### `AspNetRequestDuration` Trigger

A trigger that has its condition satisfied when the number of HTTP requests have response times longer than the threshold duration for a sliding window of time. Long running requests (ones that do not send a complete response within the threshold duration) are included in the count. The request paths can be filtered according to the described patterns.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of slow requests that start within the sliding window of time. | | | |
| `RequestDuration` | TimeSpan? | false | The threshold of the amount of time in which a request is considered to be slow. | `"00:00:05"` (5 seconds) | `"00:00:00"` (zero seconds) | `"01:00:00"` (1 hour) |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the the number of slow requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

#### Example

Usage that is satisfied when 10 requests take longer than 3 seconds during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "RequestCount": 10,
    "RequestDuration": "00:00:03",
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "10"
  CollectionRules__RuleName__Trigger__Settings__RequestDuration: "00:00:03"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestDuration
    value: "00:00:03"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

### `AspNetResponseStatus` Trigger

A trigger that has its condition satisfied when the number of HTTP responses that have status codes matching the pattern list is above the specified threshold for a sliding window of time. The request paths can be filtered according to the described patterns.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `StatusCodes` | string[] | true | The list of HTTP response status codes to monitor. Each item of the list can be a single code or a range of codes (e.g. `"400-499"`). | | | |
| `RequestCount` | int | true | The threshold number of responses with matching status codes. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of responses with matching status codes must occur. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

#### Example

Usage that is satisfied when 10 requests respond with a 5XX status code during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "StatusCodes": [ "500-599" ],
    "RequestCount": 10,
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__StatusCodes__0: "500-599"
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "10"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__StatusCodes__0
    value: "500-599"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

### `EventCounter` Trigger

A trigger that has its condition satisfied when the value of a counter falls above, below, or between the described threshold values for a duration of time.

See [Well-known Counters in .NET](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters) for a list of known available counters. Custom counters from custom event sources are supported as well.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ProviderName` | string | true | The name of the event source that provides the counter information. | | | |
| `CounterName` | string | true | The name of the counter to monitor. | | | |
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Example

Usage that is satisfied when the CPU usage of the application is higher than 70% for a 10 second window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "ProviderName": "System.Runtime",
    "CounterName": "cpu-usage",
    "GreaterThan": 70,
    "SlidingWindowDuration": "00:00:10"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "70"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "70"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  ```
</details>

### `EventMeter` Trigger (8.0+)

A trigger that has its condition satisfied when the value of an instrument falls above, below, or between the described threshold value for a duration of time. Supported instruments include [Gauges](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.observablegauge-1), [Counters](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1), and [Histograms](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1).

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `MeterName` | string | true | The name of the meter that provides the instrument information. | | | |
| `InstrumentName` | string | true | The name of the instrument to monitor. | | | |
| `GreaterThan` | double? | false | The threshold level the instrument must maintain (or higher) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified for non-histogram instruments. | `null` | | |
| `LessThan` | double? | false | The threshold level the instrument must maintain (or lower) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified for non-histogram instruments. | `null` | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the instrument must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `HistogramPercentile` | int? | false | The histogram percentile should be one of the instrument's published percentiles (by default: 50, 95, and 99) and is only specified when the instrument is a histogram. The provided percentile's value will be used to compare against `GreaterThan` and/or `LessThan`. | | 0 | 100 |

#### Example

Usage that is satisfied when the target application's custom gauge is greater than 20 for a 10 second window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "MeterName": "MyMeterName",
    "InstrumentName": "MyGaugeName",
    "GreaterThan": 20,
    "SlidingWindowDuration": "00:00:10"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__MeterName: "MyMeterName"
  CollectionRules__RuleName__Trigger__Settings__InstrumentName: "MyGaugeName"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "20"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__MeterName
    value: "MyMeterName"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__InstrumentName
    value: "MyGaugeName"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "20"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  ```
</details>

#### Example

Usage that is satisfied when the target application's custom histogram for a 10 second window has its 50th Percentile greater than 200:

<details>
  <summary>JSON</summary>

  ```json
  {
    "MeterName": "MyMeterName",
    "InstrumentName": "MyHistogramName",
    "GreaterThan": 200,
    "HistogramPercentile": 50,
    "SlidingWindowDuration": "00:00:10"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Settings__MeterName: "MyMeterName"
  CollectionRules__RuleName__Trigger__Settings__InstrumentName: "MyHistogramName"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "200"
  CollectionRules__RuleName__Trigger__Settings__HistogramPercentile: "50"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__MeterName
    value: "MyMeterName"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__InstrumentName
    value: "MyGaugeName"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "200"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__HistogramPercentile
    value: "50"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  ```
</details>

### Built-In Default Triggers

These [trigger shortcuts](../collectionrules/triggershortcuts.md) simplify configuration for several common `EventCounter` providers.

### ASP.NET Request Path Wildcards and Globbing

The `IncludePaths` and `ExcludePaths` properties of the ASP.NET triggers allow for wildcards and globbing so that every included or excluded path does not necessarily need to be explicitly specified. For these triggers, a match with an `ExcludePaths` pattern will supersede a match with an `IncludePaths` pattern.

The globstar `**/` will match zero or more path segments including the forward slash `/` character at the end of the segment.

The wildcard `*` will match zero or more non-forward-slash `/` characters.

#### Examples

| Pattern | Matches | Non-Matches |
|---|---|---|
| `**/*` | All paths | No exclusions |
| `/images/**/*` | `/images/logo.png`, `/images/products/1.png` | `/index/header.png` |
| `**/*.js` | `/script.js`, `/path/script.js`, `/path/sub/script.js` | `/script.js/page.html` |
| `**/sub/*.html` | `/path/sub/page.html`, `/sub/page.html` | `/sub/script.js`, `/path/doc.txt` |

## Actions

Actions allow executing an operation or an external executable in response to a trigger condition being satisfied.

### `CollectDump` Action

An action that collects a dump of the process that the collection rule is targeting.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Type` | [DumpType](../api/definitions.md#dumptype) | false | The type of dump to collect | `WithHeap` |
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected dump. | |

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects a full dump and egresses it to a provider named "AzureBlobDumps".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Type": "Full",
    "Egress": "AzureBlobDumps"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Type: "Full"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlobDumps"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Type
    value: "Full"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlobDumps"
  ```
</details>

### `CollectExceptions` Action

First Available: 8.0 RC 1

An action that collects exceptions from the process that the collection rule is targeting.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected dump. | |
| `Format` | [ExceptionFormat](../api/definitions.md#exceptionformat)? | false | The format of the exception entries. | `PlainText` |
| `Filters` | [ExceptionsConfiguration](../api/definitions.md#exceptionsconfiguration)? | false | Determines which exceptions should be included/excluded in the result - note that this does not alter which [exceptions are collected](in-process-features-configuration.md#filtering).

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects exceptions as newline-delimited JSON and egresses it to a provider named "AzureBlobExceptions".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": "AzureBlobExceptions",
    "Format": "NewlineDelimitedJson"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlobExceptions"
  CollectionRules__RuleName__Actions__0__Settings__Format: "NewlineDelimitedJson"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlobExceptions"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Format
    value: "NewlineDelimitedJson"
  ```
</details>

### `CollectGCDump` Action

An action that collects a gcdump of the process that the collection rule is targeting.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected gcdump. | |

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects a gcdump and egresses it to a provider named "AzureBlobGCDumps".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": "AzureBlobGCDumps"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlobGCDumps"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlobGCDumps"
  ```
</details>

### `CollectTrace` Action

An action that collects a trace of the process that the collection rule is targeting.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `Profile` | [TraceProfile](../api/definitions.md#traceprofile)? | false | The name of the profile(s) used to collect events. See [TraceProfile](../api/definitions.md#traceprofile) for details on the list of event providers, levels, and keywords each profile represents. Multiple profiles may be specified by separating them with commas. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `Providers` | [EventProvider](../api/definitions.md#eventprovider)[] | false | List of event providers from which to capture events. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `RequestRundown` | bool | false | The runtime may provide additional type information for certain types of events after the trace session is ended. This additional information is known as rundown events. Without this information, some events may not be parsable into useful information. Only applies when `Providers` is specified. | `true` | | |
| `BufferSizeMegabytes` | int | false | The size (in megabytes) of the event buffer used in the runtime. If the event buffer is filled, events produced by event providers may be dropped until the buffer is cleared. Increase the buffer size to mitigate this or pair down the list of event providers, keywords, and level to filter out extraneous events. Only applies when `Providers` is specified. | `256` | `1` | `1024` |
| `Duration` | TimeSpan? | false | The duration of the trace operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected trace. | | | |
| `StoppingEvent` | [TraceEventFilter](../api/definitions.md#traceeventfilter)? | false | The event to watch for while collecting the trace, and once either the event is hit or the `Duration` is reached the trace will be stopped. This can only be specified if `Providers` is set. | `null` | | |

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects a CPU trace for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Profile": "Cpu",
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

### `CollectLiveMetrics` Action

An action that collects live metrics for the process that the collection rule is targeting.

> [!NOTE]
> **(8.0+)** If none of `IncludeDefaultProviders`, `Provider`, or `Meters` are specified, then the [metrics configuration](<metrics-configuration.md#metrics-configuration>) is used as the collection specification.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `IncludeDefaultProviders` | bool | false | Determines if the default counter providers should be used. | `true` | | |
| `Providers` | [EventMetricsProvider](../api/definitions.md#eventmetricsprovider)[] | false | The array of providers for metrics to collect. | | | |
| `Meters` | [EventMetricsMeter](../api/definitions.md#eventmetricsmeter)[] | false | (7.1+) The array of meters for metrics to collect. | | | |
| `Duration` | TimeSpan? | false | The duration of the live metrics operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected live metrics. | | | |

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects live metrics with the default providers for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

#### Example

Usage that collects live metrics for the `cpu-usage` counter on `System.Runtime` for 20 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "UseDefaultProviders": false,
    "Providers": [
      {
        "ProviderName": "System.Runtime",
        "CounterNames": [ "cpu-usage" ]
      }
    ],
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__UseDefaultProviders: "false"
  CollectionRules__RuleName__Actions__0__Settings__Providers__0__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Actions__0__Settings__Providers__0__CounterNames__0: "cpu-usage"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__UseDefaultProviders
    value: "false"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Providers__0__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Providers__0__CounterNames__0
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

### `CollectLogs` Action

An action that collects logs for the process that the collection rule is targeting.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `DefaultLevel` | [LogLevel](../api/definitions.md#loglevel)? | false | The default log level at which logs are collected for entries in the FilterSpecs that do not have a specified LogLevel value. | `LogLevel.Warning` | | |
| `FilterSpecs` | Dictionary<string, [LogLevel](../api/definitions.md#loglevel)?> | false | A custom mapping of logger categories to log levels that describes at what level a log statement that matches one of the given categories should be captured. | `null` | | |
| `UseAppFilters` | bool | false | Specifies whether to capture log statements at the levels as specified in the application-defined filters. | `true` | | |
| `Format` | [LogFormat](../api/definitions.md#logformat)? | false | The format of the logs artifact. | `PlainText` | | |
| `Duration` | TimeSpan? | false | The duration of the logs operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected logs. | | | |

#### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

#### Example

Usage that collects logs at the Information level for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultLevel": "Information",
    "UseAppFilters": false,
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__DefaultLevel: "Information"
  CollectionRules__RuleName__Actions__0__Settings__UseAppFilters: "false"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__DefaultLevel
    value: "Information"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

### `Execute` Action

An action that executes an executable found in the file system. Non-zero exit code will fail the action.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path to the executable. | |
| `Arguments` | string | false | The arguments to pass to the executable. | `null` |
| `IgnoreExitCode` | bool? | false | Ignores checking that the exit code is zero. | `false` |

#### Outputs

| Name | Description |
|---|---|
| `ExitCode` | The exit code of the process. |

#### Example

Usage that executes a .NET executable named `myapp.dll` using `dotnet`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Path": "C:\\Program Files\\dotnet\\dotnet.exe",
    "Arguments": "C:\\Program Files\\MyApp\\myapp.dll"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Path: "C:\\Program Files\\dotnet\\dotnet.exe"
  CollectionRules__RuleName__Actions__0__Settings__Arguments: "C:\\Program Files\\MyApp\\myapp.dll"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Path
    value: "C:\\Program Files\\dotnet\\dotnet.exe"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Arguments
    value: "C:\\Program Files\\MyApp\\myapp.dll"
  ```
</details>

### `CollectStacks` Action

First Available: 8.0 Preview 7

Collect call stacks from the target process.

> [!NOTE]
> This feature is not enabled by default and requires configuration to be enabled. The [in-process features](./../configuration/in-process-features-configuration.md) must be enabled since the call stacks feature uses shared libraries loaded into the target application for collecting the call stack information.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Format` | [CallStackFormat](../api/definitions.md#callstackformat) | false | The format of the collected call stack. | `Json` |
| `Egress` | string | true | The named [egress provider](../egress.md) for egressing the collected stacks. | |

### `LoadProfiler` Action

An action that loads an [ICorProfilerCallback](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-interface) implementation into a target process as a startup profiler. This action must be used in a collection rule with a `Startup` trigger.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path of the profiler library to be loaded. This is typically the same value that would be set as the CORECLR_PROFILER_PATH environment variable. | |
| `Clsid` | Guid | true | The class identifier (or CLSID, typically a GUID) of the ICorProfilerCallback implementation. This is typically the same value that would be set as the CORECLR_PROFILER environment variable. | |

#### Outputs

No outputs

#### Example

Usage that loads one of the sample profilers from [`dotnet/runtime: src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp`](https://github.com/dotnet/runtime/blob/9ddd58a58d14a7bec5ed6eb777c6703c48aca15d/src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp).

<details>
  <summary>JSON</summary>

  ```json
  {
    "Path": "Profilers\\Profiler.dll",
    "Clsid": "55b9554d-6115-45a2-be1e-c80f7fa35369"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Path: "Profilers\\Profiler.dll"
  CollectionRules__RuleName__Actions__0__Settings__Clsid: "55b9554d-6115-45a2-be1e-c80f7fa35369"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Path
    value: "Profilers\\Profiler.dll"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Clsid
    value: "55b9554d-6115-45a2-be1e-c80f7fa35369"
  ```
</details>

### `SetEnvironmentVariable` Action

An action that sets an environment variable value in the target process. This action should be used in a collection rule with a `Startup` trigger.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to set. | |
| `Value` | string | false | The value of the environment variable to set. | `null` |

#### Outputs

No outputs

#### Example

Usage that sets a parameter to the profiler you loaded. In this case, your profiler might be looking for an account key defined in `MyProfiler_AccountId` which is used to communicate to some outside system.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Name": "MyProfiler_AccountId",
    "Value": "8fb138d2c44e4aea8545cc2df541ed4c"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Name: "MyProfiler_AccountId"
  CollectionRules__RuleName__Actions__0__Settings__Value: "8fb138d2c44e4aea8545cc2df541ed4c"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Name
    value: "MyProfiler_AccountId"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Value
    value: "8fb138d2c44e4aea8545cc2df541ed4c"
  ```
</details>

### `GetEnvironmentVariable` Action

An action that gets an environment variable from the target process. Its value is set as the `Value` action output.

#### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to get. | |

#### Outputs

| Name | Description |
|---|---|
| `Value` | The value of the environment variable in the target process. |

#### Example

Usage that gets a token your app has access to and uses it to send a trace.

> [!NOTE]
> The example below is of an entire action list to provide context, only the second json entry represents the `GetEnvironmentVariable` Action.

<details>
  <summary>JSON</summary>

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
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Name: "A"
  CollectionRules__RuleName__Actions__0__Type: "CollectTrace"
  CollectionRules__RuleName__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlob"
  CollectionRules__RuleName__Actions__1__Name: "GetEnvAction"
  CollectionRules__RuleName__Actions__1__Type: "GetEnvironmentVariable"
  CollectionRules__RuleName__Actions__1__Settings__Name: "Azure_SASToken"
  CollectionRules__RuleName__Actions__2__Name: "B"
  CollectionRules__RuleName__Actions__2__Type: "Execute"
  CollectionRules__RuleName__Actions__2__Settings__Path: "azcopy"
  CollectionRules__RuleName__Actions__2__Settings__Arguments: "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Name
    value: "A"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlob"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Name
    value: "GetEnvAction"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Type
    value: "GetEnvironmentVariable"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Settings__Name
    value: "Azure_SASToken"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Name
    value: "B"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Type
    value: "Execute"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Settings__Path
    value: "azcopy"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Settings__Arguments
    value: "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
  ```
</details>

## Limits

Collection rules have limits that constrain the lifetime of the rule and how often its actions can be run before being throttled.

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ActionCount` | int | false | The number of times the action list may be executed before being throttled. | 5 | | |
| `ActionCountSlidingWindowDuration` | TimeSpan? | false | The sliding window of time to consider whether the action list should be throttled based on the number of times the action list was executed. Executions that fall outside the window will not count toward the limit specified in the ActionCount setting. If not specified, all action list executions will be counted for the entire duration of the rule. | `null` | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `RuleDuration` | TimeSpan? | false | The amount of time before the rule will stop monitoring a process after it has been applied to a process. If not specified, the rule will monitor the process with the trigger indefinitely. | `null` | `"00:00:01"` (1 second) | `"365.00:00:00"` (1 year) |

### Example

The following example shows the `Limits` portion of a collection rule that has the rule only allow its actions to run 3 times within a 1 hour sliding time window.

<details>
  <summary>JSON</summary>

  ```json
  {
      "Limits": {
          "ActionCount": 3,
          "ActionCountSlidingWindowDuration": "01:00:00"
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Limits__ActionCount: "3"
  CollectionRules__RuleName__Limits__ActionCountSlidingWindowDuration: "01:00:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Limits__ActionCount
    value: "3"
  - name: DotnetMonitor_CollectionRules__RuleName__Limits__ActionCountSlidingWindowDuration
    value: "01:00:00"
  ```
</details>

## Collection Rule Defaults

Collection rule defaults are specified in configuration as a named item under the `CollectionRuleDefaults` property at the root of the configuration. Defaults can be used to limit the verbosity of configuration, allowing frequently used values for collection rules to be assigned as defaults. The following are the currently supported collection rule defaults:

| Name | Section | Type | Applies To |
|---|---|---|---|
| `Egress` | `Actions` | string | [CollectDump](#collectdump-action), [CollectGCDump](#collectgcdump-action), [CollectTrace](#collecttrace-action), [CollectLiveMetrics](#collectlivemetrics-action), [CollectLogs](#collectlogs-action) |
| `SlidingWindowDuration` | `Triggers` | TimeSpan? | [AspNetRequestCount](#aspnetrequestcount-trigger), [AspNetRequestDuration](#aspnetrequestduration-trigger), [AspNetResponseStatus](#aspnetresponsestatus-trigger), [EventCounter](#eventcounter-trigger) |
| `RequestCount` | `Triggers` | int | [AspNetRequestCount](#aspnetrequestcount-trigger), [AspNetRequestDuration](#aspnetrequestduration-trigger) |
| `ResponseCount` | `Triggers` | int | [AspNetResponseStatus](#aspnetresponsestatus-trigger) |
| `ActionCount` | `Limits` | int | [Limits](#limits) |
| `ActionCountSlidingWindowDuration` | `Limits` | TimeSpan? | [Limits](#limits) |
| `RuleDuration` | `Limits` | TimeSpan? | [Limits](#limits) |

### Example

The following example includes a default egress provider that corresponds to the `FileSystem` egress provider named `artifacts`. The first action, `CollectDump`, is able to omit the `Settings` section by using the default egress provider. The second action, `CollectGCDump`, is using an egress provider other than the default, and specifies that it will egress to an `AzureBlobStorage` provider named `monitorBlob`.

<details>
  <summary>JSON</summary>

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
      },
      "FileSystem": {
        "artifacts": {
          "directoryPath": "/artifacts",
          "intermediateDirectoryPath": "/intermediateArtifacts"
        }
      }
    },
    "CollectionRuleDefaults": {
      "Actions": {
        "Egress": "artifacts"
      }
    },
    "CollectionRules": {
      "HighRequestCount": {
        "Trigger": {
          "Type": "AspNetRequestCount",
          "Settings": {
            "RequestCount": 10
          }
        },
        "Actions": [
          {
            "Type": "CollectDump"
          },
          {
            "Type": "CollectGCDump",
            "Settings": {
              "Egress": "monitorBlob"
            }
          }
        ]
      }
    }
  }
  ```
</details>


<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  Egress__FileSystem__artifacts__directoryPath: "/artifacts"
  Egress__FileSystem__artifacts__intermediateDirectoryPath: "/intermediateArtifacts"
  CollectionRuleDefaults__Actions__Egress: "artifacts"
  CollectionRules__HighRequestCount__Trigger__Type: "AspNetRequestCount"
  CollectionRules__HighRequestCount__Trigger__Settings__RequestCount: "10"
  CollectionRules__HighRequestCount__Actions__0__Type: "CollectDump"
  CollectionRules__HighRequestCount__Actions__1__Type: "CollectGCDump"
  CollectionRules__HighRequestCount__Actions__1__Settings__Egress: "monitorBlob"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  - name: DotnetMonitor_Egress__FileSystem__artifacts__directoryPath
    value: "/artifacts"
  - name: DotnetMonitor_Egress__FileSystem__artifacts__intermediateDirectoryPath
    value: "/intermediateArtifacts"
  - name: DotnetMonitor_CollectionRuleDefaults__Actions__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Type
    value: "CollectDump"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__1__Type
    value: "CollectGCDump"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__1__Settings__Egress
    value: "monitorBlob"
  ```
</details>

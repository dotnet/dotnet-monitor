# Collection Rule Examples

The following examples provide sample scenarios for using a collection rule. These templates can be copied directly into your configuration file with minimal adjustments to work with your application (for more information on configuring an egress provider, see [egress providers](../configuration/egress-configuration.md)), or they can be adjusted for your specific use-case. [Learn more about configuring collection rules](collectionrules.md).

## Collect Trace - Startup (`Startup` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "AssemblyLoadTraceOnStartup": {
      "Trigger": {
        "Type": "Startup"
      },
      "Actions": [
        {
          "Type": "CollectTrace",
          "Settings": {
            "Providers": [{
                "Name": "Microsoft-Windows-DotNETRuntime",
                "EventLevel": "Informational",
                "Keywords": "0xC"
            }],
            "Duration": "00:00:15",
            "Egress": "artifacts"
          }
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__AssemblyLoadTraceOnStartup__Trigger__Type: "Startup"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Type: "CollectTrace"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Name: "Microsoft-Windows-DotNETRuntime"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__EventLevel: "Informational"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Keywords: "0xC"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Duration: "00:00:15"
  CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Egress: "artifacts"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Trigger__Type
    value: "Startup"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Name
    value: "Microsoft-Windows-DotNETRuntime"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__EventLevel
    value: "Informational"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Keywords
    value: "0xC"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Duration
    value: "00:00:15"
  - name: DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Egress
    value: "artifacts"
  ```
</details>

### Explanation

This rule, named "AssemblyLoadTraceOnStartup", will trigger on a process's startup. When the rule is triggered, a trace will be collected for 15 seconds and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). The trace will capture events from an event provider named `Microsoft-Windows-DotNETRuntime`, and will collect events at or above the `Informational` level using the keyword `0xC` (a combination of the [`loader` and `binder` keywords](https://learn.microsoft.com/dotnet/fundamentals/diagnostics/runtime-loader-binder-events)). For more information on providers, refer to [Well Known Event Providers](https://docs.microsoft.com/dotnet/core/diagnostics/well-known-event-providers). The trace will request rundown by default, and the `BufferSizeInMB` has the default value of 256 MB.

## Collect GCDump - Heap Size (`EventCounter` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "LargeGCHeapSize": {
      "Trigger": {
        "Type": "EventCounter",
        "Settings": {
          "ProviderName": "System.Runtime",
          "CounterName": "gc-heap-size",
          "GreaterThan": 10
        }
      },
      "Actions": [
        {
          "Type": "CollectGCDump",
          "Settings": {
            "Egress": "artifacts"
          }
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__LargeGCHeapSize__Trigger__Type: "EventCounter"
  CollectionRules__LargeGCHeapSize__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__LargeGCHeapSize__Trigger__Settings__CounterName: "gc-heap-size"
  CollectionRules__LargeGCHeapSize__Trigger__Settings__GreaterThan: "10"
  CollectionRules__LargeGCHeapSize__Actions__0__Type: "CollectGCDump"
  CollectionRules__LargeGCHeapSize__Actions__0__Settings__Egress: "artifacts"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__CounterName
    value: "gc-heap-size"
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__GreaterThan
    value: "10"
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Actions__0__Type
    value: "CollectGCDump"
  - name: DotnetMonitor_CollectionRules__LargeGCHeapSize__Actions__0__Settings__Egress
    value: "artifacts"
  ```
</details>

### Explanation

This rule, named "LargeGCHeapSize", will trigger when the GC Heap Size exceeds 10 MB within the default sliding window duration (1 minute). If the rule is triggered, a GCDump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the GCDump to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Trace - High CPU Usage (`EventCounter` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "HighCpuUsage": {
      "Trigger": {
        "Type": "EventCounter",
        "Settings": {
          "ProviderName": "System.Runtime",
          "CounterName": "cpu-usage",
          "GreaterThan": 60,
          "SlidingWindowDuration": "00:00:10"
        }
      },
      "Actions": [
        {
          "Type": "CollectTrace",
          "Settings": {
            "Profile": "Cpu",
            "Egress": "artifacts"
          }
        }
      ],
      "Filters": [
        {
          "Key": "ProcessName",
          "Value": "MyProcessName"
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__HighCpuUsage__Trigger__Type: "EventCounter"
  CollectionRules__HighCpuUsage__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__HighCpuUsage__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__HighCpuUsage__Trigger__Settings__GreaterThan: "60"
  CollectionRules__HighCpuUsage__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  CollectionRules__HighCpuUsage__Actions__0__Type: "CollectTrace"
  CollectionRules__HighCpuUsage__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__HighCpuUsage__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__HighCpuUsage__Filters__0__Key: "ProcessName"
  CollectionRules__HighCpuUsage__Filters__0__Value: "MyProcessName"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__GreaterThan
    value: "60"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_CollectionRules__HighCpuUsage__Filters__0__Value
    value: "MyProcessName"
  ```
</details>

### Explanation

This rule, named "HighCpuUsage", will trigger when a process named "MyProcessName" causes CPU usage to exceed 60% for greater than 10 seconds. If the rule is triggered, a Cpu trace will be collected for the default duration (30 seconds), and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Logs - Custom Histogram Metrics (`EventMeter` Trigger)

First Available: 7.1

<details>
  <summary>JSON</summary>

  ```json
  {
    "HighHistogramValues": {
      "Trigger": {
        "Type": "EventMeter",
        "Settings": {
          "MeterName": "MyCustomMeter",
          "InstrumentName": "MyCustomHistogram",
          "HistogramPercentile": "95",
          "GreaterThan": 175
        }
      },
      "Actions": [
        {
          "Type": "CollectLogs",
          "Settings": {
            "Egress": "artifacts",
            "DefaultLevel": "Warning",
            "UseAppFilters": false,
            "Duration": "00:00:30"
          }
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__HighHistogramValues__Trigger__Type: "EventMeter"
  CollectionRules__HighHistogramValues__Trigger__Settings__MeterName: "MyCustomMeter"
  CollectionRules__HighHistogramValues__Trigger__Settings__InstrumentName: "MyCustomHistogram"
  CollectionRules__HighHistogramValues__Trigger__Settings__HistogramPercentile: "95"
  CollectionRules__HighHistogramValues__Trigger__Settings__GreaterThan: "175"
  CollectionRules__HighHistogramValues__Actions__0__Type: "CollectLogs"
  CollectionRules__HighHistogramValues__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__HighHistogramValues__Actions__0__Settings__DefaultLevel: "Warning"
  CollectionRules__HighHistogramValues__Actions__0__Settings__UseAppFilters: "false"
  CollectionRules__HighHistogramValues__Actions__0__Settings__Duration: "00:00:30"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Trigger__Type
    value: "EventMeter"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Trigger__Settings_MeterName
    value: "MyCustomMeter"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Trigger__Settings__InstrumentName
    value: "MyCustomHistogram"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Trigger__Settings__HistogramPercentile
    value: "95"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Trigger__Settings__GreaterThan
    value: "175"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Actions__0__Type
    value: "CollectLogs"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Actions__0__Settings__DefaultLevel
    value: "Warning"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Actions__0__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_CollectionRules__HighHistogramValues__Actions__0__Settings__Duration
    value: "00:00:30"
  ```
</details>

### Explanation

This rule, named "HighHistogramValues", will trigger when the custom histogram's values for the 95th percentile exceed the specified threshold (175) throughout the default sliding window duration (1 minute). If the rule is triggered, logs will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the logs to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Exceptions - 4xx Response Status (`AspNetResponseStatus` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "BadResponseStatus": {
      "Trigger": {
        "Type": "AspNetResponseStatus",
        "Settings": {
          "ResponseCount": 5,
          "StatusCodes": [
            "400-499"
          ]
        }
      },
      "Actions": [
        {
          "Type": "CollectExceptions",
          "Settings": {
            "Egress": "artifacts",
            "Format": "NewlineDelimitedJson"
          }
        }
      ],
      "Limits": {
        "ActionCount": 3,
        "ActionCountSlidingWindowDuration": "00:30:00"
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__BadResponseStatus__Trigger__Type: "AspNetResponseStatus"
  CollectionRules__BadResponseStatus__Trigger__Settings__ResponseCount: "5"
  CollectionRules__BadResponseStatus__Trigger__Settings__StatusCodes__0: "400-499"
  CollectionRules__BadResponseStatus__Actions__0__Type: "CollectExceptions"
  CollectionRules__BadResponseStatus__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__BadResponseStatus__Actions__0__Settings__Format: "NewlineDelimitedJson"
  CollectionRules__BadResponseStatus__Limits__ActionCount: "3"
  CollectionRules__BadResponseStatus__Limits__ActionCountSlidingWindowDuration: "00:30:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Type
    value: "AspNetResponseStatus"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Settings__ResponseCount
    value: "5"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Settings__StatusCodes__0
    value: "400-499"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Type
    value: "CollectExceptions"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Settings__Format
    value: "NewlineDelimitedJson"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Limits__ActionCount
    value: "3"
  - name: DotnetMonitor_CollectionRules__BadResponseStatus__Limits__ActionCountSlidingWindowDuration
    value: "00:30:00"
  ```
</details>

### Explanation

This rule, named "BadResponseStatus", will trigger when 5 4xx status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, the recent exceptions from the target process are collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the exceptions to the local filesystem). There is a limit that states that this may only be triggered at most 3 times within a 30 minute sliding window (to prevent an excessive number of exceptions from being collected).

## Collect Logs - High Number of Requests (`AspNetRequestCount` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "HighRequestCount": {
      "Filters": [
        {
          "Key": "ProcessId",
          "Value": "12345",
          "MatchType": "Exact"
        }
      ],
      "Trigger": {
        "Type": "AspNetRequestCount",
        "Settings": {
          "RequestCount": 10,
          "SlidingWindowDuration": "00:01:00"
        }
      },
      "Actions": [
        {
          "Type": "CollectLogs",
          "Settings": {
            "Egress": "artifacts",
            "DefaultLevel": "Error",
            "UseAppFilters": false,
            "Duration": "00:01:00"
          }
        }
      ],
      "Limits": {
        "RuleDuration": "01:00:00"
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__HighRequestCount__Filters__0__Key: "ProcessId"
  CollectionRules__HighRequestCount__Filters__0__Value: "12345"
  CollectionRules__HighRequestCount__Filters__0__MatchType: "Exact"
  CollectionRules__HighRequestCount__Trigger__Type: "AspNetRequestCount"
  CollectionRules__HighRequestCount__Trigger__Settings__RequestCount: "10"
  CollectionRules__HighRequestCount__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__HighRequestCount__Actions__0__Type: "CollectLogs"
  CollectionRules__HighRequestCount__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__HighRequestCount__Actions__0__Settings__DefaultLevel: "Error"
  CollectionRules__HighRequestCount__Actions__0__Settings__UseAppFilters: "false"
  CollectionRules__HighRequestCount__Actions__0__Settings__Duration: "00:01:00"
  CollectionRules__HighRequestCount__Limits__RuleDuration: "01:00:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__Key
    value: "ProcessId"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__Value
    value: "12345"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__MatchType
    value: "Exact"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Type
    value: "CollectLogs"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__DefaultLevel
    value: "Error"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__Duration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Limits__RuleDuration
    value: "01:00:00"
  ```
</details>

### Explanation

This rule, named "HighRequestCount", will trigger when a process with a `ProcessId` of 12345 has 10 requests within a 1 minute sliding window. If the rule is triggered, error level logs will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the logs to the local filesystem). There is a limit that states that this may only be triggered for one hour (to prevent an excessive number of logs from being collected), and there is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Trace - Too Many Long Requests (`AspNetRequestDuration` Trigger)

<details>
  <summary>JSON</summary>

  ```json
  {
    "LongRequestDuration": {
      "Trigger": {
        "Type": "AspNetRequestDuration",
        "Settings": {
          "RequestCount": 5,
          "RequestDuration": "00:00:08",
          "SlidingWindowDuration": "00:02:00",
          "IncludePaths": [ "/api/**/*" ]
        }
      },
      "Actions": [
        {
          "Type": "CollectTrace",
          "Settings": {
            "Profile": "Http",
            "Egress": "artifacts",
            "Duration": "00:01:00"
          }
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__LongRequestDuration__Trigger__Type: "AspNetRequestDuration"
  CollectionRules__LongRequestDuration__Trigger__Settings__RequestCount: "5"
  CollectionRules__LongRequestDuration__Trigger__Settings__RequestDuration: "00:00:08"
  CollectionRules__LongRequestDuration__Trigger__Settings__SlidingWindowDuration: "00:02:00"
  CollectionRules__LongRequestDuration__Trigger__Settings__IncludePaths__0: "/api/**/*"
  CollectionRules__LongRequestDuration__Actions__0__Type: "CollectTrace"
  CollectionRules__LongRequestDuration__Actions__0__Settings__Profile: "Http"
  CollectionRules__LongRequestDuration__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__LongRequestDuration__Actions__0__Settings__Duration: "00:01:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Type
    value: "AspNetRequestDuration"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__RequestCount
    value: "5"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__RequestDuration
    value: "00:00:08"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__SlidingWindowDuration
    value: "00:02:00"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Profile
    value: "Http"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Duration
    value: "00:01:00"
  ```
</details>

### Explanation

This rule, named "LongRequestDuration", will trigger when 5 requests each take greater than 8 seconds to complete within a 2 minute sliding window for all paths under the `/api` route. If the rule is triggered, an Http trace will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Dump And Execute - Collect Dump and Open In Visual Studio

<details>
  <summary>JSON</summary>

  ```json
  {
    "CollectDumpAndExecute": {
      "Trigger": {
        "Type": "AspNetResponseStatus",
        "Settings": {
          "ResponseCount": 3,
          "StatusCodes": [
            "400"
          ]
        }
      },
      "Actions": [
        {
          "Name": "MyDump",
          "Type": "CollectDump",
          "Settings": {
            "Egress": "artifacts",
            "Type": "Mini"
          },
          "WaitForCompletion": true
        },
        {
          "Type": "Execute",
          "Settings": {
            "Path": "C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe",
            "Arguments": "\"$(Actions.MyDump.EgressPath)\""
          }
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__CollectDumpAndExecute__Trigger__Type: "AspNetResponseStatus"
  CollectionRules__CollectDumpAndExecute__Trigger__Settings__ResponseCount: "3"
  CollectionRules__CollectDumpAndExecute__Trigger__Settings__StatusCodes__0: "400"
  CollectionRules__CollectDumpAndExecute__Actions__0__Name: "MyDump"
  CollectionRules__CollectDumpAndExecute__Actions__0__Type: "CollectDump"
  CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Egress: "artifacts"
  CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Type: "Mini"
  CollectionRules__CollectDumpAndExecute__Actions__0__WaitForCompletion: "true"
  CollectionRules__CollectDumpAndExecute__Actions__1__Type: "Execute"
  CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Path: "C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe"
  CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Arguments: "\"$(Actions.MyDump.EgressPath)\""
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Type
    value: "AspNetResponseStatus"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Settings__ResponseCount
    value: "3"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Settings__StatusCodes__0
    value: "400"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Name
    value: "MyDump"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Type
    value: "CollectDump"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Type
    value: "Mini"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__WaitForCompletion
    value: "true"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Type
    value: "Execute"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Path
    value: "C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe"
  - name: DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Arguments
    value: "\"$(Actions.MyDump.EgressPath)\""
  ```
</details>

### Explanation

This rule, named "CollectDumpAndExecute", will trigger when 3 400 status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, a Mini dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the dump to the local filesystem). Upon the dump's completion, Visual Studio will open the egressed dump artifact. To reference a prior result, the general syntax to use is `$(Actions.<ActionName>.<OutputName>)`, where `ActionName` is the name of the previous action whose result is being referenced (in this case, `MyDump`), and `OutputName` is the name of the output being referenced from that action (in this case, `EgressPath`). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

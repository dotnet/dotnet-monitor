# Collection Rule Examples

The following examples provide sample scenarios for using a collection rule. These templates can be copied directly into your configuration file with minimal adjustments to work with your application (for more information on configuring an egress provider, see [egress providers](./../configuration.md#egress-configuration)), or they can be adjusted for your specific use-case. [Learn more about configuring collection rules](collectionrules.md).

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
                "Keywords": "0x8"
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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Trigger__Type="Startup"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Type="CollectTrace"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Name="Microsoft-Windows-DotNETRuntime"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__EventLevel="Informational"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Providers__0__Keywords="0x8"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Duration="00:00:15"
  export DotnetMonitor_CollectionRules__AssemblyLoadTraceOnStartup__Actions__0__Settings__Egress="artifacts"
  ```
</details>

### Explanation

This rule, named "AssemblyLoadTraceOnStartup", will trigger on a process's startup. When the rule is triggered, a trace will be collected for 15 seconds and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). The trace will capture events from an event provider named `Microsoft-Windows-DotNETRuntime`, and will collect events at or above the `Informational` level using the keyword `0x8` (`LoaderKeyword`). For more information on providers, refer to [Well Known Event Providers](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers). The trace will request rundown by default, and the `BufferSizeInMB` has the default value of 256 MB.

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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Type="EventCounter"
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__ProviderName="System.Runtime"
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__CounterName="gc-heap-size"
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Trigger__Settings__GreaterThan="10"
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Actions__0__Type="CollectGCDump"
  export DotnetMonitor_CollectionRules__LargeGCHeapSize__Actions__0__Settings__Egress="artifacts"
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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Type="EventCounter"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__ProviderName="System.Runtime"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__CounterName="cpu-usage"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__GreaterThan="60"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Trigger__Settings__SlidingWindowDuration="00:00:10"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Type="CollectTrace"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Settings__Profile="Cpu"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Actions__0__Settings__Egress="artifacts"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Filters__0__Key="ProcessName"
  export DotnetMonitor_CollectionRules__HighCpuUsage__Filters__0__Value="MyProcessName"
  ```
</details>

### Explanation

This rule, named "HighCpuUsage", will trigger when a process named "MyProcessName" causes CPU usage to exceed 60% for greater than 10 seconds. If the rule is triggered, a Cpu trace will be collected for the default duration (30 seconds), and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Dump - 4xx Response Status (`AspNetResponseStatus` Trigger)

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
          "Type": "CollectDump",
          "Settings": {
            "Egress": "artifacts",
            "Type": "Full"
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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Type="AspNetResponseStatus"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Settings__ResponseCount="5"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Trigger__Settings__StatusCodes__0="400-499"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Type="CollectDump"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Settings__Egress="artifacts"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Actions__0__Settings__Type="Full"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Limits__ActionCount="3"
  export DotnetMonitor_CollectionRules__BadResponseStatus__Limits__ActionCountSlidingWindowDuration="00:30:00"
  ```
  
</details>

### Explanation

This rule, named "BadResponseStatus", will trigger when 5 4xx status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, a Full dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the dump to the local filesystem). There is a limit that states that this may only be triggered at most 3 times within a 30 minute sliding window (to prevent an excessive number of dumps from being collected).

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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__Key="ProcessId"
  export DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__Value="12345"
  export DotnetMonitor_CollectionRules__HighRequestCount__Filters__0__MatchType="Exact"
  export DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Type="AspNetRequestCount"
  export DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__RequestCount="10"
  export DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__SlidingWindowDuration="00:01:00"
  export DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Type="CollectLogs"
  export DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__Egress="artifacts"
  export DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__DefaultLevel="Error"
  export DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__UseAppFilters="false"
  export DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Settings__Duration="00:01:00"
  export DotnetMonitor_CollectionRules__HighRequestCount__Limits__RuleDuration="01:00:00"
  ```
</details>

### Explanation

This rule, named "HighRequestCount", will trigger when a process with a `ProcessId` of 12345 has 10 requests within a 1 minute sliding window. If the rule is triggered, information level logs will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the logs to the local filesystem). There is a limit that states that this may only be triggered for one hour (to prevent an excessive number of logs from being collected), and there is a default `ActionCount` limit stating that this rule may only be triggered 5 times.
    
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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Type="AspNetRequestDuration"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__RequestCount="5"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__RequestDuration="00:00:08"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__SlidingWindowDuration="00:02:00"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Trigger__Settings__IncludePaths__0="/api/**/*"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Type="CollectTrace"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Profile="Http"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Egress="artifacts"
  export DotnetMonitor_CollectionRules__LongRequestDuration__Actions__0__Settings__Duration="00:01:00"
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
  <summary>Environment Variables</summary>
  
  ```bash
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Type="AspNetResponseStatus"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Settings__ResponseCount="3"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Trigger__Settings__StatusCodes__0="400"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Name="MyDump"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Type="CollectDump"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Egress="artifacts"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__Settings__Type="Mini"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__0__WaitForCompletion="true"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Type="Execute"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Path="C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe"
  export DotnetMonitor_CollectionRules__CollectDumpAndExecute__Actions__1__Settings__Arguments="\"$(Actions.MyDump.EgressPath)\""
  ```
  
</details>

### Explanation

This rule, named "CollectDumpAndExecute", will trigger when 3 400 status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, a Mini dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the dump to the local filesystem). Upon the dump's completion, Visual Studio will open the egressed dump artifact. To reference a prior result, the general syntax to use is `$(Actions.<ActionName>.<OutputName>)`, where `ActionName` is the name of the previous action whose result is being referenced (in this case, `MyDump`), and `OutputName` is the name of the output being referenced from that action (in this case, `EgressPath`). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

# Collection Rule Examples

The following examples provide sample scenarios for using a collection rule. These templates can be copied directly into your configuration file with minimal adjustments to work with your application (for more information on configuring an egress provider, see [egress providers](./configuration.md#egress-configuration)), or they can be adjusted for your specific use-case. [Learn more about configuring collection rules](collectionrules.md).

## Collect Trace - Startup (`Startup` Trigger)

### JSON

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

### Explanation

This rule, named "AssemblyLoadTraceOnStartup", will trigger on a process's startup. When the rule is triggered, a trace will be collected for 15 seconds and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). The trace will capture events from an event provider named `Microsoft-Windows-DotNETRuntime`, and will collect events at or above the `Informational` level using the keyword `0x8` (`LoaderKeyword`). For more information on providers, refer to [Well Known Event Providers](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers). The trace will request rundown by default, and the `BufferSizeInMB` has the default value of 256 MB.

## Collect GCDump - Heap Size (`EventCounter` Trigger)

### JSON

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

### Explanation

This rule, named "LargeGCHeapSize", will trigger when the GC Heap Size exceeds 10 MB within the default sliding window duration (1 minute). If the rule is triggered, a GCDump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the GCDump to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Trace - High CPU Usage (`EventCounter` Trigger)

### JSON

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

### Explanation

This rule, named "HighCpuUsage", will trigger when a process named "MyProcessName" causes CPU usage to exceed 60% for greater than 10 seconds. If the rule is triggered, a Cpu trace will be collected for the default duration (30 seconds), and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Dump - 4xx Response Status (`AspNetResponseStatus` Trigger)

### JSON

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

### Explanation

This rule, named "BadResponseStatus", will trigger when 5 4xx status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, a Full dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the dump to the local filesystem). There is a limit that states that this may only be triggered at most 3 times within a 30 minute sliding window (to prevent an excessive number of dumps from being collected).

## Collect Logs - High Number of Requests (`AspNetRequestCount` Trigger)

### JSON

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

### Explanation

This rule, named "HighRequestCount", will trigger when a process with a `ProcessId` of 12345 has 10 requests within a 1 minute sliding window. If the rule is triggered, information level logs will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the logs to the local filesystem). There is a limit that states that this may only be triggered for one hour (to prevent an excessive number of logs from being collected), and there is a default `ActionCount` limit stating that this rule may only be triggered 5 times.
    
## Collect Trace - Too Many Long Requests (`AspNetRequestDuration` Trigger)

### JSON

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

### Explanation

This rule, named "LongRequestDuration", will trigger when 5 requests each take greater than 8 seconds to complete within a 2 minute sliding window for all paths under the `/api` route. If the rule is triggered, an Http trace will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

## Collect Dump And Execute - Collect Dump and Open In Visual Studio

### JSON

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

### Explanation

This rule, named "CollectDumpAndExecute", will trigger when 3 400 status codes are encountered within the default sliding window duration (1 minute). If the rule is triggered, a Mini dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the dump to the local filesystem). Upon the dump's completion, Visual Studio will open the egressed dump artifact. To reference a prior result, the general syntax to use is `$(Actions.<ActionName>.<OutputName>)`, where `ActionName` is the name of the previous action whose result is being referenced (in this case, `MyDump`), and `OutputName` is the name of the output being referenced from that action (in this case, `EgressPath`). There is a default `ActionCount` limit stating that this rule may only be triggered 5 times.

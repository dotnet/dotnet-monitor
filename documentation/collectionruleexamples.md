# Collection Rule Examples

The following examples provide sample scenarios for using a collection rule. These templates can be copied directly into your configuration file and do not require any other set-up (with the exception of updating the `path` for the `Execute` action); however, they may also be used as a starting point upon which adjustments can be made for specific use-cases. Learn more about configuring [collection rules](collectionrules.md).

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

This rule, named "HighCpuUsage", will trigger when a process named "MyProcessName" causes CPU usage to exceed 60% for greater than 10 seconds. If the rule is triggered, a Cpu trace will be collected for the default duration (30 seconds), and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem).

## Collect Dump - 4xx Response Status (`AspNetResponseStatus` Trigger)

### JSON

```json
{
  "BadResponseStatus": {
    "Trigger": {
      "Type": "AspNetResponseStatus",
      "Settings": {
        "ResponseCount": 1,
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
    {
      "Limits": {
        "ActionCount": 10,
        "ActionCountSlidingWindowDuration": "00:30:00"
      }
    }
  }
}
```

### Explanation

This rule, named "BadResponseStatus", will trigger when a 4xx status code is received. If the rule is triggered, a Full dump will be collected for the default duration (30 seconds), and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a limit that states that this may only be triggered at most 10 times within a 30 minute sliding window (to prevent an excessive number of dumps from being collected).

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
          "DefaultLevel": "Information",
          "UseAppFilters": false,
          "Duration": "00:01:00"
        }
      }
    ],
    "Limits": {
      "RuleDuration": "00:01:00"
    }
  }
}
```

### Explanation

This rule, named "HighRequestCount", will trigger when a process with a `ProcessId` 12345 has 10 requests within a 1 minute sliding window. If the rule is triggered, information level logs will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). There is a limit that states that this may only be triggered for one hour (to prevent an excessive number of logs from being collected).
    
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
        "SlidingWindowDuration": "00:02:00"
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

This rule, named "LongRequestDuration", will trigger when a process has 5 requests that take greater than 8 seconds within a 2 minute sliding window. If the rule is triggered, an Http trace will be collected for one minute and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem).

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
          "Type": "Triage"
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

This rule, named "CollectDumpAndExecute", will trigger when a process has 3 requests with a 400 response within the default sliding window duration (1 minute). If the rule is triggered, a Triage dump will be collected and egressed to the specified `Egress` provider (in this case, `artifacts` has been configured to save the trace to the local filesystem). Upon the dump's completion, Visual Studio will open the egressed dump artifact.

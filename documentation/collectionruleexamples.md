# Collection Rule Examples

## Overview

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

## Collect Trace - Too Many Long Requests (`AspNetRequestDuration` Trigger)

## Execute - Collect Dump and Open In Visual Studio

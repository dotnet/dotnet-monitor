# Custom Shortcuts

Custom Shortcuts allow users to design reusable collection rule components to decrease configuration verbosity, improve standardization between rules, and speed up the process of writing complex scenarios.

Custom Shortcuts associate a name with a single Filter, Trigger, Action, or Limit; this name can then be used throughout configuration to represent the use of that Filter/Trigger/Action/Limit. This is ideal for scenarios where multiple collection rules re-use the same functionality, allowing the author to write/edit the configuration in a single place.

## Example

The following example creates a custom shortcut trigger named "HighRequestCount", two custom shortcut actions named "CpuTrace" and "ErrorLogs", a custom shortcut filter named "AppPID", and a custom shortcut limit named "ShortDuration". These custom shortcuts are integrated into collection rules alongside the existing configuration format.

<details>
  <summary>JSON</summary>

  ```json
  {
    "CustomShortcuts": {
      "Actions": {
        "CPUTrace": {
          "Type": "CollectTrace",
          "Settings": {
            "Egress": "artifacts",
            "SlidingWindowDuration": "00:00:15",
            "Profile": "Cpu"
          }
        },
        "ErrorLogs": {
          "Type": "CollectLogs",
          "Settings": {
            "Egress": "artifacts",
            "DefaultLevel": "Error",
            "UseAppFilters": false,
            "Duration": "00:01:00"
          }
        }
      },
      "Triggers": {
        "HighRequestCount": {
          "Type": "AspNetRequestCount",
          "Settings": {
            "RequestCount": 10,
            "SlidingWindowDuration": "00:01:00"
          }
        }
      },
      "Filters": {
        "AppPID": {
          "Key": "ProcessId",
          "Value": "12345",
          "MatchType": "Exact"
        }
      },
      "Limits": {
        "ShortDuration": {
          "RuleDuration": "00:05:00",
          "ActionCount": "1",
          "ActionCountSlidingWindowDuration": "00:00:30"
        }
      }
    },
    ...
    "CollectionRules": {
      "LogAndDumpWhenHighRequestCount": {
        "Trigger": "HighRequestCount",
        "Actions": [
          "ErrorLogs",
          {
            "Type": "CollectDump",
            "Settings": {
              "Egress": "artifacts",
              "Type": "Full"
            }
          }
        ],
        "Filters": [
          "AppPID"
        ],
        "Limits": "ShortDuration"  
      }
    },
    "TraceWhenHighCPU": {
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
        "CPUTrace"
      ],
      "Filters": [
        {
          "ProcessName": "MyProcess"
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>ConfigMap</summary>
  
  ```yaml

  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml

  ```
</details>

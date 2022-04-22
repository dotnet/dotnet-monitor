# Custom Shortcuts

Custom Shortcuts allow users to design reusable collection rule components to decrease configuration verbosity, reduce duplication between rules, and speed up the process of writing complex scenarios.

Custom Shortcuts associate a name with a single Filter, Trigger, Action, or Limit; this name can then be used throughout configuration to represent the use of that Filter/Trigger/Action/Limit. This is ideal for scenarios where multiple collection rules re-use the same functionality, allowing the author to write/edit the configuration in a single place.

You can easily translate existing configuration to Custom Shortcuts using the format in the following sample; once defined, the Custom Shortcut is referenced by its name in the collection rule.

<details>
  <summary>JSON</summary>

  ```json
  {
    "CustomShortcuts": {
      "Actions": {
        "NameOfActionShortcut": {
          "Type": "CollectTrace",
          "Settings": {
            "Egress": "artifacts",
            "SlidingWindowDuration": "00:00:15",
            "Profile": "Cpu"
          }
        }
      },
      "Triggers": {
        "NameOfTriggerShortcut": {
          "Type": "AspNetRequestCount",
          "Settings": {
            "RequestCount": 10,
            "SlidingWindowDuration": "00:01:00"
          }
        }
      }
    },
    ...
    "CollectionRules": {
      "NameOfCollectionRule": {
        "Trigger": "NameOfTriggerShortcut",
        "Actions": [
          "NameOfActionShortcut"
        ] 
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Type: "CollectTrace"
  DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__Egress: "artifacts"
  DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__SlidingWindowDuration: "00:00:15"
  DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__Profile: "Cpu"
  DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Type: "AspNetRequestCount"
  DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Settings__RequestCount: "10"
  DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Settings__SlidingWindowDuration: "00:01:00"

  DotnetMonitor_CollectionRules__NameOfCollectionRule__Trigger: "NameOfTriggerShortcut"
  DotnetMonitor_CollectionRules__NameOfCollectionRule__Actions__0: "NameOfActionShortcut"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__SlidingWindowDuration
    value: "00:00:15"
  - name: DotnetMonitor_CustomShortcuts__Actions__NameOfActionShortcut__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CustomShortcuts__Triggers__NameOfTriggerShortcut__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__NameOfCollectionRule__Trigger
    value: "NameOfTriggerShortcut"
  - name: DotnetMonitor_CollectionRules__NameOfCollectionRule__Actions__0
    value: "NameOfActionShortcut"

  ```
</details>

## Example

The following example creates a custom shortcut trigger named "HighRequestCount", two custom shortcut actions named "CpuTrace" and "ErrorLogs", a custom shortcut filter named "AppPID", and a custom shortcut limit named "ShortDuration". These custom shortcuts are integrated into collection rules alongside the existing configuration format to demonstrate that rules can contain a mix of custom shortcuts and standard configuration.

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
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Type: "CollectTrace"
  DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__Egress: "artifacts"
  DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__SlidingWindowDuration: "00:00:15"
  DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__Profile: "Cpu"
  DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Type: "CollectLogs"
  DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__Egress: "artifacts"
  DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__DefaultLevel: "Error"
  DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__UseAppFilters: "false"
  DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__Duration: "00:01:00"
  DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Type: "AspNetRequestCount"
  DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Settings__RequestCount: "10"
  DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Settings__SlidingWindowDuration: "00:01:00"
  DotnetMonitor_CustomShortcuts__Filters__AppPID__Key: "ProcessId"
  DotnetMonitor_CustomShortcuts__Filters__AppPID__Value: "12345"
  DotnetMonitor_CustomShortcuts__Filters__AppPID__MatchType: "Exact"
  DotnetMonitor_CustomShortcuts__Limits__ShortDuration__RuleDuration: "00:05:00"
  DotnetMonitor_CustomShortcuts__Limits__ShortDuration__ActionCount: "1"
  DotnetMonitor_CustomShortcuts__Limits__ShortDuration__ActionCountSlidingWindowDuration: "00:00:30"
  
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Trigger: "HighRequestCount"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__0: "ErrorLogs"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Type: "CollectDump"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Egress: "artifacts"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Type: "Full"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Filters__0: "AppPID"
  DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Limits: "ShortDuration"

  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Type: "EventCounter"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__ProviderName: "System.Runtime"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__CounterName: "cpu-usage"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__GreaterThan: "60"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Actions__0: "CPUTrace"
  DotnetMonitor_CollectionRules__TraceWhenHighCPU__Filters__0__ProcessName: "MyProcess"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__SlidingWindowDuration
    value: "00:00:15"
  - name: DotnetMonitor_CustomShortcuts__Actions__CPUTrace__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Type
    value: "CollectLogs"
  - name: DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__DefaultLevel
    value: "Error"
  - name: DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_CustomShortcuts__Actions__ErrorLogs__Settings__Duration
    value: "00:01:00"
  - name: DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CustomShortcuts__Triggers__HighRequestCount__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CustomShortcuts__Filters__AppPID__Key
    value: "ProcessId"
  - name: DotnetMonitor_CustomShortcuts__Filters__AppPID__Value
    value: "12345"
  - name: DotnetMonitor_CustomShortcuts__Filters__AppPID__MatchType
    value: "Exact"
  - name: DotnetMonitor_CustomShortcuts__Limits__ShortDuration__RuleDuration
    value: "00:05:00"
  - name: DotnetMonitor_CustomShortcuts__Limits__ShortDuration__ActionCount
    value: "1"
  - name: DotnetMonitor_CustomShortcuts__Limits__ShortDuration__ActionCountSlidingWindowDuration
    value: "00:00:30"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Trigger
    value: "HighRequestCount"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__0
    value: "ErrorLogs"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Type
    value: "CollectDump"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Type
    value: "Full"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Filters__0
    value: "AppPID"
  - name: DotnetMonitor_CollectionRules__LogAndDumpWhenHighRequestCount__Limits
    value: "ShortDuration"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__GreaterThan
    value: "60"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Actions__0
    value: "CPUTrace"
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Filters__0__ProcessName
    value: "MyProcess"
  ```
</details>

# Templates

Templates allow users to design reusable collection rule components to decrease configuration verbosity, reduce duplication between rules, and speed up the process of writing complex scenarios.

Templates associate a name with a single Filter, Trigger, Action, or Limit; this name can then be used throughout configuration to represent the use of that Filter/Trigger/Action/Limit. This is ideal for scenarios where multiple collection rules re-use the same functionality, allowing the author to write/edit the configuration in a single place.

You can easily translate existing configuration to Templates using the format in the following sample; once defined, the Template is referenced by its name in the collection rule.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Templates": {
      "CollectionRuleActions": {
        "NameOfActionTemplate": {
          "Type": "CollectTrace",
          "Settings": {
            "Egress": "artifacts",
            "SlidingWindowDuration": "00:00:15",
            "Profile": "Cpu"
          }
        }
      },
      "CollectionRuleTriggers": {
        "NameOfTriggerTemplate": {
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
        "Trigger": "NameOfTriggerTemplate",
        "Actions": [
          "NameOfActionTemplate"
        ]
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Templates__CollectionRuleActions__NameOfActionTemplate__Type: "CollectTrace"
  Templates__CollectionRuleActions__NameOfActionTemplate__Settings__Egress: "artifacts"
  Templates__CollectionRuleActions__NameOfActionTemplate__Settings__SlidingWindowDuration: "00:00:15"
  Templates__CollectionRuleActions__NameOfActionTemplate__Settings__Profile: "Cpu"
  Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Type: "AspNetRequestCount"
  Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Settings__RequestCount: "10"
  Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__NameOfCollectionRule__Trigger: "NameOfTriggerTemplate"
  CollectionRules__NameOfCollectionRule__Actions__0: "NameOfActionTemplate"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Templates__CollectionRuleActions__NameOfActionTemplate__Type
    value: "CollectTrace"
  - name: DotnetMonitor_Templates__CollectionRuleActions__NameOfActionTemplate__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_Templates__CollectionRuleActions__NameOfActionTemplate__Settings__SlidingWindowDuration
    value: "00:00:15"
  - name: DotnetMonitor_Templates__CollectionRuleActions__NameOfActionTemplate__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__NameOfTriggerTemplate__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__NameOfCollectionRule__Trigger
    value: "NameOfTriggerTemplate"
  - name: DotnetMonitor_CollectionRules__NameOfCollectionRule__Actions__0
    value: "NameOfActionTemplate"
  ```
</details>

## Example

The following example creates a template trigger named "HighRequestCount", two template actions named "CpuTrace" and "ErrorLogs", a template filter named "AppName", and a template limit named "ShortDuration". These templates are integrated into collection rules alongside the existing configuration format to demonstrate that rules can contain a mix of templates and standard configuration.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Templates": {
      "CollectionRuleActions": {
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
      "CollectionRuleTriggers": {
        "HighRequestCount": {
          "Type": "AspNetRequestCount",
          "Settings": {
            "RequestCount": 10,
            "SlidingWindowDuration": "00:01:00"
          }
        }
      },
      "CollectionRuleFilters": {
        "AppName": {
          "Key": "ProcessName",
          "Value": "MyProcessName",
          "MatchType": "Exact"
        }
      },
      "CollectionRuleLimits": {
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
          "AppName"
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
          "ProcessId": "12345"
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Templates__CollectionRuleActions__CPUTrace__Type: "CollectTrace"
  Templates__CollectionRuleActions__CPUTrace__Settings__Egress: "artifacts"
  Templates__CollectionRuleActions__CPUTrace__Settings__SlidingWindowDuration: "00:00:15"
  Templates__CollectionRuleActions__CPUTrace__Settings__Profile: "Cpu"
  Templates__CollectionRuleActions__ErrorLogs__Type: "CollectLogs"
  Templates__CollectionRuleActions__ErrorLogs__Settings__Egress: "artifacts"
  Templates__CollectionRuleActions__ErrorLogs__Settings__DefaultLevel: "Error"
  Templates__CollectionRuleActions__ErrorLogs__Settings__UseAppFilters: "false"
  Templates__CollectionRuleActions__ErrorLogs__Settings__Duration: "00:01:00"
  Templates__CollectionRuleTriggers__HighRequestCount__Type: "AspNetRequestCount"
  Templates__CollectionRuleTriggers__HighRequestCount__Settings__RequestCount: "10"
  Templates__CollectionRuleTriggers__HighRequestCount__Settings__SlidingWindowDuration: "00:01:00"
  Templates__CollectionRuleFilters__AppName__Key: "ProcessName"
  Templates__CollectionRuleFilters__AppName__Value: "MyProcessName"
  Templates__CollectionRuleFilters__AppName__MatchType: "Exact"
  Templates__CollectionRuleLimits__ShortDuration__RuleDuration: "00:05:00"
  Templates__CollectionRuleLimits__ShortDuration__ActionCount: "1"
  Templates__CollectionRuleLimits__ShortDuration__ActionCountSlidingWindowDuration: "00:00:30"

  CollectionRules__LogAndDumpWhenHighRequestCount__Trigger: "HighRequestCount"
  CollectionRules__LogAndDumpWhenHighRequestCount__Actions__0: "ErrorLogs"
  CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Type: "CollectDump"
  CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Egress: "artifacts"
  CollectionRules__LogAndDumpWhenHighRequestCount__Actions__1__Settings__Type: "Full"
  CollectionRules__LogAndDumpWhenHighRequestCount__Filters__0: "AppName"
  CollectionRules__LogAndDumpWhenHighRequestCount__Limits: "ShortDuration"
  CollectionRules__TraceWhenHighCPU__Trigger__Type: "EventCounter"
  CollectionRules__TraceWhenHighCPU__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__TraceWhenHighCPU__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__TraceWhenHighCPU__Trigger__Settings__GreaterThan: "60"
  CollectionRules__TraceWhenHighCPU__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  CollectionRules__TraceWhenHighCPU__Actions__0: "CPUTrace"
  CollectionRules__TraceWhenHighCPU__Filters__0__ProcessId: "12345"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Templates__CollectionRuleActions__CPUTrace__Type
    value: "CollectTrace"
  - name: DotnetMonitor_Templates__CollectionRuleActions__CPUTrace__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_Templates__CollectionRuleActions__CPUTrace__Settings__SlidingWindowDuration
    value: "00:00:15"
  - name: DotnetMonitor_Templates__CollectionRuleActions__CPUTrace__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_Templates__CollectionRuleActions__ErrorLogs__Type
    value: "CollectLogs"
  - name: DotnetMonitor_Templates__CollectionRuleActions__ErrorLogs__Settings__Egress
    value: "artifacts"
  - name: DotnetMonitor_Templates__CollectionRuleActions__ErrorLogs__Settings__DefaultLevel
    value: "Error"
  - name: DotnetMonitor_Templates__CollectionRuleActions__ErrorLogs__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_Templates__CollectionRuleActions__ErrorLogs__Settings__Duration
    value: "00:01:00"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__HighRequestCount__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__HighRequestCount__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_Templates__CollectionRuleTriggers__HighRequestCount__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_Templates__CollectionRuleFilters__AppName__Key
    value: "ProcessName"
  - name: DotnetMonitor_Templates__CollectionRuleFilters__AppName__Value
    value: "MyProcessName"
  - name: DotnetMonitor_Templates__CollectionRuleFilters__AppName__MatchType
    value: "Exact"
  - name: DotnetMonitor_Templates__CollectionRuleLimits__ShortDuration__RuleDuration
    value: "00:05:00"
  - name: DotnetMonitor_Templates__CollectionRuleLimits__ShortDuration__ActionCount
    value: "1"
  - name: DotnetMonitor_Templates__CollectionRuleLimits__ShortDuration__ActionCountSlidingWindowDuration
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
    value: "AppName"
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
  - name: DotnetMonitor_CollectionRules__TraceWhenHighCPU__Filters__0__ProcessId
    value: "12345"
  ```
</details>

# Trigger Shortcuts

These triggers simplify configuration for several common trigger use-cases. All of these shortcuts can be expressed as `EventCounter` triggers; however, these shortcuts provide improved defaults, range validation, and a simpler syntax. There are currently three built-in default triggers; additional trigger shortcuts may be added in future versions of `dotnet monitor`.

### `CPUUsage` Trigger Shortcut

Execute a trigger when the target application's CPU Usage is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level as a percentage that the counter must maintain (or higher) for the specified duration. | `50` | `0` | `100` |
| `LessThan` | double? | false | The threshold level as a percentage that the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | `100` |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "EventCounter",
      "Settings": {
        "ProviderName": "System.Runtime",
        "CounterName": "cpu-usage",
        "GreaterThan": 50
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "EventCounter"
  CollectionRules__RuleName__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "50"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "50"
  ```
</details>

#### Example

Usage that is satisfied when the CPU usage of the application is higher than 50% for a 1 minute window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "CPUUsage"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "CPUUsage"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "CPUUsage"
  ```
</details>

### `GCHeapSize` Trigger Shortcut

Execute a trigger when the target application's GC Heap Size is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level in MBs that the counter must maintain (or higher) for the specified duration. | `10` | `0` | |
| `LessThan` | double? | false | The threshold level in MBs that the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | `100` |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "EventCounter",
      "Settings": {
        "ProviderName": "System.Runtime",
        "CounterName": "gc-heap-size",
        "GreaterThan": "10"
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "EventCounter"
  CollectionRules__RuleName__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Trigger__Settings__CounterName: "gc-heap-size"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__CounterName
    value: "gc-heap-size"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "10"
  ```
</details>

#### Example

Usage that is satisfied when the GC Heap Size of the application is greater than 10MB for a 1 minute window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "GCHeapSize"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "GCHeapSize"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "GCHeapSize"
  ```
</details>

### `ThreadpoolQueueLength` Trigger Shortcut

Execute a trigger when the target application's threadpool queue length is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. | `200` | `0` | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "EventCounter",
      "Settings": {
        "ProviderName": "System.Runtime",
        "CounterName": "threadpool-queue-length",
        "GreaterThan": "200"
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "EventCounter"
  CollectionRules__RuleName__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Trigger__Settings__CounterName: "threadpool-queue-length"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "200"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__CounterName
    value: "threadpool-queue-length"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "200"
  ```
</details>

#### Example

Usage that is satisfied when the threadpool queue length of the application is higher than 200 for a 1 minute window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Trigger": {
      "Type": "ThreadpoolQueueLength"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Trigger__Type: "ThreadpoolQueueLength"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Type
    value: "ThreadpoolQueueLength"
  ```
</details>

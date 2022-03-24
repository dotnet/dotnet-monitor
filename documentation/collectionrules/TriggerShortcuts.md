# Trigger Shortcuts

These triggers simplify configuration for several common trigger use-cases. There are currently three built-in default triggers; additional trigger shortcuts may be added in future versions of `dotnet monitor`.

### `CpuUsage` Trigger Shortcut

Execute a trigger when the target application's CPU Usage is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. | `50` | `0` | `100` |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | `100` |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

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

#### Example

Usage that is satisfied when the CPU usage of the application is higher than 50% for a 1 minute window.

```json
{
  "Trigger": {
    "Type": "CpuUsage"
  }
}
```

### `GCHeapSize` Trigger Shortcut

Execute a trigger when the target application's GC Heap Size is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. | `TBD` | `0` | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | `100` |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

```json
{
  "Trigger": {
    "Type": "EventCounter",
    "Settings": {
      "ProviderName": "System.Runtime",
      "CounterName": "gc-heap-size",
      "GreaterThan": TBD
    }
  }
}
```

#### Example

Usage that is satisfied when the GC Heap Size of the application is greater than TBD for a 1 minute window.

```json
{
  "Trigger": {
    "Type": "GCHeapSize"
  }
}
```

### `ThreadpoolQueueLength` Trigger Shortcut

Execute a trigger when the target application's threadpool queue length is continuously greater than or less than a specified value for a set duration of time.

#### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. | `TBD` | `0` | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. If `LessThan` is specified, the default value of `GreaterThan` becomes `null`. | `null` | `0` | |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

#### Equivalent EventCounter Trigger

```json
{
  "Trigger": {
    "Type": "EventCounter",
    "Settings": {
      "ProviderName": "System.Runtime",
      "CounterName": "threadpool-queue-length",
      "GreaterThan": TBD
    }
  }
}
```

#### Example

Usage that is satisfied when the threadpool queue length of the application is higher than TBD for a 1 minute window.

```json
{
  "Trigger": {
    "Type": "ThreadpoolQueueLength"
  }
}
```

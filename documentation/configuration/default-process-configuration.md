# Default Process Configuration

Default process configuration is used to determine which process is used for metrics and in situations where the process is not specified in the query to retrieve an artifact. A process must match all the specified filters. If a `Key` is not specified, the default is `ProcessId`.

| Name | Type | Description |
|---|---|---|
| Key | string | Specifies which criteria to match on the process. Can be `ProcessId`, `ProcessName`, `CommandLine`. |
| Value | string | The value to match against the process. |
| MatchType | string | The type of match to perform. Can be `Exact` or `Contains` for sub-string matching. Both are case-insensitive.|


Optionally, a shorthand format allows you to omit the `Key` and `Value` terms and specify your Key/Value pair as a single line.

| Name | Type | Description |
|---|---|---|
| ProcessId | string | Specifies that the corresponding value is the expected `ProcessId`. |
| ProcessName | string | Specifies that the corresponding value is the expected `ProcessName`. |
| CommandLine | string | Specifies that the corresponding value is the expected `CommandLine`.|

## Examples

### Match the IISExpress process by name

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultProcess": {
      "Filters": [{
        "Key": "ProcessName",
        "Value": "iisexpress"
      }]
    },
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  DefaultProcess__Filters__0__Key: "ProcessName"
  DefaultProcess__Filters__0__Value: "iisexpress"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_DefaultProcess__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_DefaultProcess__Filters__0__Value
    value: "iisexpress"
  ```
</details>

### Match the IISExpress process by name (Shorthand)

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultProcess": {
      "Filters": [{
        "ProcessName": "iisexpress"
      }]
    },
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  DefaultProcess__Filters__0__ProcessName: "iisexpress"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_DefaultProcess__Filters__0__ProcessName
    value: "iisexpress"
  ```
</details>

### Match pid 1

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultProcess": {
      "Filters": [{
        "Key": "ProcessId",
        "Value": "1"
      }]
    },
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  DefaultProcess__Filters__0__Key: "ProcessId"
  DefaultProcess__Filters__0__Value: "1"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_DefaultProcess__Filters__0__Key
    value: "ProcessId"
  - name: DotnetMonitor_DefaultProcess__Filters__0__Value
    value: "1"
  ```
</details>

### Match pid 1 (Shorthand)

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultProcess": {
      "Filters": [{
        "ProcessId": "1"
      }]
    },
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  DefaultProcess__Filters__0__ProcessId: "1"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_DefaultProcess__Filters__0__ProcessId
    value: "1"
  ```
</details>

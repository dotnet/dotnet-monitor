# In-Process Features Configuration

First Available: 8.0 Preview 7

> [!NOTE]
> In-process features are only supported when running dotnet-monitor in `Listen` mode.
> See [Diagnostic Port](./diagnostic-port-configuration.md) configuration for details.

Some features of `dotnet monitor` require loading libraries into target applications. These libraries ship with `dotnet monitor` and are provisioned to be available to target applications using the `DefaultSharedPath` option in the [storage configuration](./storage-configuration.md) section. The following features require these in-process libraries to be used:

- [Call Stacks](#call-stacks)
- [Exceptions History](#exceptions-history)

Because these libraries are loaded into the target application (they are not loaded into `dotnet monitor`), they may have performance impact on memory and CPU utilization in the target application. These features are off by default and may be enabled via the `InProcessFeatures` configuration section.

### Example

To enable all available in-process features, use the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "Enabled": true
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__Enabled: "true"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__Enabled
    value: "true"
  ```
</details>

## Call Stacks

The call stacks feature is individually enabled by setting the `Enabled` property of the `CallStacks` section to `true`:

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "CallStacks": {
        "Enabled": true
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__CallStacks__Enabled: "true"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__CallStacks__Enabled
    value: "true"
  ```
</details>

Similarly, the call stacks feature can be individually disabled by setting the same property to `false`:

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "Enabled": true,
      "CallStacks": {
        "Enabled": false
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__Enabled: "true"
  InProcessFeatures__CallStacks__Enabled: "false"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__Enabled
    value: "true"
  - name: DotnetMonitor_InProcessFeatures__CallStacks__Enabled
    value: "false"
  ```
</details>

## Exceptions History

The exceptions history feature is individually enabled by setting the `Enabled` property of the `Exceptions` section to `true`:

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "Exceptions": {
        "Enabled": true
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__Exceptions__Enabled: "true"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__Exceptions__Enabled
    value: "true"
  ```
</details>

Similarly, the exceptions history feature can be individually disabled by setting the same property to `false`:

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "Enabled": true,
      "Exceptions": {
        "Enabled": false
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__Enabled: "true"
  InProcessFeatures__Exceptions__Enabled: "false"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__Enabled
    value: "true"
  - name: DotnetMonitor_InProcessFeatures__Exceptions__Enabled
    value: "false"
  ```
</details>

### Filtering

Which exceptions are collected and stored can be filtered via configuration using [ExceptionsConfiguration](../api/definitions.md#ExceptionsConfiguration). This can be useful for noisy exceptions that are not useful to capture - an example of this may be disregarding any `TaskCanceledException` that the target application produces.

Note that this is different than [real-time filtering](../api/exceptions-custom.md), which does **not** restrict the collection of exceptions and is solely responsible for determining which exceptions are displayed when using the `/exceptions` route.

In this example, a user is choosing to only collect exceptions where the top frame's class is `MyClassName`, and exceptions of types `TaskCanceledException` or `OperationCanceledException` will not be collected.

<details>
  <summary>JSON</summary>

  ```json
  {
    "InProcessFeatures": {
      "Exceptions": {
        "Enabled": true,
        "CollectionFilters": {
          "Include": [
            {
              "TypeName": "MyClassName"
            }
          ],
          "Exclude": [
            {
              "ExceptionType": "TaskCanceledException"
            },
            {
              "ExceptionType": "OperationCanceledException"
            }
          ]
        }
      }
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  InProcessFeatures__Exceptions__Enabled: "true"
  InProcessFeatures__Exceptions__CollectionFilters__Include__0__TypeName: "MyClassName"
  InProcessFeatures__Exceptions__CollectionFilters__Exclude__0__ExceptionType: "TaskCanceledException"
  InProcessFeatures__Exceptions__CollectionFilters__Exclude__1__ExceptionType: "OperationCanceledException"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_InProcessFeatures__Exceptions__Enabled
    value: "true"
  - name: DotnetMonitor_InProcessFeatures__Exceptions__CollectionFilters__Include__0__TypeName
    value: "MyClassName"
  - name: DotnetMonitor_InProcessFeatures__Exceptions__CollectionFilters__Exclude__0__ExceptionType
    value: "TaskCanceledException"
  - name: DotnetMonitor_InProcessFeatures__Exceptions__CollectionFilters__Exclude__1__ExceptionType
    value: "OperationCanceledException"
  ```
</details>

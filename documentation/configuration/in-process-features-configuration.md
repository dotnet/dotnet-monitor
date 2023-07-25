### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fconfiguration%2Fin-process-features-configuration)

# In-Process Features Configuration

First Available: 8.0 Preview 7

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

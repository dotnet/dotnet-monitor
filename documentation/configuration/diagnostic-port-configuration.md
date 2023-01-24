# Diagnostic Port Configuration

`dotnet monitor` communicates via .NET processes through their diagnostic port. In the default configuration, .NET processes listen on a platform native transport (named pipes on Windows/Unix-domain sockets on \*nix) in a well-known location.

## Connection Mode

It is possible to change this behavior and have .NET processes connect to `dotnet monitor`. This allow you to monitor a process from start and collect traces for events such as assembly load events that primarily occur at process startup and weren't possible to collect previously.

<details>
  <summary>JSON</summary>

  ```json
  {
    "DiagnosticPort": "/diag/port.sock"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  DiagnosticPort: "/diag/port.sock"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_DiagnosticPort
    value: "/diag/port.sock"
  ```
</details>

Alternatively, `dotnet monitor` can be set to `Listen` mode using the expanded format. In the event of conflicting configuration, the simplified format will take priority over the expanded format.

<details>
  <summary>JSON</summary>

  ```json
  {
    "DiagnosticPort": {
      "ConnectionMode": "Listen",
      "EndpointName": "/diag/port.sock"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  DiagnosticPort__ConnectionMode: "Listen"
  DiagnosticPort__EndpointName: "/diag/port.sock"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_DiagnosticPort__ConnectionMode
    value: "Listen"
  - name: DotnetMonitor_DiagnosticPort__EndpointName
    value: "/diag/port.sock"
  ```
</details>

When `dotnet monitor` is in `Listen` mode, you have to configure .NET processes to connect to `dotnet monitor`. You can do so by specifying the appropriate environment variable on your .NET process.

```bash
export DOTNET_DiagnosticPorts="/diag/port.sock,suspend"
```


### Maximum connection

When operating in `Listen` mode, you can also specify the maximum number of incoming connections for `dotnet monitor` to accept via the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "DiagnosticPort": {
      "MaxConnections": "10"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  DiagnosticPort__MaxConnections: "10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_DiagnosticPort__MaxConnections
    value: "10"
  ```
</details>
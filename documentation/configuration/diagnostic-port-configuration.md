
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fconfiguration%2Fdiagnostic-port-configuration)

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
      "MaxConnections": 10
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


## Supported Scenarios


### Default Configuration

As noted the default configuration for `dotnet monitor` is `Connect` mode. This assumes your .NET apps do not have any additional configuration and as a result are communicating via the diagnostics port which is in the default location.

The following table shows the supported scenarios for the default configuration.

| API | Supported |
| :-------- | :-------: |
| /process | Yes |
| /dump | Yes |
| /gcdump | Yes |
| /trace | Yes |
| /metrics | Yes |
| /livemetrics | Yes |
| /logs | Yes |
| /info | Yes |
| /operations | Yes |
| /collectionrules | No |
| /stacks | No |
| /exceptions | No |
| /parameters | No |

| Trigger | Supported |
| :-------- | :-------: |
| Trigger - Startup | No |
| Trigger - EventCounter | No |
| Trigger - EventMeter | No |
| Trigger - AspNetResponseStatus | No |
| Trigger - AspNetRequestCount | No |
| Trigger - AspNetRequestDuration | No |


### `dotnet monitor` in Listen mode

When `dotnet monitor` is in `Listen` mode, you must explicitly configure your .NET processes to connect to `dotnet monitor`, the options for the .NET process are either `Connect (susp)` or `Connect (nosusp)`.

#### App in Connect (susp) mode

The `susp` option indicates that the .NET process will suspend its own startup execution until it successfully communicates with `dotnet monitor`.

| API | Supported |
| :-------- | :-------: |
| /process | Yes |
| /dump | Yes |
| /gcdump | Yes |
| /trace | Yes |
| /metrics | Yes |
| /livemetrics | Yes |
| /logs | Yes |
| /info | Yes |
| /operations | Yes |
| /collectionrules | Yes |
| /stacks | Yes |
| /exceptions | Yes |
| /parameters | Yes |

| Trigger | Supported |
| :-------- | :-------: |
| Trigger - Startup | Yes |
| Trigger - EventCounter | Yes |
| Trigger - EventMeter | Yes |
| Trigger - AspNetResponseStatus | Yes |
| Trigger - AspNetRequestCount | Yes |
| Trigger - AspNetRequestDuration | Yes |


#### App in Connect (nosusp)

The `nosusp` suffix indicates that the .NET process will pause but not permanently suspend execution, and will continue to run even if it is unable to communicate with `dotnet monitor`.

| API | Supported |
| :-------- | :-------: |
| /process | Yes |
| /dump | Yes |
| /gcdump | Yes |
| /trace | Yes |
| /metrics | Yes |
| /livemetrics | Yes |
| /logs | Yes |
| /info | Yes |
| /operations | Yes |
| /collectionrules | Yes |
| /stacks | Yes |
| /exceptions | Yes |
| /parameters | No |

| Trigger | Supported |
| :-------- | :-------: |
| Trigger - Startup | Partial |
| Trigger - EventCounter | Yes |
| Trigger - EventMeter | Yes |
| Trigger - AspNetResponseStatus | Yes |
| Trigger - AspNetRequestCount | Yes |
| Trigger - AspNetRequestDuration | Yes |


### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fconfiguration)

# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Ordinarily, you are not required to specify most of this configuration and only exists if you wish to change the default behavior in `dotnet monitor`.

>**Note**: Some features are [experimental](./experimental.md) and are denoted as `**[Experimental]**` in this document.

## Configuration Sources

`dotnet monitor` can read and combine configuration from multiple sources. The configuration sources are listed below in the order in which they are read (User-specified json file is highest precedence) :

- Command line parameters
- User settings path
  - On Windows, `%USERPROFILE%\.dotnet-monitor\settings.json`
  - On \*nix, `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`
  -  If `$XDG_CONFIG_HOME` isn't defined, we fall back to ` $HOME/.config/dotnet-monitor/settings.json`
- [Key-per-file](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/#key-per-file-configuration-provider) in the shared settings path
    - On Windows, `%ProgramData%\dotnet-monitor`
    - On \*nix, `/etc/dotnet-monitor`

- Environment variables
- User-Specified json file
  - (6.3+) Use the `--configuration-file-path` flag from the command line to specify your own configuration file (using its full path).

### Translating configuration between providers

While the rest of this document will showcase configuration examples in a json format, the same configuration can be expressed via any of the other configuration sources. For example, the API Key configuration can be expressed via shown below:

```json
{
  "Authentication": {
    "MonitorApiKey": {
      "Subject": "ae5473b6-8dad-498d-b915-ffffffffffff",
      "PublicKey": "eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"
    }
  }
}
```

The same configuration can be expressed via environment variables using the using `__`(double underscore) as the hierarchical separator:

```bash
export Authentication__MonitorApiKey__Subject="ae5473b6-8dad-498d-b915-ffffffffffff"
export Authentication__MonitorApiKey__PublicKey="eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"
```

Environment variables _can_ be prefixed with `DotnetMonitor_` (single underscore) to give them a higher precedence over environment variables without this prefix. In the following example:

```bash
export DotnetMonitor_DefaultProcess__Filters__0__Key="myapp"
export DefaultProcess__Filters__0__Key="dotnet"
```

The value from the variable `DotnetMonitor_DefaultProcess__Filters__0__Key` will be observed rather than the value from the variable `DefaultProcess__Filters__0__Key`, thus `dotnet monitor` will observe `DefaultProcess:Filters:0:Key` to be `myapp`.

#### Kubernetes

When running in Kubernetes, you are able to specify the same configuration via Kubernetes secrets.

```bash
kubectl create secret generic apikey \
  --from-literal=Authentication__MonitorApiKey__Subject=ae5473b6-8dad-498d-b915-ffffffffffff \
  --from-literal=Authentication__MonitorApiKey__PublicKey=eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19 \
  --dry-run=client -o yaml \
  | kubectl apply -f -
```

You can then use a Kubernetes volume mount to supply the secret to the container at runtime.

```yaml 
spec:
  volumes:
  - name: config
    secret:
      secretName: apikey
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/monitor:6
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

Alternatively, you can also use configuration maps to specify configuration to the container at runtime.

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: my-configmap
data:
  Metrics__MetricCount: "6"
```

You can then use a Kubernetes volume mount to supply the configuration map to the container at runtime

```yaml 
spec:
  volumes:
  - name: config
    configmap:
      name: my-configmap
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/monitor:6
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

If using multiple configuration maps, secrets, or some combination of both, you need to use a [projected volume](https://kubernetes.io/docs/concepts/storage/volumes/#projected) to map several volume sources into a single directory

```yaml 
spec:
  volumes:
  - name: config
    projected:
      sources:
        - secret:
            name: apiKey
        - configMap:
            name: my-configmap
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/monitor:6
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```


## Configuration Schema

`dotnet monitor`'s various configuration knobs have been documented via JSON schema. Using a modern editor like VS or VS Code that supports JSON Schema makes it trivial to author complex configuration objects with support for completions and rich descriptions via tooltips.

To get completion support in your editor, simply add the `$schema` property to the root JSON object as shown below:

```json
{
  "$schema": "https://aka.ms/dotnet-monitor-schema"
}
```

Once you've added the `$schema` property, you should have support for completions in your editor.

![completions](https://user-images.githubusercontent.com/4734691/115377729-bf2bb600-a184-11eb-9b8e-50f361c112f0.gif)

## View Merged Configuration

`dotnet monitor` includes a diagnostic command that allows you to output the resulting configuration after merging the configuration from all the various sources.

To view the merged configuration, run the following command:

```cmd
dotnet monitor config show
```
The output of the command should resemble the following JSON object:

```json
Tell us about your experience with dotnet monitor: https://aka.ms/dotnet-monitor-survey

{
  "urls": "https://localhost:52323",
  "Kestrel": ":NOT PRESENT:",
  "CorsConfiguration": ":NOT PRESENT:",
  "DiagnosticPort": {
    "ConnectionMode": "Connect",
    "EndpointName": null
  },
  "Metrics": {
    "Enabled": "True",
    "Endpoints": "http://*:52325",
    "IncludeDefaultProviders": "True",
    "MetricCount": "3",
    "Providers": {
      "0": {
        "CounterNames": {
          "0": "connections-per-second",
          "1": "total-connections"
        },
        "ProviderName": "Microsoft-AspNetCore-Server-Kestrel"
      }
    },
  },
  "Storage": {
    "DumpTempFolder": "C:\\Users\\shirh\\AppData\\Local\\Temp\\"
  },
  "Authentication": {
    "MonitorApiKey": {
      "Subject": "2c866b1a-38c5-4454-a686-1e022e38a7f6",
      "PublicKey": ":REDACTED:"
    }
  },
  "Egress": ":NOT PRESENT:"
}
```

To view the loaded configuration providers, run the following command:

```cmd
dotnet monitor config show --show-sources
```

## Diagnostic Port Configuration

`dotnet monitor` communicates via .NET processes through their diagnostic port. In the default configuration, .NET processes listen on a platform native transport (named pipes on Windows/Unix-domain sockets on \*nix) in a well-known location.

### Connection Mode

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


#### Maximum connection

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

## Kestrel Configuration

// TODO

## Storage Configuration

Some diagnostic features (e.g. memory dumps, stack traces) require that a directory is shared between the `dotnet monitor` tool and the target applications. The `Storage` configuration section allows specifying these directories to facilitate this sharing.

### Default Shared Path (7.0+)

The default shared path option (`DefaultSharedPath`) can be set, which allows artifacts to be shared automatically without requiring additional configuration for each artifact type. By setting this property with an appropriate value, the following become available:
- dumps are temporarily stored in this directory or in a subdirectory.
- **[Experimental]** shared libraries are shared from `dotnet monitor` to target applications in this directory or in a subdirectory.
- **[Experimental]** in-process diagnostics share files back to `dotnet monitor` in this directory or in a subdirectory.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "DefaultSharedPath": "/diag"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Storage__DefaultSharedPath: "/diag"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Storage__DefaultSharedPath
    value: "/diag"
  ```
</details>

### Dumps Path

Unlike the other diagnostic artifacts (for example, traces), memory dumps aren't streamed back from the target process to `dotnet monitor`. Instead, they are written directly to disk by the runtime. After successful collection of a process dump, `dotnet monitor` will read the process dump directly from disk. In the default configuration, the directory that the runtime writes its process dump to is the temp directory (`%TMP%` on Windows, `/tmp` on \*nix). It is possible to change to the ephemeral directory that these dump files get written to via the following configuration:

>**Note**: This option is optional if `dotnet monitor` is running in the same process namespace as the target processes or if `DefaultSharedPath` is specified.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "DumpTempFolder": "/diag/dumps/"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Storage__DumpTempFolder: "/diag/dumps/"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Storage__DumpTempFolder
    value: "/diag/dumps/"
  ```
</details>

### **[Experimental]** Shared Library Path (7.0+)

The shared library path option (`SharedLibraryPath`) allows specifying the path to where shared libraries are copied from the `dotnet monitor` installation to make them available to target applications for in-process diagnostics scenarios, such as call stack collection.

>**Note**: This option is not required if `DefaultSharedPath` is specified. This option provides an alternative directory path compared to the behavior of specifying `DefaultSharedPath`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "SharedLibraryPath": "/diag/libs/"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Storage__SharedLibraryPath: "/diag/libs/"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Storage__SharedLibraryPath
    value: "/diag/libs/"
  ```
</details>

## Default Process Configuration

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

### Examples

#### Match the IIS Express process by name

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

#### Match the IIS Express process by name (Shorthand)

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

#### Match pid 1

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

#### Match pid 1 (Shorthand)

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

## Cross-Origin Resource Sharing (CORS) Configuration

// TODO

## Metrics Configuration

### Global Counter Interval

Due to limitations in event counters, `dotnet monitor` supports only **one** refresh interval when collecting metrics. This interval is used for
Prometheus metrics, livemetrics, triggers, traces, and trigger actions that collect traces. The default interval is 5 seconds, but can be changed in configuration.

<details>
  <summary>JSON</summary>

  ```json
  {
      "GlobalCounter": {
        "IntervalSeconds": 10
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  GlobalCounter__IntervalSeconds: "10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_GlobalCounter__IntervalSeconds
    value: "10"
  ```
</details>

### Metrics Urls

In addition to the ordinary diagnostics urls that `dotnet monitor` binds to, it also binds to metric urls that only expose the `/metrics` endpoint. Unlike the other endpoints, the metrics urls do not require authentication. Unless you enable collection of custom providers that may contain sensitive business logic, it is generally considered safe to expose metrics endpoints. 

<details>
  <summary>Command Line</summary>

  ```cmd
  dotnet monitor collect --metricUrls http://*:52325
  ```
</details>

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Endpoints": "http://*:52325"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__Endpoints: "http://*:52325"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__Endpoints
    value: "http://*:52325"
  ```
</details>

### Customize collection interval and counts

In the default configuration, `dotnet monitor` requests that the connected runtime provides updated counter values every 5 seconds and will retain 3 data points for every collected metric. When using a collection tool like Prometheus, it is recommended that you set your scrape interval to `MetricCount` * `GlobalCounter:IntervalSeconds`. In the default configuration, we recommend you scrape `dotnet monitor` for metrics every 15 seconds.

You can customize the number of data points stored per metric via the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "MetricCount": 3
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__MetricCount: "3"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__MetricCount
    value: "3"
  ```
</details>

See [Global Counter Interval](#global-counter-interval) to change the metrics frequency.

### Custom Metrics

Additional metrics providers and counter names to return from this route can be specified via configuration. 

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Providers": [
        {
          "ProviderName": "Microsoft-AspNetCore-Server-Kestrel",
          "CounterNames": [
            "connections-per-second",
            "total-connections"
          ]
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__Providers__0__ProviderName: "Microsoft-AspNetCore-Server-Kestrel"
  Metrics__Providers__0__CounterNames__0: "connections-per-second"
  Metrics__Providers__0__CounterNames__1: "total-connections"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__Providers__0__ProviderName
    value: "Microsoft-AspNetCore-Server-Kestrel"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__0
    value: "connections-per-second"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__1
    value: "total-connections"
  ```
</details>

> **Warning:** In the default configuration, custom metrics will be exposed along with all other metrics on an unauthenticated endpoint. If your metrics contains sensitive information, we recommend disabling the [metrics urls](#metrics-urls) and consuming metrics from the authenticated endpoint (`--urls`) instead.

When `CounterNames` are not specified, all the counters associated with the `ProviderName` are collected.

[7.1+] Custom metrics support labels for metadata. Metadata cannot include commas (`,`); the inclusion of a comma in metadata will result in all metadata being removed from the custom metric.

### Disable default providers

In addition to enabling custom providers, `dotnet monitor` also allows you to disable collection of the default providers. You can do so via the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "IncludeDefaultProviders": false
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__IncludeDefaultProviders: "false"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__IncludeDefaultProviders
    value: "false"
  ```
</details>

## Egress Configuration

### Azure blob storage egress provider

| Name | Type | Required | Description |
|---|---|---|---|
| accountUri | string | true | The URI of the Azure blob storage account.|
| containerName | string | true | The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.|
| blobPrefix | string | false | Optional path prefix for the artifacts to egress.|
| copyBufferSize | string | false | The buffer size to use when copying data from the original artifact to the blob stream.|
| accountKey | string | false | The account key used to access the Azure blob storage account; must be specified if `accountKeyName` is not specified.|
| sharedAccessSignature | string | false | The shared access signature (SAS) used to access the Azure blob and optionally queue storage accounts; if using SAS, must be specified if `sharedAccessSignatureName` is not specified.|
| accountKeyName | string | false | Name of the property in the Properties section that will contain the account key; must be specified if `accountKey` is not specified.|
| managedIdentityClientId | string | false | The ClientId of the ManagedIdentity that can be used to authorize egress. Note this identity must be used by the hosting environment (such as Kubernetes) and must also have a Storage role with appropriate permissions. |
| sharedAccessSignatureName | string | false | Name of the property in the Properties section that will contain the SAS token; if using SAS, must be specified if `sharedAccessSignature` is not specified.|
| queueName | string | false | The name of the queue to which a message will be dispatched upon writing to a blob.|
| queueAccountUri | string | false | The URI of the Azure queue storage account.|
| queueSharedAccessSignature | string | false | (6.3+) The shared access signature (SAS) used to access the Azure queue storage account; if using SAS, must be specified if `queueSharedAccessSignatureName` is not specified.|
| queueSharedAccessSignatureName | string | false | (6.3+) Name of the property in the Properties section that will contain the queue SAS token; if using SAS, must be specified if `queueSharedAccessSignature` is not specified.|
| metadata | Dictionary<string, string> | false | A mapping of metadata keys to environment variable names. The values of the environment variables will be added as metadata for egressed artifacts.|

> **Note**: Starting with `dotnet monitor` 7.0, all built-in metadata keys are prefixed with `DotnetMonitor_`; to avoid metadata naming conflicts, avoid prefixing your metadata keys with `DotnetMonitor_`.

### Example azureBlobStorage provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

### Example azureBlobStorage provider with queue

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey",
                  "queueAccountUri": "https://exampleaccount.queue.core.windows.net",
                  "queueName": "dotnet-monitor-queue"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__AzureBlobStorage__monitorBlob__queueAccountUri: "https://exampleaccount.queue.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__queueName: "dotnet-monitor-queue"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueAccountUri
    value: "https://exampleaccount.queue.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueName
    value: "dotnet-monitor-queue"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

#### azureBlobStorage Queue Message Format

The Queue Message's payload will be the blob name (`<BlobPrefix>/<ArtifactName>`; using the above example with an artifact named `mydump.dmp`, this would be `artifacts/mydump.dmp`) that is being egressed to blob storage. This is designed to be easily integrated into an Azure Function that triggers whenever a new message is added to the queue, providing you with the contents of the artifact as a stream. See [Azure Blob storage input binding for Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-input?tabs=csharp#example) for an example.

### S3 storage egress provider

| Name | Type | Required | Description |
|---|---|---|---|
| endpoint | string | false | An optional endpoint of S3 storage service. Can be left empty in case of using AWS. |
| bucketName | string | true | The name of the S3 Bucket to which the blob will be egressed |
| accessKeyId | string | false | The AWS AccessKeyId for IAM user to login.  |
| secretAccessKey | string | false | The AWS SecretAccessKey associated AccessKeyId for IAM user to login. To login by access key id either the 'secretAccessKeyFile' or 'secretAccessKey' must be set. |
| awsProfileName | string | false | The AWS profile name to be used for login. |
| awsProfilePath | string | false | The AWS profile path, if profile details not stored in default path. |
| generatePreSignedUrl | bool | false | A boolean flag to control if either a pre-signed url is returned after successful upload or only the name of bucket and the artifacts S3 object key. |
| regionName | string | false | A Region is a named set of AWS resources in the same geographical area. This option specifies the region to connect to. |
| preSignedUrlExpiry | TimeStamp? | false | The amount of time the generated pre-signed url should be accessible. The value has to be between 1 minute and 1 day. |
| forcePathStyle | bool | false | The boolean flag set for AWS connection configuration ForcePathStyle option. |
| copyBufferSize | int | false | The buffer size to use when copying data from the original artifact to the blob stream. There is a minimum size of 5 MB which is set when the given value is lower.|

### Example S3 storage provider

<details>
  <summary>JSON with password</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "http://localhost:9000",
                  "bucketName": "myS3Bucket",
                  "accessKeyId": "minioUser",
                  "secretAccessKey": "mySecretPassword",
                  "regionName": "us-east-1",
                  "generatePresSignedUrl" : true,
                  "preSignedUrlExpiry" : "00:15:00",
                  "copyBufferSize": 1024
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes Secret</summary>
  
  ```sh
  #!/bin/sh
  kubectl create secret generic my-s3-secrets \
  --from-literal=Egress__S3Storage__monitorS3Blob__bucketName=myS3Bucket \
  --from-literal=Egress__S3Storage__monitorS3Blob__accessKeyId=minioUser \
  --from-literal=Egress__S3Storage__monitorS3Blob__secretAccessKey=mySecretPassword \
  --from-literal=Egress__S3Storage__monitorS3Blob__regionName=us-east-1 \
  --dry-run=client -o yaml | kubectl apply -f -
  ```
</details>

### Filesystem egress provider

| Name | Type | Description |
|---|---|---|
| directoryPath | string | The directory path to which the stream data will be egressed.|
| intermediateDirectoryPath | string | The directory path to which the stream data will initially be written; if specified, the file will then be moved/renamed to the directory specified in 'directoryPath'.|

### Example fileSystem provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "FileSystem": {
              "monitorFile": {
                  "directoryPath": "/artifacts",
                  "intermediateDirectoryPath": "/intermediateArtifacts"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Egress__FileSystem__monitorFile__directoryPath: "/artifacts"
  Egress__FileSystem__monitorFile__intermediateDirectoryPath: "/intermediateArtifacts"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__directoryPath
    value: "/artifacts"
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__intermediateDirectoryPath
    value: "/intermediateArtifacts"
  ```
</details>

## Collection Rule Configuration

Collection rules are specified in configuration as a named item under the `CollectionRules` property at the root of the configuration. Each collection rule has four properties for describing the behavior of the rule: `Filters`, `Trigger`, `Actions`, and `Limits`.

### Example

The following is a collection rule that collects a 1 minute CPU trace and egresses it to a provider named "TmpDir" after it has detected high CPU usage for 10 seconds. The rule only applies to processes named "dotnet" and only collects at most 2 traces per 1 hour sliding time window.

<details>
  <summary>JSON</summary>

  ```json
  {
      "CollectionRules": {
          "HighCpuRule": {
              "Filters": [{
                  "Key": "ProcessName",
                  "Value": "dotnet",
                  "MatchType": "Exact"
              }],
              "Trigger": {
                  "Type": "EventCounter",
                  "Settings": {
                      "ProviderName": "System.Runtime",
                      "CounterName": "cpu-usage",
                      "GreaterThan": 70,
                      "SlidingWindowDuration": "00:00:10"
                  }
              },
              "Actions": [{
                  "Type": "CollectTrace",
                  "Settings": {
                      "Profile": "Cpu",
                      "Duration": "00:01:00",
                      "Egress": "TmpDir"
                  }
              }],
              "Limits": {
                  "ActionCount": 2,
                  "ActionCountSlidingWindowDuration": "1:00:00"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__HighCpuRule__Filters__0__Key: "ProcessName"
  CollectionRules__HighCpuRule__Filters__0__Value: "dotnet"
  CollectionRules__HighCpuRule__Filters__0__MatchType: "Exact"
  CollectionRules__HighCpuRule__Trigger__Type: "EventCounter"
  CollectionRules__HighCpuRule__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__HighCpuRule__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__HighCpuRule__Trigger__Settings__GreaterThan: "70"
  CollectionRules__HighCpuRule__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  CollectionRules__HighCpuRule__Actions__0__Type: "CollectTrace"
  CollectionRules__HighCpuRule__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__HighCpuRule__Actions__0__Settings__Duration: "00:01:00"
  CollectionRules__HighCpuRule__Actions__0__Settings__Egress: "TmpDir"
  CollectionRules__HighCpuRule__Limits__ActionCount: "2"
  CollectionRules__HighCpuRule__Limits__ActionCountSlidingWindowDuration: "1:00:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__Value
    value: "dotnet"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Filters__0__MatchType
    value: "Exact"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Type
    value: "EventCounter"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__GreaterThan
    value: "70"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Duration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Actions__0__Settings__Egress
    value: "TmpDir"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Limits__ActionCount
    value: "2"
  - name: DotnetMonitor_CollectionRules__HighCpuRule__Limits__ActionCountSlidingWindowDuration
    value: "1:00:00"
  ```
</details>

### Filters

Each collection rule can specify a set of process filters to select which processes the rule should be applied. The filter criteria are the same as those used for the [default process](#default-process-configuration) configuration.

#### Example

The following example shows the `Filters` portion of a collection rule that has the rule only apply to processes named `dotnet` and whose command line contains `myapp.dll`.

<details>
  <summary>JSON</summary>

  ```json
  {
      "Filters": [{
          "Key": "ProcessName",
          "Value": "dotnet",
          "MatchType": "Exact"
      },{
          "CommandLine": "myapp.dll",
          "MatchType": "Contains"
      }]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Filters__0__Key: "ProcessName"
  CollectionRules__RuleName__Filters__0__Value: "dotnet"
  CollectionRules__RuleName__Filters__0__MatchType: "Exact"
  CollectionRules__RuleName__Filters__1__CommandLine: "myapp.dll"
  CollectionRules__RuleName__Filters__1__MatchType: "Contains"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__Key
    value: "ProcessName"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__Value
    value: "dotnet"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__0__MatchType
    value: "Exact"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__1__CommandLine
    value: "myapp.dll"
  - name: DotnetMonitor_CollectionRules__RuleName__Filters__1__MatchType
    value: "Contains"
  ```
</details>

### Triggers

#### `AspNetRequestCount` Trigger

A trigger that has its condition satisfied when the number of HTTP requests is above the described threshold level for a sliding window of time. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of requests that start within the sliding window of time. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when request count is higher than 500 requests during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "RequestCount": 500,
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "500"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "500"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

#### `AspNetRequestDuration` Trigger

A trigger that has its condition satisfied when the number of HTTP requests have response times longer than the threshold duration for a sliding window of time. Long running requests (ones that do not send a complete response within the threshold duration) are included in the count. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `RequestCount` | int | true | The threshold of the number of slow requests that start within the sliding window of time. | | | |
| `RequestDuration` | TimeSpan? | false | The threshold of the amount of time in which a request is considered to be slow. | `"00:00:05"` (5 seconds) | `"00:00:00"` (zero seconds) | `"01:00:00"` (1 hour) |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the the number of slow requests are counted. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when 10 requests take longer than 3 seconds during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "RequestCount": 10,
    "RequestDuration": "00:00:03",
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "10"
  CollectionRules__RuleName__Trigger__Settings__RequestDuration: "00:00:03"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestDuration
    value: "00:00:03"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

#### `AspNetResponseStatus` Trigger

A trigger that has its condition satisfied when the number of HTTP responses that have status codes matching the pattern list is above the specified threshold for a sliding window of time. The request paths can be filtered according to the described patterns.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `StatusCodes` | string[] | true | The list of HTTP response status codes to monitor. Each item of the list can be a single code or a range of codes (e.g. `"400-499"`). | | | |
| `RequestCount` | int | true | The threshold number of responses with matching status codes. | | | |
| `SlidingWindowDuration` | TimeSpan? | false | The sliding time window in which the number of responses with matching status codes must occur. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |
| `IncludePaths` | string[] | false | The list of request path patterns to monitor. If not specified, all request paths are considered. If specified, only request paths matching one of the patterns in this list will be considered. Request paths matching a pattern in the `ExcludePaths` list will be ignored. | `null` | | |
| `ExcludePaths` | string[] | false | The list of request path patterns to ignore. Request paths matching a pattern in this list will be ignored. | `null` | | |

The `IncludePaths` and `ExcludePaths` support [wildcards and globbing](#aspnet-request-path-wildcards-and-globbing).

##### Example

Usage that is satisfied when 10 requests respond with a 5XX status code during a 1 minute period for all paths under the `/api` route:

<details>
  <summary>JSON</summary>

  ```json
  {
    "StatusCodes": [ "500-599" ],
    "RequestCount": 10,
    "SlidingWindowDuration": "00:01:00",
    "IncludePaths": [ "/api/**/*" ]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Trigger__Settings__StatusCodes__0: "500-599"
  CollectionRules__RuleName__Trigger__Settings__RequestCount: "10"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:01:00"
  CollectionRules__RuleName__Trigger__Settings__IncludePaths__0: "/api/**/*"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__StatusCodes__0
    value: "500-599"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:01:00"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__IncludePaths__0
    value: "/api/**/*"
  ```
</details>

#### `EventCounter` Trigger

A trigger that has its condition satisfied when the value of a counter falls above, below, or between the described threshold values for a duration of time.

See [Well-known Counters in .NET](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/available-counters) for a list of known available counters. Custom counters from custom event sources are supported as well.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ProviderName` | string | true | The name of the event source that provides the counter information. | | | |
| `CounterName` | string | true | The name of the counter to monitor. | | | |
| `GreaterThan` | double? | false | The threshold level the counter must maintain (or higher) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `LessThan` | double? | false | The threshold level the counter must maintain (or lower) for the specified duration. Either `GreaterThan` or `LessThan` (or both) must be specified. | `null` | | |
| `SlidingWindowDuration` | false | TimeSpan? | The sliding time window in which the counter must maintain its value as specified by the threshold levels in `GreaterThan` and/or `LessThan`. | `"00:01:00"` (one minute) | `"00:00:01"` (one second) | `"1.00:00:00"` (1 day) |

##### Example

Usage that is satisfied when the CPU usage of the application is higher than 70% for a 10 second window.

<details>
  <summary>JSON</summary>

  ```json
  {
    "ProviderName": "System.Runtime",
    "CounterName": "cpu-usage",
    "GreaterThan": 70,
    "SlidingWindowDuration": "00:00:10"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Trigger__Settings__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Trigger__Settings__CounterName: "cpu-usage"
  CollectionRules__RuleName__Trigger__Settings__GreaterThan: "70"
  CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration: "00:00:10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__CounterName
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__GreaterThan
    value: "70"
  - name: DotnetMonitor_CollectionRules__RuleName__Trigger__Settings__SlidingWindowDuration
    value: "00:00:10"
  ```
</details>

#### Built-In Default Triggers

These [trigger shortcuts](collectionrules/triggershortcuts.md) simplify configuration for several common `EventCounter` providers.

#### ASP.NET Request Path Wildcards and Globbing

The `IncludePaths` and `ExcludePaths` properties of the ASP.NET triggers allow for wildcards and globbing so that every included or excluded path does not necessarily need to be explicitly specified. For these triggers, a match with an `ExcludePaths` pattern will supersede a match with an `IncludePaths` pattern.

The globstar `**/` will match zero or more path segments including the forward slash `/` character at the end of the segment.

The wildcard `*` will match zero or more non-forward-slash `/` characters.

##### Examples

| Pattern | Matches | Non-Matches |
|---|---|---|
| `**/*` | All paths | No exclusions |
| `/images/**/*` | `/images/logo.png`, `/images/products/1.png` | `/index/header.png` |
| `**/*.js` | `/script.js`, `/path/script.js`, `/path/sub/script.js` | `/script.js/page.html` |
| `**/sub/*.html` | `/path/sub/page.html`, `/sub/page.html` | `/sub/script.js`, `/path/doc.txt` |

### Actions

#### `CollectDump` Action

An action that collects a dump of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Type` | [DumpType](api/definitions.md#dumptype) | false | The type of dump to collect | `WithHeap` |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected dump. | |

##### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

##### Example

Usage that collects a full dump and egresses it to a provider named "AzureBlobDumps".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Type": "Full",
    "Egress": "AzureBlobDumps"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Type: "Full"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlobDumps"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Type
    value: "Full"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlobDumps"
  ```
</details>

#### `CollectGCDump` Action

An action that collects a gcdump of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected gcdump. | |

##### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

##### Example

Usage that collects a gcdump and egresses it to a provider named "AzureBlobGCDumps".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": "AzureBlobGCDumps"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlobGCDumps"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlobGCDumps"
  ```
</details>

#### `CollectTrace` Action

An action that collects a trace of the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `Profile` | [TraceProfile](api/definitions.md#traceprofile)? | false | The name of the profile(s) used to collect events. See [TraceProfile](api/definitions.md#traceprofile) for details on the list of event providers, levels, and keywords each profile represents. Multiple profiles may be specified by separating them with commas. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `Providers` | [EventProvider](api/definitions.md#eventprovider)[] | false | List of event providers from which to capture events. Either `Profile` or `Providers` must be specified, but not both. | `null` | | |
| `RequestRundown` | bool | false | The runtime may provide additional type information for certain types of events after the trace session is ended. This additional information is known as rundown events. Without this information, some events may not be parsable into useful information. Only applies when `Providers` is specified. | `true` | | |
| `BufferSizeMegabytes` | int | false | The size (in megabytes) of the event buffer used in the runtime. If the event buffer is filled, events produced by event providers may be dropped until the buffer is cleared. Increase the buffer size to mitigate this or pair down the list of event providers, keywords, and level to filter out extraneous events. Only applies when `Providers` is specified. | `256` | `1` | `1024` |
| `Duration` | TimeSpan? | false | The duration of the trace operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected trace. | | | |
| `StoppingEvent` | [TraceEventFilter](api/definitions.md#traceeventfilter)? | false | The event to watch for while collecting the trace, and once either the event is hit or the `Duration` is reached the trace will be stopped. This can only be specified if `Providers` is set. | `null` | | |

##### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

##### Example

Usage that collects a CPU trace for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Profile": "Cpu",
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

#### `CollectLiveMetrics` Action

An action that collects live metrics for the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `IncludeDefaultProviders` | bool | false | Determines if the default counter providers should be used. | `true` | | |
| `Providers` | [EventMetricsProvider](api/definitions.md#eventmetricsprovider)[] | false | The array of counter providers for metrics to collect. | | | |
| `Meters` | [EventMetricsMeter](api/definitions.md#eventmetricsmeter)[] | false | The array of meters for metrics to collect. | | | |
| `Duration` | TimeSpan? | false | The duration of the live metrics operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected live metrics. | | | |

##### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

##### Example

Usage that collects live metrics with the default providers for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

##### Example

Usage that collects live metrics for the `cpu-usage` counter on `System.Runtime` for 20 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "UseDefaultProviders": false,
    "Providers": [
      {
        "ProviderName": "System.Runtime",
        "CounterNames": [ "cpu-usage" ]
      }
    ],
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__UseDefaultProviders: "false"
  CollectionRules__RuleName__Actions__0__Settings__Providers__0__ProviderName: "System.Runtime"
  CollectionRules__RuleName__Actions__0__Settings__Providers__0__CounterNames__0: "cpu-usage"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__UseDefaultProviders
    value: "false"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Providers__0__ProviderName
    value: "System.Runtime"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Providers__0__CounterNames__0
    value: "cpu-usage"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

#### `CollectLogs` Action

An action that collects logs for the process that the collection rule is targeting.

##### Properties

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `DefaultLevel` | [LogLevel](api/definitions.md#loglevel)? | false | The default log level at which logs are collected for entries in the FilterSpecs that do not have a specified LogLevel value. | `LogLevel.Warning` | | |
| `FilterSpecs` | Dictionary<string, [LogLevel](api/definitions.md#loglevel)?> | false | A custom mapping of logger categories to log levels that describes at what level a log statement that matches one of the given categories should be captured. | `null` | | |
| `UseAppFilters` | bool | false | Specifies whether to capture log statements at the levels as specified in the application-defined filters. | `true` | | |
| `Format` | [LogFormat](api/definitions.md#logformat)? | false | The format of the logs artifact. | `PlainText` | | |
| `Duration` | TimeSpan? | false | The duration of the logs operation. | `"00:00:30"` (30 seconds) | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected logs. | | | |

##### Outputs

| Name | Description |
|---|---|
| `EgressPath` | The path of the file that was egressed using the specified egress provider. |

##### Example

Usage that collects logs at the Information level for 30 seconds and egresses it to a provider named "TmpDir".

<details>
  <summary>JSON</summary>

  ```json
  {
    "DefaultLevel": "Information",
    "UseAppFilters": false,
    "Egress": "TmpDir"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__DefaultLevel: "Information"
  CollectionRules__RuleName__Actions__0__Settings__UseAppFilters: "false"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "TmpDir"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__DefaultLevel
    value: "Information"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__UseAppFilters
    value: "false"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "TmpDir"
  ```
</details>

#### `Execute` Action

An action that executes an executable found in the file system. Non-zero exit code will fail the action.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path to the executable. | |
| `Arguments` | string | false | The arguments to pass to the executable. | `null` |
| `IgnoreExitCode` | bool? | false | Ignores checking that the exit code is zero. | `false` |

##### Outputs

| Name | Description |
|---|---|
| `ExitCode` | The exit code of the process. |

##### Example

Usage that executes a .NET executable named `myapp.dll` using `dotnet`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Path": "C:\\Program Files\\dotnet\\dotnet.exe",
    "Arguments": "C:\\Program Files\\MyApp\\myapp.dll"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Path: "C:\\Program Files\\dotnet\\dotnet.exe"
  CollectionRules__RuleName__Actions__0__Settings__Arguments: "C:\\Program Files\\MyApp\\myapp.dll"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Path
    value: "C:\\Program Files\\dotnet\\dotnet.exe"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Arguments
    value: "C:\\Program Files\\MyApp\\myapp.dll"
  ```
</details>

#### **[Experimental]** `CollectStacks` Action (7.0+)

Collect call stacks from the target process.

>**Note**: This feature is [experimental](./experimental.md). To enable this feature, set `DotnetMonitor_Experimental_Feature_CallStacks` to `true` as an environment variable on the `dotnet monitor` process or container. Additionally, the [in-process features](#experimental-in-process-features-configuration-70) must be enabled since the call stacks feature uses shared libraries loaded into the target application for collecting the call stack information.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Format` | [CallStackFormat](api/definitions.md#experimental-callstackformat-70) | false | The format of the collected call stack. | `Json` |
| `Egress` | string | true | The named [egress provider](egress.md) for egressing the collected stacks. | |

#### `LoadProfiler` Action

An action that loads an [ICorProfilerCallback](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-interface) implementation into a target process as a startup profiler. This action must be used in a collection rule with a `Startup` trigger.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Path` | string | true | The path of the profiler library to be loaded. This is typically the same value that would be set as the CORECLR_PROFILER_PATH environment variable. | |
| `Clsid` | Guid | true | The class identifier (or CLSID, typically a GUID) of the ICorProfilerCallback implementation. This is typically the same value that would be set as the CORECLR_PROFILER environment variable. | |

##### Outputs

No outputs

##### Example

Usage that loads one of the sample profilers from [`dotnet/runtime: src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp`](https://github.com/dotnet/runtime/blob/9ddd58a58d14a7bec5ed6eb777c6703c48aca15d/src/tests/profiler/native/gcallocateprofiler/gcallocateprofiler.cpp).

<details>
  <summary>JSON</summary>

  ```json
  {
    "Path": "Profilers\\Profiler.dll",
    "Clsid": "55b9554d-6115-45a2-be1e-c80f7fa35369"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Path: "Profilers\\Profiler.dll"
  CollectionRules__RuleName__Actions__0__Settings__Clsid: "55b9554d-6115-45a2-be1e-c80f7fa35369"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Path
    value: "Profilers\\Profiler.dll"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Clsid
    value: "55b9554d-6115-45a2-be1e-c80f7fa35369"
  ```
</details>

#### `SetEnvironmentVariable` Action

An action that sets an environment variable value in the target process. This action should be used in a collection rule with a `Startup` trigger.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to set. | |
| `Value` | string | false | The value of the environment variable to set. | `null` |

##### Outputs

No outputs

##### Example

Usage that sets a parameter to the profiler you loaded. In this case, your profiler might be looking for an account key defined in `MyProfiler_AccountId` which is used to communicate to some outside system.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Name": "MyProfiler_AccountId",
    "Value": "8fb138d2c44e4aea8545cc2df541ed4c"
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Settings__Name: "MyProfiler_AccountId"
  CollectionRules__RuleName__Actions__0__Settings__Value: "8fb138d2c44e4aea8545cc2df541ed4c"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Name
    value: "MyProfiler_AccountId"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Value
    value: "8fb138d2c44e4aea8545cc2df541ed4c"
  ```
</details>

#### `GetEnvironmentVariable` Action

An action that gets an environment variable from the target process. Its value is set as the `Value` action output.

##### Properties

| Name | Type | Required | Description | Default Value |
|---|---|---|---|---|
| `Name` | string | true | The name of the environment variable to get. | |

##### Outputs

| Name | Description |
|---|---|
| `Value` | The value of the environment variable in the target process. |

##### Example

Usage that gets a token your app has access to and uses it to send a trace.

> **Note**: the example below is of an entire action list to provide context, only the second json entry represents the `GetEnvironmentVariable` Action.

<details>
  <summary>JSON</summary>

  ```json
  [{
      "Name": "A",
      "Type": "CollectTrace",
      "Settings": {
          "Profile": "Cpu",
          "Egress": "AzureBlob"
      }
  },{
      "Name": "GetEnvAction",
      "Type": "GetEnvironmentVariable",
      "Settings": {
         "Name": "Azure_SASToken",
      }
  },{
      "Name": "B",
      "Type": "Execute",
      "Settings": {
          "Path": "azcopy",
          "Arguments": "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
      }
  }]
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Actions__0__Name: "A"
  CollectionRules__RuleName__Actions__0__Type: "CollectTrace"
  CollectionRules__RuleName__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlob"
  CollectionRules__RuleName__Actions__1__Name: "GetEnvAction"
  CollectionRules__RuleName__Actions__1__Type: "GetEnvironmentVariable"
  CollectionRules__RuleName__Actions__1__Settings__Name: "Azure_SASToken"
  CollectionRules__RuleName__Actions__2__Name: "B"
  CollectionRules__RuleName__Actions__2__Type: "Execute"
  CollectionRules__RuleName__Actions__2__Settings__Path: "azcopy"
  CollectionRules__RuleName__Actions__2__Settings__Arguments: "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Name
    value: "A"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlob"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Name
    value: "GetEnvAction"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Type
    value: "GetEnvironmentVariable"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Settings__Name
    value: "Azure_SASToken"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Name
    value: "B"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Type
    value: "Execute"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Settings__Path
    value: "azcopy"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__2__Settings__Arguments
    value: "$(Actions.A.EgressPath) https://Contoso.blob.core.windows.net/MyTraces/AwesomeAppTrace.nettrace?$(Actions.GetEnvAction.Value)"
  ```
</details>

### Limits

Collection rules have limits that constrain the lifetime of the rule and how often its actions can be run before being throttled.

| Name | Type | Required | Description | Default Value | Min Value | Max Value |
|---|---|---|---|---|---|---|
| `ActionCount` | int | false | The number of times the action list may be executed before being throttled. | 5 | | |
| `ActionCountSlidingWindowDuration` | TimeSpan? | false | The sliding window of time to consider whether the action list should be throttled based on the number of times the action list was executed. Executions that fall outside the window will not count toward the limit specified in the ActionCount setting. If not specified, all action list executions will be counted for the entire duration of the rule. | `null` | `"00:00:01"` (1 second) | `"1.00:00:00"` (1 day) |
| `RuleDuration` | TimeSpan? | false | The amount of time before the rule will stop monitoring a process after it has been applied to a process. If not specified, the rule will monitor the process with the trigger indefinitely. | `null` | `"00:00:01"` (1 second) | `"365.00:00:00"` (1 year) |

#### Example

The following example shows the `Limits` portion of a collection rule that has the rule only allow its actions to run 3 times within a 1 hour sliding time window.

<details>
  <summary>JSON</summary>

  ```json
  {
      "Limits": {
          "ActionCount": 3,
          "ActionCountSlidingWindowDuration": "01:00:00"
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  CollectionRules__RuleName__Limits__ActionCount: "3"
  CollectionRules__RuleName__Limits__ActionCountSlidingWindowDuration: "01:00:00"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Limits__ActionCount
    value: "3"
  - name: DotnetMonitor_CollectionRules__RuleName__Limits__ActionCountSlidingWindowDuration
    value: "01:00:00"
  ```
</details>

## Collection Rule Defaults

Collection rule defaults are specified in configuration as a named item under the `CollectionRuleDefaults` property at the root of the configuration. Defaults can be used to limit the verbosity of configuration, allowing frequently used values for collection rules to be assigned as defaults. The following are the currently supported collection rule defaults:

| Name | Section | Type | Applies To |
|---|---|---|---|
| `Egress` | `Actions` | string | [CollectDump](#collectdump-action), [CollectGCDump](#collectgcdump-action), [CollectTrace](#collecttrace-action), [CollectLiveMetrics](#collectlivemetrics-action), [CollectLogs](#collectlogs-action) |
| `SlidingWindowDuration` | `Triggers` | TimeSpan? | [AspNetRequestCount](#aspnetrequestcount-trigger), [AspNetRequestDuration](#aspnetrequestduration-trigger), [AspNetResponseStatus](#aspnetresponsestatus-trigger), [EventCounter](#eventcounter-trigger) |
| `RequestCount` | `Triggers` | int | [AspNetRequestCount](#aspnetrequestcount-trigger), [AspNetRequestDuration](#aspnetrequestduration-trigger) |
| `ResponseCount` | `Triggers` | int | [AspNetResponseStatus](#aspnetresponsestatus-trigger) |
| `ActionCount` | `Limits` | int | [Limits](#limits) |
| `ActionCountSlidingWindowDuration` | `Limits` | TimeSpan? | [Limits](#limits) |
| `RuleDuration` | `Limits` | TimeSpan? | [Limits](#limits) |

### Example

The following example includes a default egress provider that corresponds to the `FileSystem` egress provider named `artifacts`. The first action, `CollectDump`, is able to omit the `Settings` section by using the default egress provider. The second action, `CollectGCDump`, is using an egress provider other than the default, and specifies that it will egress to an `AzureBlobStorage` provider named `monitorBlob`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Egress": {
      "AzureBlobStorage": {
        "monitorBlob": {
          "accountUri": "https://exampleaccount.blob.core.windows.net",
          "containerName": "dotnet-monitor",
          "blobPrefix": "artifacts",
          "accountKeyName": "MonitorBlobAccountKey"
        }
      },
      "Properties": {
        "MonitorBlobAccountKey": "accountKey"
      },
      "FileSystem": {
        "artifacts": {
          "directoryPath": "/artifacts",
          "intermediateDirectoryPath": "/intermediateArtifacts"
        }
      }
    },
    "CollectionRuleDefaults": {
      "Actions": {
        "Egress": "artifacts"    
      }
    },
    "CollectionRules": {
      "HighRequestCount": {
        "Trigger": {
          "Type": "AspNetRequestCount",
          "Settings": {
            "RequestCount": 10
          }
        },
        "Actions": [
          {
            "Type": "CollectDump"
          },
          {
            "Type": "CollectGCDump",
            "Settings": {
              "Egress": "monitorBlob"
            }
          }
        ]
      }
    }
  }
  ```
</details>


<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  Egress__FileSystem__artifacts__directoryPath: "/artifacts"
  Egress__FileSystem__artifacts__intermediateDirectoryPath: "/intermediateArtifacts"
  CollectionRuleDefaults__Actions__Egress: "artifacts"
  CollectionRules__HighRequestCount__Trigger__Type: "AspNetRequestCount"
  CollectionRules__HighRequestCount__Trigger__Settings__RequestCount: "10"
  CollectionRules__HighRequestCount__Actions__0__Type: "CollectDump"
  CollectionRules__HighRequestCount__Actions__1__Type: "CollectGCDump"
  CollectionRules__HighRequestCount__Actions__1__Settings__Egress: "monitorBlob"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  - name: DotnetMonitor_Egress__FileSystem__artifacts__directoryPath
    value: "/artifacts"
  - name: DotnetMonitor_Egress__FileSystem__artifacts__intermediateDirectoryPath
    value: "/intermediateArtifacts"
  - name: DotnetMonitor_CollectionRuleDefaults__Actions__Egress
    value: "artifacts"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Type
    value: "AspNetRequestCount"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Trigger__Settings__RequestCount
    value: "10"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__0__Type
    value: "CollectDump"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__1__Type
    value: "CollectGCDump"
  - name: DotnetMonitor_CollectionRules__HighRequestCount__Actions__1__Settings__Egress
    value: "monitorBlob"
  ```
</details>

## **[Experimental]** In-Process Features Configuration (7.0+)

Some features of `dotnet monitor` require loading libraries into target applications. These libraries ship with `dotnet monitor` and are provisioned to be available to target applications using the `DefaultSharedPath` option in the [storage configuration](#storage-configuration) section. The following features require these in-process libraries to be used:

- Call stack collection

Because these libraries are loaded into the target application (they are not loaded into `dotnet monitor`), they may have performance impact on memory and CPU utilization in the target application. These features are off by default and may be enabled via the `InProcessFeatures` configuration section.

### Example

To enable in-process features, such as call stack collection, use the following configuration:

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

## Garbage Collector Mode

By default `dotnet monitor` (7.0+) will use Workstation GC mode, unless running in one of the official [docker images](./docker.md) where it will use Server GC mode by default but will fallback to Workstation mode if only one logical CPU core is available.

You can learn more about the different GC modes [here](https://learn.microsoft.com/aspnet/core/performance/memory?view=aspnetcore-6.0#workstation-gc-vs-server-gc), and how to configure the default GC mode [here](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector#workstation-vs-server).

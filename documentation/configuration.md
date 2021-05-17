# Configuration

`dotnet monitor` has extensive configuration to control various aspects of it's behavior. Ordinarily, you are not required to specify most of this configuration and only exists if you wish the change the default behavior in `dotnet monitor`.

## Configuration Sources

`dotnet monitor` can read and combine configuration from multiple sources. The configuration sources are listed below in the order in which they are read (Environment variables are highest precedence) :

- Command line parameters
- User settings path
  - On Windows, `%USERPROFILE%\.dotnet-monitor\settings.json`
  - On \*nix, `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`
  -  If `$XDG_CONFIG_HOME` isn't defined, we fall back to ` $HOME/.config/dotnet-monitor/settings.json`
- [Key-per-file](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/#key-per-file-configuration-provider) in the shared settings path
    - On Windows, `%ProgramData%\dotnet-monitor`
    - On \*nix, `etc/dotnet-monitor`

- Environment variables

### Translating configuration between providers

While the rest of this document will showcase configuration examples in a json format, the same configuration can be expressed via any of the other configuration sources. For example, the API Key configuration can be expressed via shown below:

```json
{
  "ApiAuthentication": {
    "ApiKeyHash": "CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683",
    "ApiKeyHashType": "SHA256"
  }
}
```

The same configuration can be expressed via environment variables using the `DotnetMonitor_` prefix and using `__`(double underscore) as the hierarchical separator

```bash
export DotnetMonitor_ApiAuthentication__ApiKeyHash="CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683"
export DotnetMonitor_ApiAuthentication__ApiKeyHashType="SHA256"
```

#### Kubernetes

When running in Kubernetes, you are able to specify the same configuration via Kubernetes secrets.

```bash
kubectl create secret generic apikey \
  --from-literal=ApiAuthentication__ApiKeyHash=$hash \
  --from-literal=ApiAuthentication__ApiKeyHashType=SHA256 \
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
    image: mcr.microsoft.com/dotnet/dotnet-monitor:5.0.0-preview.4
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

Alternatively, you can also configuration maps to specify configuration to the container at runtime.

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: my-configmap
data:
  Metrics__UpdateIntervalSeconds: "10"
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
    image: mcr.microsoft.com/dotnet/dotnet-monitor:5.0.0-preview.4
    volumeMounts:
      - name: config
        mountPath: /etc/dotnet-monitor
```

If using multiple configuration maps, secrets, or some combination of both, you need use a [projected volume](https://kubernetes.io/docs/concepts/storage/volumes/#projected) to map serveral volume sources into a single directory

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
    image: mcr.microsoft.com/dotnet/dotnet-monitor:5.0.0-preview.4
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

Once you've added the `$schema` property, you should started support for completions in your editor.

![completions](https://user-images.githubusercontent.com/4734691/115377729-bf2bb600-a184-11eb-9b8e-50f361c112f0.gif)

## View  merged configuration

`dotnet monitor` includes a diagnostic command that allows you to output the the resulting configuration after merging the configuration from all the various sources.

To view the merged configuration, run the following command:

```cmd
dotnet monitor config show
```
The output of command should resemble the following JSON object:

```json
{
  "urls": "https://localhost:52323",
  "Kestrel": ":NOT PRESENT:",
  "CorsConfiguration": ":NOT PRESENT:",
  "DiagnosticPort": {
    "ConnectionMode": "Connect",
    "EndpointName": null
  },
  "Metrics": {
    "AllowInsecureChannelForCustomMetrics": "True",
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
    "UpdateIntervalSeconds": "10"
  },
  "Storage": {
    "DumpTempFolder": "C:\\Users\\shirh\\AppData\\Local\\Temp\\"
  },
  "ApiAuthentication": {
    "ApiKeyHash": ":REDACTED:",
    "ApiKeyHashType": "SHA256"
  },
  "Egress": ":NOT PRESENT:"
}
```

## Diagnostic Port Configuration

`dotnet monitor` communicates via .NET processes through their diagnostic port. In the default configuration, .NET processes listen on a platform native transport (named pipes on Windows/Unix-domain sockets on \*nix) in a well-known location.

### Connection Mode

It is possible to change this behavior and have .NET processes connect to `dotnet monitor`. This allow you to monitor a process from start and collect traces for events such assembly load events that primarily occur at process startup and weren't possible to collect previously.

```json
  "DiagnosticPort": {
    "ConnectionMode": "Listen",
    "EndpointName": "\\\\.\\pipe\\dotnet-monitor-pipe"
  }
```

When `dotnet monitor` is in `Listen` mode, you have to configure .NET processes to connect to `dotnet monitor`. You can do so specifying the appropriate environment variable on your .NET process

```powershell
$env:DOTNET_DiagnosticPorts="dotnet-monitor-pipe,suspend"
```

#### Maximum connection

When operating in `Listen` mode, you can also specify the maximum number of incoming connections for `dotnet monitor` to accept via the following configuration:

```json
  "DiagnosticPort": {
    "MaxConnections": "10"
  }
```

## Kestrel Configuration

// TODO

## Storage Configuration

Unlike the other diagnostic artifacts (for example, traces), memory dumps aren't streamed back from the target process to `dotnet monitor`. Instead, they are written directly to disk by the runtime. After successful collection of a process dump, `dotnet monitor` will read the process dump directly from disk. In the default configuration, the directory that the runtime writes it's process dump to is the temp directory (`%TMP%` on Windows, `/tmp` on \*nix). It is possible to change to the ephemeral directory that these dump files get written to via the following configuration:

```json
{
  "Storage": {
    "DumpTempFolder": "/ephemeral-directory/"
  }
}
```

## Cross-Origin Resource Sharing (CORS) Configuration

// TODO

## Metrics Configuration

### Metrics Urls

In addition to the ordinary diagnostics urls that `dotnet monitor` binds to, it also binds to metric urls that only expose the `/metrics` endpoint. Unlike the other endpoints, the metrics urls do not require authentication. Unless you enable collection of custom providers that may contain sensitive business logic, it is generally considered safe to expose metrics endpoints. 

Metrics urls can configured via the command line:

```cmd
dotnet monitor collect --metricUrls http://*:52325/
```

Or configured via a configuration file:

```json
{
  "Metrics": {
    "Endpoints": "http://localhost:52325"
  }
}
```

### Customize collection interval and counts

In the default configuration, `dotnet monitor` requests that the connected runtime provides updated counter values every 10 seconds and will retain 3 data point for every collected metric. When using a collection tool like Prometheus, it is recommended that you set your scrape interval to `MetricCount` * `UpdateIntervalSeconds`. In the default configuration, we recommend you scrape `dotnet monitor` for metrics every 30 seconds.

You can customize the number of data points stored per metric and the frequency at which the runtime updates each metric via the following configuration:

```json
{
  "Metrics": {
    "MetricCount": 3,
    "UpdateIntervalSeconds": 10,
  }
}
```

### Custom Metrics

Additional metrics providers and counter names to return from this route can be specified via configuration. 

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

When `CounterNames` are not specified, all the counters associated with the `ProviderName` are collected.

#### Allow insecure channel for custom metrics

In the default configuration, enabling of custom metrics changes the default binding address from `http` to `https` for the metrics urls. To continue egressing custom metric via an endpoint with `http` scheme, you explicit opt-in to it. You can do so via the following configuration:

```json
{
  "Metrics": {
    "AllowInsecureChannelForCustomMetrics": true
  }
}
```

### Disable default providers

In addition to enabling custom providers, `dotnet monitor` also allows you to disable collection of the default providers. You can do so via the following configuration:

```json
{
  "Metrics": {
    "IncludeDefaultProviders": false
  }
}
```

## Egress Configuration

### Azure blob storage egress provider

| Name | Type | Description |
|---|---|---|
| type | string | Must be 'azureBlobStorage'.|
| accountUri | string | The URI of the Azure blob storage account.|
| containerName | string | The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.|
| blobPrefix | string | Optional path prefix for the artifacts to egress.|
| copyBufferSize | string | The buffer size to use when copying data from the original artifact to the blob stream.|
| accountKey | string | The account key used to access the Azure blob storage account.|
| sharedAccessSignature | string | The shared access signature (SAS) used to access the azure blob storage account.|
| accountKeyName | string | Name of the property in the Properties section that will contain the account key.|
| sharedAccessSignatureName | string | Name of the property in the Properties section that will contain the SAS token.|

### Example azureBlobStorage provider

```json
{
    "Egress": {
        "Providers": {
            "monitorBlob": {
                "type": "azureBlobStorage",
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

### Filesystem egress provider

| Name | Type | Description |
|---|---|---|
| type | string | Must be 'fileSystem'|
| directoryPath | string | The directory path to which the stream data will be egressed.|
| intermediateDirectoryPath | string | The directory path to which the stream data will initially be written, if specified; the file will then be moved/renamed to the directory specified in 'directoryPath'.|

### Example fileSystem provider

```json
{
    "Egress": {
        "Providers": {
            "monitorFile": {
                "type": "fileSystem",
                "directoryPath": "/artifacts",
                "intermediateDirectoryPath": "/intermediateArtifacts"
            }
        }
    }
}
```

# Configuration

`dotnet-monitor` has extensive configuration to control various aspects of it's behavior. Ordinarily, you are not required to specify most of this configuration and only exists if you wish the change the default behavior in `dotnet-monitor`.

## Configuration Sources

`dotnet-monitor` can read and combine configuration from multiple sources. The configuration sources are listed below in the order in which they are read (Environment variables are highest precedence) :

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

When running in Kubernetes, you are able to specify the same configuration via Kubernetes secrets (or configuration maps)

```bash
kubectl create secret generic apikey \
  --from-literal=ApiAuthentication__ApiKeyHash=$hash \
  --from-literal=ApiAuthentication__ApiKeyHashType=SHA256 \
  --dry-run=client -o yaml \
  | kubectl apply -f -
```

You can then use a Kubernetes volume mount to supply the secret to the container at runtime

```yaml 
spec:
  volumes:
  - name: apikey
    secret:
      secretName: apikey
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/dotnet-monitor:5.0.0-preview.4
    volumeMounts:
      - name: apikey
        mountPath: /etc/dotnet-monitor
```

## Configuration Schema

`dotnet-monitor`'s various configuration knobs have been documented via JSON schema. Using a modern editor like VS or VS Code that supports JSON Schema makes it trivial to author complex configuration objects with support for completions and rich descriptions via tooltips.

To get completion support in your editor, simply add the `$schema` property to the root JSON object as shown below:

```json
{
  "$schema": "https://aka.ms/dotnet-monitor-schema"
}
```

Once you've added the `$schema` property, you should started support for completions in your editor.

![completions](https://user-images.githubusercontent.com/4734691/115377729-bf2bb600-a184-11eb-9b8e-50f361c112f0.gif)

## View  merged configuration

`dotnet-monitor` includes a diagnostic command that allows to output the the resulting configuration after merging the configuration from all the various sources.

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

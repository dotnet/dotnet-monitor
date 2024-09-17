# Configuration Sources

`dotnet monitor` can read and combine configuration from multiple sources. The configuration sources are listed below in the order in which they are read (first is lowest precedence, last is highest precedence):

- Command line parameters
- Environment variables without `DOTNETMONITOR_` prefix e.g. `ASPNETCORE_Urls`
- User settings path
  - On Windows, `%USERPROFILE%\.dotnet-monitor\settings.json`
  - On \*nix, `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`
    - If `$XDG_CONFIG_HOME` isn't defined, we fall back to ` $HOME/.config/dotnet-monitor/settings.json`
- Shared settings path
  - On Windows, `%ProgramData%\dotnet-monitor\settings.json`
  - On \*nix, `/etc/dotnet-monitor/settings.json`
- [Key-per-file](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/#key-per-file-configuration-provider) in the shared settings path
    - On Windows, `%ProgramData%\dotnet-monitor`
    - On \*nix, `/etc/dotnet-monitor`
- Environment variables with `DOTNETMONITOR_` prefix e.g. `DOTNETMONITOR_Urls`
- Full path to a JSON settings file via the `--configuration-file-path` command line option. First available in .NET Monitor 6.3

## Translating configuration between providers

While the rest of this document will showcase configuration examples in a JSON format, the same configuration can be expressed via any of the other configuration sources. For example, the API Key configuration can be expressed via shown below:

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

### Kubernetes

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

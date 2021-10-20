# Authentication

Authenticated requests to `dotnet monitor` help protect sensitive diagnostic artifacts from unauthorized users and lower privileged processes. `dotnet monitor` can be configured to use either [Windows Authentication](#windows-authentication) or via an [API Key](#api-key-authentication). It is possible, although strongly not recommended, to [disable authentication](#disabling-authentication).

> **NOTE:** Authentication is not performed on requests to the metrics endpoint (by default, http://localhost:52325).

The recommended configuration for `dotnet monitor` is to use [API Key Authentication](#api-key-authentication) over a channel [secured with TLS](./enabling-ssl.md).

## Windows Authentication
We only recommend using Windows Authentication if you're running `dotnet monitor` as a local development tool on Windows; for all other environments using an [API Key](#api-key-authentication) is recommended.

Windows authentication doesn't require explicit configuration and is enabled automatically when running `dotnet monitor` on Windows. When available, `dotnet monitor` will authorize any user authenticated as the same user that started the `dotnet monitor` process. It is not possible to disable Windows authentication.

> **NOTE:** Windows authentication will not be attempted if you are running `dotnet monitor` as an Administrator

## API Key Authentication
An API Key is the recommended authentication mechanism for `dotnet monitor`. API Keys are referred to as `MonitorApiKey` in configuration and source code but we will shorten the term to "API key" in this document. To enable it, you will need to generate a secret token, update the configuration of `dotnet monitor`, and then specify the secret token in the `Authorization` header on all requests to `dotnet monitor`.

> **NOTE:** API Key Authentication should only be used when [TLS is enabled](#) to protect the key while in transit. `dotnet monitor` will emit a warning if authentication is enabled over an insecure transport medium.

### Generating an API Key

The API Key you use to secure `dotnet monitor` is a secret JWT token, cryptographically signed by a public/private key algorithm. You can generate a key either using `dotnet monitor` or via your shell. To generate an API Key with `dotnet monitor`, simply invoke the `generatekey` command:

```powershell
dotnet monitor generatekey
```

The output from this command will display the API key (a bearer JWT token) formatted as an `Authorization` header along with its corresponding configuration for `dotnet monitor`. You will need to store the `Subject` and `PublicKey` in the configuration for `dotnet monitor` and use the `Authorization` header value when making requests to the `dotnet monitor` HTTPS endpoint.

```yaml
Generated ApiKey for dotnet-monitor; use the following header for authorization:

Authorization: Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU

Settings in Text format:
Subject: ae5473b6-8dad-498d-b915-ffffffffffff
Public Key: eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19
```
>**Note:** While all values provided in this document are the correct length and format, the raw values have been edited to prevent this public example being used as a dotnet-monitor configuration.

The `generatekey` command supports 1 parameter `--output`/`-o` to specify the configuration format. By default, `dotnet monitor generatekey` will use the `--output json` format. Currently, the values in the list below are supported values for `--output`.

- `Json` output format will provide a json blob in the correct format to merge with an `appsettings.json` file to specify configuration via json file.
- `Text` output format write the individual parameters in an easily human-readable format.
- `Cmd` output format in environment variables for a `cmd.exe` prompt.
- `PowerShell` output format in environment variables for a `powershell` or `pwsh` prompt.
- `Shell` output format in environment variables for a `bash` shell or another linux shell prompt.

### Configuring dotnet-monitor to use an API Key

Using the `generatekey` command does not automatically update the configuration of `dotnet monitor` to use the new key. You can update the configuration of `dotnet monitor` via settings file, environment variable or Kubernetes secrets.

If you're running on Windows, you can save these settings to `%USERPROFILE%\.dotnet-monitor\settings.json`. If you're on other operating systems, you can save this file to `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`.

> **NOTE:** If `$XDG_CONFIG_HOME` isn't defined, `dotnet monitor` will fall back to looking at `$HOME/.config/dotnet-monitor/settings.json`

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

Alternatively, you can use environment variables to specify the configuration. Use `dotnet monitor generatekey --output shell` to get the format below.

```sh
export Authentication__MonitorApiKey__Subject="ae5473b6-8dad-498d-b915-ffffffffffff"
export Authentication__MonitorApiKey__PublicKey="eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"
```

> **NOTE:** When you use environment variables to configure the API Key, you must restart `dotnet monitor` for the changes to take effect.

### Configuring an API Key in a Kubernetes Cluster
If you're running in Kubernetes, we recommend creating secrets and mounting them into the `dotnet monitor` sidecar container via a deployment manifest. You can use this `kubectl` command to either create or rotate the API Key.

```sh
kubectl create secret generic apikey \
  --from-literal=Authentication__MonitorApiKey__Subject='ae5473b6-8dad-498d-b915-ffffffffffff' \
  --from-literal=Authentication__MonitorApiKey__PublicKey='eyffffffffffff...19' \
  --dry-run=client -o yaml \
  | kubectl apply -f -
```

Mount the secret as a file into `/etc/dotnet-monitor` in the deployment manifest for your application (abbreviated for clarity). `dotnet monitor` only supports automatic key rotations for secrets mounted as volumes. For other kinds of secrets, the `dotnet monitor` process must be restarted for the new key to take effect. 

```yaml
spec:
  volumes:
  - name: apikey
    secret:
      secretName: apikey
  containers:
  - name: dotnetmonitoragent
    image: mcr.microsoft.com/dotnet/dotnet-monitor:6.0.0-preview.8
    volumeMounts:
      - name: apikey
        mountPath: /etc/dotnet-monitor

```

> **NOTE:** For a complete example of running dotnet-monitor in Kubernetes, see [Running in a Kubernetes Cluster](getting-started.md#running-in-a-kubernetes-cluster) in the Getting Started guide.

### Generate API Keys manually

API keys for `dotnet monitor` are standard JSON Web Tokens or JWTs and can be generated without the `generatekey` command on `dotnet monitor`. For more details see [Api Key Format](api-key-format.md).

## Authenticating requests
When using Windows Authentication, your browser will automatically handle the Windows authentication challenge. If you are using an API Key, you must specify it via the `Authorization` header.

```sh
curl -H "Authorization: Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU" https://localhost:52323/processes
```

If using PowerShell, you can use `Invoke-WebRequest` but it does not accept the same parameters.

```powershell
 (Invoke-WebRequest -Uri https://localhost:52323/processes -Headers @{ 'Authorization' = 'Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU' }).Content | ConvertFrom-Json
```

To use Windows authentication with PowerShell, you can specify the `-UseDefaultCredentials` flag for `Invoke-WebRequest` or `--negotiate` for `curl.exe`
```powershell
curl.exe --negotiate https://localhost:52323/processes -u $(whoami)
```
```powershell
(Invoke-WebRequest -Uri https://localhost:52323/processes -UseDefaultCredentials).Content | ConvertFrom-Json
```

## Disabling Authentication

Disabling authentication could enable lower privileged processes to exfiltrate sensitive information, such as the full contents of memory, from any .NET application running within the same boundary. You should only disable authentication when you have evaluated and mitigated the security implications of running `dotnet monitor` unauthenticated.

Authentication can be turned off by specifying the `--no-auth` option to `dotnet monitor` at startup:
```powershell
dotnet monitor collect --no-auth
```

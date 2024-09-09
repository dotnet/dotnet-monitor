# Configuring API Key Authentication

The API Key you use to secure `dotnet monitor` is a secret Json Web Token (JWT), cryptographically signed by a public/private key algorithm. You can **[Recommended]** use the integrated command to generate a key or you can generate the key yourself following the [format, documented here](./api-key-format.md). This guide will use the integrated command.

## 1. Using the integrated `generatekey` command

Run the command:

```bash
> dotnet monitor generatekey
```

The output from this command will display the API key (a bearer JWT token) formatted as an `Authorization` header along with its corresponding configuration for `dotnet monitor`. You will need to store the `Subject` and `PublicKey` in the configuration for `dotnet monitor` and use the `Authorization` header value when making requests to the `dotnet monitor` HTTPS endpoint.

> [!NOTE]
> The `Authorization` header value is the string `Bearer` (representing the type) + the JWT, separated by a space. In some applications (like Postman), you fill in the `Authorization` header type in a separate field from the JWT.

```yaml
Tell us about your experience with dotnet monitor: https://aka.ms/dotnet-monitor-survey

Generated ApiKey for dotnet-monitor; use the following header for authorization:

Authorization: Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU

Settings in Json format:
{
  "Authentication": {
    "MonitorApiKey": {
      "Subject": "ae5473b6-8dad-498d-b915-ffffffffffff",
      "PublicKey": "eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"
    }
  }
}
```

> [!NOTE]
> The actual values provided in this document will never work as valid configuration. All values provided in this document are the correct length and format, but the raw values have been edited to prevent this public example being used to configure authentication for a dotnet-monitor installation.

The `generatekey` command supports 1 parameter `--output`/`-o` to specify the configuration format. By default, `dotnet monitor generatekey` will use the `--output json` format. Currently, the values in the list below are supported values for `--output`.

- `Json` output format will provide a json blob in the correct format to merge with a `settings.json` file that configures `dotnet-monitor`. See [Configuration Sources](./configuration/configuration-sources.md) for where to find or create a `settings.json` file.
- `Text` output format writes the individual parameters in an easily human-readable format.
- `Cmd` output format in environment variables for a `cmd.exe` prompt.
- `PowerShell` output format in environment variables for a `powershell` or `pwsh` prompt.
- `Shell` output format in environment variables for a `bash` shell or another linux shell prompt.
- `MachineJson` output a single json blob designed to be easy to parse by other tools. The entire STDOUT from `dotnet-monitor` will be a parsable json object.

## 2. Configure `dotnet-monitor`

Once you have the key material from [step 1](#1-using-the-integrated-generatekey-command), you then must provide that configuration `dotnet-monitor`. There are several ways to [configure dotnet-monitor](./configuration/README.md) and the easiest method usually depends on your platform.

### A local dev box (Windows, OSX or Linux)

The easiest way to configure `dotnet-monitor` on a local dev box is by using the `settings.json` file loaded from the following directory (depending on platform):

- Windows: `%USERPROFILE%\.dotnet-monitor\settings.json`
- Linux (and \*nix like):
  - If `XDG_CONFIG_HOME` is defined: `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`
  - Otherwise: `$HOME/.config/dotnet-monitor/settings.json`

> [!NOTE]
> You probably need to create the directory and `settings.json` file within.

  or

- Use the `--configuration-file-path` switch to specify the location of the configuration file

Run the command `dotnet monitor generatekey --output json` and copy the json blob into `settings.json`. A typical settings file might look like this:

```json
{
  "$schema": "https://aka.ms/dotnet-monitor-schema",
  "Authentication": {
    "MonitorApiKey": {
      "Subject": "ae5473b6-8dad-498d-b915-ffffffffffff",
      "PublicKey": "eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"
    }
  }
}
```

> [!NOTE]
> The example above is not valid configuration, use the provided command to get a unique authentication key.

### Docker

The easiest way to configure `docker` is to pass environment variables for the required configuration. First start by running `dotnet monitor generatekey --output Shell`.

```bash
> docker run --rm --entrypoint dotnet-monitor mcr.microsoft.com/dotnet/monitor generatekey --output Text
```

> [!NOTE]
> You'll need 3 parameters from the above execution. Grab the value in `Subject` and `PublicKey` and fill in `<Subject>` and `<PublicKey>` in the command below; save the value in `Authorization` for [step 3](#3-using-an-api-key-to-access-the-http-api).

```bash
> docker run --rm -p 127.0.0.1:52323:52323/tcp --entrypoint dotnet-monitor --env DOTNETMONITOR_Authentication__MonitorApiKey__Subject=<Subject> --env DOTNETMONITOR_Authentication__MonitorApiKey__PublicKey=<PublicKey> mcr.microsoft.com/dotnet/monitor collect --urls http://+:52323 --metricUrls http://+:52325
```

> [!WARNING]
> This command disables TLS, and should only be used for testing.

### Configuring an API Key in a Kubernetes Cluster

If you're running in Kubernetes, we recommend creating secrets and mounting them into the `dotnet monitor` sidecar container via a [deployment manifest](kubernetes.md). You can use this `kubectl` command to either create or rotate the API Key.

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
    image: mcr.microsoft.com/dotnet/monitor:6
    volumeMounts:
      - name: apikey
        mountPath: /etc/dotnet-monitor

```

> [!NOTE]
> For a complete example of running dotnet-monitor in Kubernetes, see [Running in a Kubernetes Cluster](./kubernetes.md)

## 3. Using an API Key to access the HTTP API

When using API Key authentication, you must fill in the `Authorization` header in `Bearer` mode with the parameter given in `Authorization` from running the `generatekey` command.

A valid authorization header value will look like this:

```text
Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU
```

## From CURL

If using Curl, you can use the `-H` parameter to specify the authorization header. The expected format for `-H` is `<Header Name>: <Header Value>`. Here is an example `curl` command:

```sh
curl -H "Authorization: Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU" https://localhost:52323/processes
```

## From PowerShell

If using PowerShell, you can use `Invoke-WebRequest` but it does not accept the same parameters. Specify the Authorization header in a Dictionary provided to the `-Headers` parameter like this:

```powershell
 (Invoke-WebRequest -Uri https://localhost:52323/processes -Headers @{ 'Authorization' = 'Bearer eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU' }).Content | ConvertFrom-Json
```

Authentication
================

Authenticating requests to `dotnet-monitor` helps protect sensitive diagnostic artifacts from unauthorized users and lower privileged processes. `dotnet-monitor` can be configured to use either [Windows Authentication](#windows-authentication) or via an [API Token](#api-token-authentication). It is possible, although strongly not recommended, to [disable authentication](#disabling-authentication).

## Windows Authentication
We only recommend using Windows Authentication if you're running `dotnet-monitor` as a local development tool on Windows, for all other environments using an [API Token](#api-token-authentication) is recommended.

Windows authentication doesn't require explicit configuration and is enabled automatically when running `dotnet-monitor` on Windows. When available, dotnet-monitor will authorize any user authenticated as the same user that started the `dotnet-monitor` process. It is not possible to disable Windows authentication.

> **FACT CHECK:** Windows Authentication cannot be disabled when running on Windows. See: [DiagnosticsMonitorCommandHandler](https://github.com/dotnet/dotnet-monitor/blob/69542c44b7d70a83901b93a73d0ea09b90870bcd/src/Tools/dotnet-monitor/DiagnosticsMonitorCommandHandler.cs#L136)

## API Token Authentication
An API Token is the recommended authentication mechanism for `dotnet-monitor`. To enable it, you will need to generate a secret key, update the configuration of `dotnet monitor`, and then specify the API Token in the Authorization header on all requests to `dotnet-monitor`.

> **NOTE:** API Token Authentication should only be used when [TLS is enabled](#) to protect the token while in transit.

### Generating an API Token

The API Token you use to secure `dotnet-monitor` should be a 32-bit cryptographically random secret. You can generate a token either using `dotnet-monitor` or via your shell. To generate an API Token with `dotnet-monitor`, simply invoke the `generatekey` command:

```powershell
dotnet-monitor generatekey
```

The output from this command will display the API Token formatted as an authentication header along with its hash and associated hashing algorithm. You will need to store the `ApiKeyHash` and `ApiKeyHashType` in the configuration for `dotnet-monitor` and use the authorication header value when making requests to the `dotnet-monitor` HTTPS endpoint.

```
Authorization: MonitorApiKey Uf6Yq8ZGkcn+ltq2QLHxuXpKA6/Q5VH5Mb5aSIJBxRc=
ApiKeyHash: CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683
ApiKeyHashType: SHA256
```

> NOTE: The generated API token should be secured at rest. We recommend using a tool such as a password manager to save it.

### Configuring dotnet-monitor to use an API Token

Using the `generatekey` command does not automatically update the configuration of `dotnet-monitor` to use the new key. You can update the configuration of `dotnet-monitor` via settings file, environemt variable of kubernetes secrets.

If you're running on Windows, you can save these settings to `%USERPROFILE%\.dotnet-monitor\settings.json`. If you're on other operating systems, you can save this file to `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`.

> **NOTE:** If `$XDG_CONFIG_HOME` isn't defined, dotnet-monitor will fall back to looking at `$HOME/.config/dotnet-monitor/settings.json`

```json
{
  "ApiAuthentication": {
    "ApiKeyHash": "<HASHED-TOKEN>",
    "ApiKeyHashType": "SHA256"
  }
}
```

Alternatively, you can use environment variables to specify the configuration.

```sh
DotnetMonitor_ApiAuthentication__ApiKeyHash="<HASHED-TOKEN>"
DotnetMonitor_ApiAuthentication__ApiKeyHashType="SHA256"
```

> **NOTE:** When you use environment variables to configure the API Key hash, you must restart `dotnet-monitor` for the changes to take effect.

### Configuring an API Token in a Kubernetes Cluster
If you're running in Kubernetes, we recommend creating secrets and mounting them into container via a deployment manifest.

```sh
kubectl create secret generic apikey \
	--from-literal=ApiAuthentication__ApiKeyHash=$hash \
	--dry-run=client -o yaml \
	| kubectl apply -f -
```

Mount the secret as a file into `/etc/dotnet-monitor` in the deployment manifest for your application (abbreviated for clarity).

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

> **NOTE:** For a complete example of running dotnet-monitor in Kubernetes, see [Running in a Kubernetes Cluster](getting-started.md#running-in-a-kubernetes-cluster) in the Getting Started guide.

### Generate an API Token with PowerShell
```powershell
$rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$secret = [byte[]]::new(32)
$rng.GetBytes($secret)
$API_TOKEN = [Convert]::ToBase64String($secret)
"Authorization: MonitorApiKey $API_TOKEN"

$secretStream = [System.IO.MemoryStream]::new($secret)
$HASHED_TOKEN = (Get-FileHash -Algorithm SHA256 -InputStream $secretStream).Hash
$rng.Dispose()

Write-Output "ApiKeyHash: $HASHED_TOKEN"
Write-Output "ApiKeyHashType: SHA256"
```

### Generate an API Token with a Linux shell

```sh
API_TOKEN=`openssl rand -base64 32`
echo "Authorization: MonitorApiKey $API_TOKEN"

HASHED_TOKEN=`$API_TOKEN | base64 -d | sha256sum`
echo "ApiKeyHash: $HASHED_TOKEN"
echo "ApiKeyHashType: SHA256"
```

## Authenticating requests
When using Windows Authentication, your browser with automatically handle the Windows authentication challenge. If you are using an API Token, you must specify it via the authentication header.

```sh
curl -H "Authorization: MonitorApiKey HdFb8OPkE0Dc5hpu0kAxA7hhNguAah9SNUFftlP2Dk0=" https://localhost:52323/processes
```



## Disabling Authentication

Disabling authentication could enable lower privileged processes to exfiltrate sensitive information, such as the full contents of memory, from any .NET Core application running within the same boundary. You should only disable authentication when you have evaluated and mitigated the security implications of running `dotnet-monitor` unauthenticated.

Authentication can be turned off by specifying the `--no-auth` option to `dotnet-monitor` at startup:
```powershell
dotnet-monitor collect --no-auth
```


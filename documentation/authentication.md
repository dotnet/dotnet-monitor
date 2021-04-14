# Authentication

Authenticated requests to `dotnet-monitor` help protect sensitive diagnostic artifacts from unauthorized users and lower privileged processes. `dotnet-monitor` can be configured to use either [Windows Authentication](#windows-authentication) or via an [API Key](#api-key-authentication). It is possible, although strongly not recommended, to [disable authentication](#disabling-authentication).

> **NOTE:** Authentication is not performed on requests to the metrics endpoint (by default, http://localhost:52325).

The recommended configuration for `dotnet-monitor` is to use [API Key Authentication](#api-key-authentication) over a channel [secured with TLS](./enabling-ssl.md).

## Windows Authentication
We only recommend using Windows Authentication if you're running `dotnet-monitor` as a local development tool on Windows; for all other environments using an [API Key](#api-key-authentication) is recommended.

Windows authentication doesn't require explicit configuration and is enabled automatically when running `dotnet-monitor` on Windows. When available, dotnet-monitor will authorize any user authenticated as the same user that started the `dotnet-monitor` process. It is not possible to disable Windows authentication.

> **NOTE:** Windows authentication will not be attempted if you are running `dotnet-monitor` as an Administrator

## API Key Authentication
An API Key is the recommended authentication mechanism for `dotnet-monitor`. To enable it, you will need to generate a secret key, update the configuration of `dotnet monitor`, and then specify the API Key in the Authorization header on all requests to `dotnet-monitor`.

> **NOTE:** API Key Authentication should only be used when [TLS is enabled](#) to protect the key while in transit. `dotnet-monitor` will emit a warning if authentication is enabled over an insecure transport medium.

### Generating an API Key

The API Key you use to secure `dotnet-monitor` should be a 32-byte cryptographically random secret. You can generate a key either using `dotnet-monitor` or via your shell. To generate an API Key with `dotnet-monitor`, simply invoke the `generatekey` command:

```powershell
dotnet-monitor generatekey
```

The output from this command will display the API Key formatted as an authentication header along with its hash and associated hashing algorithm. You will need to store the `ApiKeyHash` and `ApiKeyHashType` in the configuration for `dotnet-monitor` and use the authorication header value when making requests to the `dotnet-monitor` HTTPS endpoint.

```
Authorization: MonitorApiKey Uf6Yq8ZGkcn+ltq2QLHxuXpKA6/Q5VH5Mb5aSIJBxRc=
ApiKeyHash: CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683
ApiKeyHashType: SHA256
```

The API Key is hashed using the SHA256 algorithm when using the `generatekey` command, but other [secure hash implementations](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.hashalgorithm.create?view=net-5.0#System_Security_Cryptography_HashAlgorithm_Create_System_String) (such as SHA384 and SHA512) are also supported; due to collision problems the SHA1 and MD5 algorithms are not permitted.

> NOTE: The generated API Key should be secured at rest. We recommend using a tool such as a password manager to save it.

### Configuring dotnet-monitor to use an API Key

Using the `generatekey` command does not automatically update the configuration of `dotnet-monitor` to use the new key. You can update the configuration of `dotnet-monitor` via settings file, environemt variable of kubernetes secrets.

If you're running on Windows, you can save these settings to `%USERPROFILE%\.dotnet-monitor\settings.json`. If you're on other operating systems, you can save this file to `$XDG_CONFIG_HOME/dotnet-monitor/settings.json`.

> **NOTE:** If `$XDG_CONFIG_HOME` isn't defined, dotnet-monitor will fall back to looking at `$HOME/.config/dotnet-monitor/settings.json`

```json
{
  "ApiAuthentication": {
    "ApiKeyHash": "CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683",
    "ApiKeyHashType": "SHA256"
  }
}
```

Alternatively, you can use environment variables to specify the configuration.

```sh
DotnetMonitor_ApiAuthentication__ApiKeyHash="CB233C3BE9F650146CFCA81D7AA608E3A3865D7313016DFA02DAF82A2505C683"
DotnetMonitor_ApiAuthentication__ApiKeyHashType="SHA256"
```

> **NOTE:** When you use environment variables to configure the API Key hash, you must restart `dotnet-monitor` for the changes to take effect.

### Configuring an API Key in a Kubernetes Cluster
If you're running in Kubernetes, we recommend creating secrets and mounting them into container via a deployment manifest. You can use this `kubectl` command to either create or rotate the API Key.

```sh
kubectl create secret generic apikey \
	--from-literal=ApiAuthentication__ApiKeyHash=$hash \
	--dry-run=client -o yaml \
	| kubectl apply -f -
```

Mount the secret as a file into `/etc/dotnet-monitor` in the deployment manifest for your application (abbreviated for clarity). `dotnet-monitor` only supports automatic key rotations for secrets mounted as volumes. For other kinds of secrets, the `dotnet-monitor` process must be restarted for the new key to take effect. 

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

### Generate an API Key with PowerShell
```powershell
$rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$secret = [byte[]]::new(32)
$rng.GetBytes($secret)
$API_KEY = [Convert]::ToBase64String($secret)
"Authorization: MonitorApiKey $API_KEY"

$secretStream = [System.IO.MemoryStream]::new($secret)
$HASHED_KEY = (Get-FileHash -Algorithm SHA256 -InputStream $secretStream).Hash
$rng.Dispose()

Write-Output "ApiKeyHash: $HASHED_KEY"
Write-Output "ApiKeyHashType: SHA256"
```

### Generate an API Key with a Linux shell

```sh
API_KEY=`openssl rand -base64 32`
echo "Authorization: MonitorApiKey $API_KEY"

HASHED_KEY=`$API_KEY | base64 -d | sha256sum`
echo "ApiKeyHash: $HASHED_KEY"
echo "ApiKeyHashType: SHA256"
```

## Authenticating requests
When using Windows Authentication, your browser will automatically handle the Windows authentication challenge. If you are using an API Key, you must specify it via the authentication header.

```sh
curl -H "Authorization: MonitorApiKey Uf6Yq8ZGkcn+ltq2QLHxuXpKA6/Q5VH5Mb5aSIJBxRc=" https://localhost:52323/processes
```

If using PowerShell, be sure to use `curl.exe`, as `curl` is an alias for `Invoke-WebRequest` that does not accept the same parameters.

```powershell
curl.exe -H "Authorization: MonitorApiKey Uf6Yq8ZGkcn+ltq2QLHxuXpKA6/Q5VH5Mb5aSIJBxRc=" https://localhost:52323/processes
```



## Disabling Authentication

Disabling authentication could enable lower privileged processes to exfiltrate sensitive information, such as the full contents of memory, from any .NET application running within the same boundary. You should only disable authentication when you have evaluated and mitigated the security implications of running `dotnet-monitor` unauthenticated.

Authentication can be turned off by specifying the `--no-auth` option to `dotnet-monitor` at startup:
```powershell
dotnet-monitor collect --no-auth
```
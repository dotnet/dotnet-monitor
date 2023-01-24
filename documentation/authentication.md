
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fauthentication)

# Authentication

Authenticated requests to `dotnet monitor` help protect sensitive diagnostic artifacts from unauthorized users and lower privileged processes. `dotnet monitor` can be configured to use either [Windows Authentication](#windows-authentication) or via an [API Key](#api-key-authentication). It is possible, although strongly not recommended, to [disable authentication](#disabling-authentication).

> **Note**: Authentication is not performed on requests to the metrics endpoint (by default, http://localhost:52325).

The recommended configuration for `dotnet monitor` is to use [API Key Authentication](#api-key-authentication) over a channel secured with TLS.

## Windows Authentication

We only recommend using Windows Authentication if you're running `dotnet monitor` as a local development tool on Windows; for all other environments using an [API Key](#api-key-authentication) is recommended.

Windows authentication doesn't require explicit configuration and is enabled automatically when running `dotnet monitor` on Windows. When available, `dotnet monitor` will authorize any user authenticated as the same user that started the `dotnet monitor` process. It is not possible to disable Windows authentication.

> **Note**: Windows authentication will not be attempted if you are running `dotnet monitor` as an Administrator

## API Key Authentication

An API Key is the recommended authentication mechanism for `dotnet monitor`. API Keys are referred to as `MonitorApiKey` in configuration and source code but we will shorten the term to "API key" in this document. To enable API key authentication:

- You will need to generate a secret token, update the configuration of `dotnet monitor`, and then specify the secret token in the `Authorization` header on all requests to `dotnet monitor`. To configure API Key authentication using the integrated `generatekey` command see: [API Key Setup](./api-key-setup.md).

  or

- Use the `--temp-api-key` command line option to generate a one-time API key for that instantiation of dotnet-monitor. The API key will be reported back as part of log output during the startup of the process.

> **Note**: API Key Authentication should only be used when TLS is enabled to protect the key while in transit. `dotnet monitor` will emit a warning if authentication is enabled over an insecure transport medium.

## Authenticating requests

### Windows authentication

- When using a web browser, it will automatically handle the Windows authentication challenge. 

- To use Windows authentication with PowerShell, you can specify the `-UseDefaultCredentials` flag for `Invoke-WebRequest` or `--negotiate` for `curl.exe`
```powershell
curl.exe --negotiate https://localhost:52323/processes -u $(whoami)
```
```powershell
(Invoke-WebRequest -Uri https://localhost:52323/processes -UseDefaultCredentials).Content | ConvertFrom-Json
```

### API Authentication

- If you are using an API Key, you must specify it via the `Authorization` header.

```sh
curl -H "Authorization: Bearer <API Key from GenerateKey command>" https://localhost:52323/processes
```

- If using PowerShell, you can use `Invoke-WebRequest` but it does not accept the same parameters.

```powershell
 (Invoke-WebRequest -Uri https://localhost:52323/processes -Headers @{ 'Authorization' = 'Bearer <API Key from GenerateKey command>' }).Content | ConvertFrom-Json
```



## Disabling Authentication

Disabling authentication could enable lower privileged processes to exfiltrate sensitive information, such as the full contents of memory, from any .NET application running within the same boundary. You should only disable authentication when you have evaluated and mitigated the security implications of running `dotnet monitor` unauthenticated.

Authentication can be turned off by specifying the `--no-auth` option to `dotnet monitor` at startup:
```powershell
dotnet monitor collect --no-auth
```

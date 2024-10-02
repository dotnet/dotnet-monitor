> [!IMPORTANT]
> This document is currently a work in progress.

# Security Considerations

## Azure Active Directory Authentication (Entra ID)

### Configuration

Prior to 9.0 RC 2, the `TenantId` [configuration option](./configuration/azure-ad-authentication-configuration.md#configuration-options) is optional. However when configuring `dotnet-monitor`, `TenantId` should always be explicitly set to [your tenant's id](https://learn.microsoft.com/entra/fundamentals/how-to-find-tenant) and not a pseudo tenant (e.g. `common` or `organizations`).

### Token Validation

When using Azure Active Directory for authentication, the following noteworthy properties on a token will be validated:
- `aud` will be validated using the `AppIdUri` configuration option.
- `iss` will be validated using the `TenantId` configuration option.
- `roles` will be validated to make sure that the `RequiredRole` configuration option is present.
- Properties relating to the lifetime of the token will be validated.

## Storing Configuration Secrets

It is **not recommended** to store secrets such as blob storage keys in JSON configuration. The following are recommendations for how to more securely store your configuration secrets for different platforms:

### Locally

When running locally, a preferred alternative is to specify secrets via environment variables when launching `dotnet monitor`. The following is an example using PowerShell, setting the value of `AzureBlobStorage__monitorBlob__AccountKey` prior to beginning collection:

```pwsh
$env:Egress__AzureBlobStorage__monitorBlob__AccountKey = "accountKey"; dotnet-monitor collect
```

### Kubernetes

For Kubernetes, a preferred alternative is to mount your secrets in the file system with restricted access - for more information and an example of how to do this, view the [Kubernetes documentation](https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/#create-a-pod-that-has-access-to-the-secret-data-through-a-volume). For additional information on how secrets work in Kubernetes, view the [documentation](https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/#create-a-secret).

## Item 3

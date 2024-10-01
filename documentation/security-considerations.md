> [!IMPORTANT]
> This document is currently a work in progress.

# Security Considerations

## Item 1

## Storing Configuration Secrets

It is **not recommended** to store secrets such as blob storage keys in JSON configuration. The following are recommendations for how to more securely store your configuration secrets for different platforms:

### Locally

When running locally, a preferred alternative is to specify secrets via environment variables when launching `dotnet monitor`. The following is an example using PowerShell, setting the value of `AzureBlobStorage__monitorBlob__AccountKey` prior to beginning collection:

```pwsh
$env:Egress__AzureBlobStorage__monitorBlob__AccountKey = "accountKey"; dotnet-monitor collect
```

### Kubernetes

For Kubernetes, a preferred alternative is to mount your secrets in the file system with restricted access - for more information and an example of how to do this, view the [Kubernetes documentation](https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/#set-posix-permissions-for-secret-keys). For additional information on how secrets work in Kubernetes, view the [documentation](https://kubernetes.io/docs/tasks/inject-data-application/distribute-credentials-secure/#create-a-secret).

## Item 3

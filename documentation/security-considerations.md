> [!IMPORTANT]
> This document is currently a work in progress.

# Security Considerations

## Azure Active Directory Authentication (Entra ID)

### Configuration

Prior to 9.0 RC 2, the `TenantId` [configuration option](./configuration/azure-ad-authentication-configuration.md#configuration-options) is optional. However when configuring `dotnet-monitor`, `TenantId` should always be explicitly set to [your tenant's id](https://learn.microsoft.com/entra/fundamentals/how-to-find-tenant) and not a pseudo tenant.

### Token Validation

When using Azure Active Directory for authentication, the following noteworthy properties on a token will be validated:
- `aud` will be validated using the `AppIdUri` configuration option.
- `iss` will be validated using the `TenantId` configuration option.
- `roles` will be validated to make sure that the `RequiredRole` configuration option is present.
- Properties relating to the lifetime of the token will be validated.

## Item 2

## Item 3

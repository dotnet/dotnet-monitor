# Breaking Changes in 9.0

If you are migrating your usage to `dotnet monitor` 9.0, the following changes might affect you. Changes are grouped together by areas within the tool.

## Changes

| Area | Title | Introduced |
|--|--|--|
| Acquisition | [.NET tool installation requires .NET 9 SDK](#acquisition-net-tool-installation-requires-net-9-sdk) | Preview 1 |
| Configuration | [`TenantId` is now required when configuring Azure Active Directory authentication](#configuration-azure-active-directory-authentication) | RC 2 |

## Details

### Acquisition: .NET tool installation requires .NET 9 SDK

Acquisition of the tool through `dotnet tool` command requires the use of the .NET 9 SDK as the tool requires .NET 9 run. This has no impact on what applications can be monitored nor any impact on the `dotnet/monitor` Docker image.

### Configuration: Azure Active Directory Authentication

When using Azure Active Directory (Entra ID) for authentication, setting the `TenantId` option is now **required**. See [Azure Active Directory Authentication Configuration](../../configuration/azure-ad-authentication-configuration.md#configuration-options) for more details.

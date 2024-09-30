# Breaking Changes in 9.0

If you are migrating your usage to `dotnet monitor` 9.0, the following changes might affect you. Changes are grouped together by areas within the tool.

## Changes

| Area | Title | Introduced |
|--|--|--|
| Configuration | [`TenantId` is now required when configuring Azure Active Directory authentication](#configuration-azure-active-directory-authentication) | RC 2 |

## Details

### Configuration: Azure Active Directory Authentication

When using Azure Active Directory (Entra Id) for authentication, setting the `TenantId` is now **required**.

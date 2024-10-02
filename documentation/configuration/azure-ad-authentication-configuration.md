# Azure Active Directory Authentication Configuration

First Available: 7.1

Azure Active Directory authentication must be configured before `dotnet monitor` starts, it does not support being configured or changed at runtime.

> [!IMPORTANT]
> See [Security Considerations](../security-considerations.md#azure-active-directory-authentication-entra-id) for important information regarding configuring Azure Activity Directory authentication.

## Configuration Options

> [!NOTE]
> Starting in 9.0 RC 2, the `TenantId` option is now **required**.

| Name | Type | Required | Description |
|---|---|---|---|
| ClientId | string | true | The unique application (client) id assigned to the app registration in Azure Active Directory. |
| RequiredRole | string | true | The role required to be able to authenticate. |
| AppIdUri | uri | false | The App ID URI of the app registration. Defaults to `api://{ClientId}` if not specified. |
| Instance | uri | false | Specifies the Azure cloud instance users are signing in from. Can be either the Azure public cloud or one of the national clouds. Defaults to the Azure public cloud (`https://login.microsoftonline.com`). |
| TenantId (9.0 RC 2+) | string | true | The tenant id of the Azure Active Directory tenant. |
| TenantId | string | false | The tenant id of the Azure Active Directory tenant. Defaults to `organizations`. |

A minimal configuration requires setting just the `TenantId`, `ClientId`, and `RequiredRole`.

### Example Configuration

<details>
  <summary>JSON</summary>

  ```json
  {
      "Authentication": {
          "AzureAd": {
            "TenantId": "6f565143-0d4c-4e44-a35b-974e4b2f78a0",
            "ClientId": "5eaf6ccc-e8c1-47c6-a68c-a6453172c655",
            "RequiredRole": "Application.Access"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Authentication__AzureAd__TenantId: "6f565143-0d4c-4e44-a35b-974e4b2f78a0"
  Authentication__AzureAd__ClientId: "5eaf6ccc-e8c1-47c6-a68c-a6453172c655"
  Authentication__AzureAd__RequiredRole: "Application.Access"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Authentication__AzureAd__TenantId
    value: "6f565143-0d4c-4e44-a35b-974e4b2f78a0"
  - name: DotnetMonitor_Authentication__AzureAd__ClientId
    value: "5eaf6ccc-e8c1-47c6-a68c-a6453172c655"
  - name: DotnetMonitor_Authentication__AzureAd__RequiredRole
    value: "Application.Access"
  ```
</details>

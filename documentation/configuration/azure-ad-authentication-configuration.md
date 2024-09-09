# Azure Active Directory Authentication Configuration

First Available: 7.1

Azure Active Directory authentication must be configured before `dotnet monitor` starts, it does not support being configured or changed at runtime.

## Configuration Options

| Name | Type | Required | Description |
|---|---|---|---|
| ClientId | string | true | The unique application (client) id assigned to the app registration in Azure Active Directory. |
| RequiredRole | string | true | The role required to be able to authenticate. |
| AppIdUri | uri | false | The App ID URI of the app registration. Defaults to `api://{ClientId}` if not specified. |
| Instance | uri | false | Specifies the Azure cloud instance users are signing in from. Can be either the Azure public cloud or one of the national clouds. Defaults to the Azure public cloud (`https://login.microsoftonline.com`). |
| TenantId | string | false | The tenant id of the Azure Active Directory tenant, or its tenant domain. Defaults to `organizations`. |

A minimal configuration requires setting just the `ClientId` and `RequiredRole`.

### Example Configuration

<details>
  <summary>JSON</summary>

  ```json
  {
      "Authentication": {
          "AzureAd": {
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
  Authentication__AzureAd__ClientId: "5eaf6ccc-e8c1-47c6-a68c-a6453172c655"
  Authentication__AzureAd__RequiredRole: "Application.Access"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Authentication__AzureAd__ClientId
    value: "5eaf6ccc-e8c1-47c6-a68c-a6453172c655"
  - name: DotnetMonitor_Authentication__AzureAd__RequiredRole
    value: "Application.Access"
  ```
</details>

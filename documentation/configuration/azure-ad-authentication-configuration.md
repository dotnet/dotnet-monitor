
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fconfiguration%2Fazure-ad-authentication-configuration)

# Azure Active Directory Authentication Configuration (8.0+)

When `dotnet-monitor` is used to produce artifacts such as dumps or traces, an egress provider enables the artifacts to be stored in a manner suitable for the hosting environment rather than streamed back directly.

## Configuration Options

| Name | Type | Required | Description |
|---|---|---|---|
| ClientId | string | true | The unique application (client) id assigned to the app registration in Azure Active Directory. |
| RequiredRole | string | false | The app role required by other applications to be able to authenticate. If not specified, other applications will not be able to authenticate. |
| RequiredScope | string | false | The API scope required by users to be able to authenticate. If not specified, users will not be able to authenticate. |
| Audience | string | false | The App ID URI of the app registration. Defaults to `api://{ClientId}` if not specified. |
| Instance | string | false | Specifies the Azure cloud instance users are signing in from. Can be either the Azure public cloud or one of the national clouds. Defaults to the Azure public cloud (`https://login.microsoftonline.com`). |
| TenantId | string | false | The tenant id of the Azure Active Directory tenant, or its tenant domain. Defaults to `organizations`. |

A minimal configuration requires setting just the `ClientId` and either (or both) the `RequiredRole` and `RequiredScope` fields.

### Example Configuration

<details>
  <summary>JSON</summary>

  ```json
  {
      "Authentication": {
          "AzureAd": {
            "ClientId": "5eaf6ccc-e8c1-47c6-a68c-a6453172c655",
            "RequiredRole": "Application.Access",
            "RequiredScope": "access_as_user"
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
  Authentication__AzureAd__RequiredScope: "access_as_user"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Authentication__AzureAd__ClientId
    value: "5eaf6ccc-e8c1-47c6-a68c-a6453172c655"
  - name: DotnetMonitor_Authentication__AzureAd__RequiredRole
    value: "Application.Access"
  - name: DotnetMonitor_Authentication__AzureAd__RequiredScope
    value: "access_as_user"
  ```
</details>


### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Fparameters)

# Parameters - List (experimental feature)

Lists all parameters that have been captured.

> [!IMPORTANT]
> This feature is not enabled by default and requires configuration to be enabled. See [Enabling](parameters-post.md#enabling) for more information.

## HTTP Route

```http
GET /parameters?pid={pid}&uid={uid}&name={name}&requestId={requestId}&egressProvider={egressProvider}&tags={tags} HTTP/1.1
```

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `pid` | query | false | int | The ID of the process. |
| `uid` | query | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `name` | query | false | string | The name of the process. |
| `requestId` | query | false | guid | A value that uniquely identifies the request operation. If not specified, all captured parameters for all operations will be written. See the [request sample response](parameters-post#sample-response) for more details. |
| `egressProvider` | query | false | string | If specified, uses the named egress provider for egressing the captured parameters. When not specified, the parameters are written to the HTTP response stream. See [Egress Providers](../egress.md) for more details. |
| `tags` | query | false | string | A comma-separated list of user-readable identifiers for the operation. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, parameters from the [default process](defaultprocess.md) will be captured. Attempting to capture parameters from the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [CapturedParametersResult](definitions.md#capturedparametersresult)  | The captured parameters. | application/json |
| 200 OK | text | Text representation of the captured parameters. | text/plain |
| 202 Accepted | | When an egress provider is specified, the artifact has begun being collected. | |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many parameters requests at this time. Try to request parameters at a later time. | `application/problem+json` |


## Examples

### Sample Request

```http
GET /parameters?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
Accept: application/json
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json
{
    "captures": [
        {
            "requestId": "ca17b977-e5f4-46c5-98ca-17046ece998a",
            "activityId": "00-17657ab99a51e3e46cf5bb3fd583daab-03c903f46b511852-00",
            "capturedDateTime": "2024-03-15T14:47:51.2129742-04:00",
            "module": "System.Private.CoreLib.dll",
            "type": "System.String",
            "method": "Concat",
            "parameters": [
                {
                    "name": "str0",
                    "type": "System.String",
                    "module": "System.Private.CoreLib.dll",
                    "value": "\u0027localhost\u0027"
                },
                {
                "name": "str1",
                    "type": "System.String",
                    "module": "System.Private.CoreLib.dll",
                    "value": "\u0027:\u0027"
                },
                {
                    "name": "str2",
                    "type": "System.String",
                    "module": "System.Private.CoreLib.dll",
                    "value": "\u00277290\u0027"
                }
            ]
        },
        {
            "requestId": "d8cbec93-3fb8-4aae-82f6-fe9bdda20c34",
            "activityId": "00-9838f17b20cd76c2df3bdaa0fcd716c9-7ce53ae1e886e236-00",
            "capturedDateTime": "2024-03-15T14:48:42.5997554-04:00",
            "module": "System.Private.CoreLib.dll",
            "type": "System.String",
            "method": "Concat",
            "parameters": [
                {
                    "name": "str0",
                    "type": "System.String",
                    "module": "System.Private.CoreLib.dll",
                    "value": "\u0027\u0027"
                },
                {
                    "name": "str1",
                    "type": "System.String",
                    "module": "System.Private.CoreLib.dll",
                    "value": "\u0027/Account/SignIn\u0027"
                }
            ]
        }
    ]
}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 7+ |
| Linux | .NET 7+ |
| MacOS | .NET 7+ |

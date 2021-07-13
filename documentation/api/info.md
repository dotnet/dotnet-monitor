# Info - Get

Gets information about the Dotnet-Monitor version, the runtime version, and the listening mode.

## HTTP Route

```http
GET /info HTTP/1.1
```

## Host Address

The default host address for these routes is `https://localhost:52323`.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | | Information about Dotnet-Monitor formatted as JSON.  | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET info HTTP/1.1
Host: localhost:52323
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{"Version":"5.0.0-dev.21362.1+813e40f07bcf3de83b3bb374e2736ad256d0c067","RuntimeVersion":"3.1.16","ListeningMode":"Connect"}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

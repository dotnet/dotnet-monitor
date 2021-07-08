# Info - Get

Gets information about the Dotnet-Monitor version. Other configuration information may be included in the future.

## HTTP Route

```http
GET /info HTTP/1.1
```

## Host Address

This route is available on all configured addresses.

## Authentication

Authentication is not enforced for this route.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | | Information about Dotnet-Monitor formatted as JSON.  | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |

## Examples

### Sample Request

```http
GET info HTTP/1.1
Host: localhost:52325
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{"version":"5.0.0-dev.21358.1+6e4c58bec2230791bcd7958e4b44883ee224c596"}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

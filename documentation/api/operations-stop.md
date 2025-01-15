# Operations - Stop (7.1+)

Gracefully stops a running operation. Only valid against operations with the `isStoppable` property set to `true`, not all operations support being gracefully stopped. Transitions the operation to `Succeeded` or `Failed` state depending on if the operation was successful.

Stopping an operation may not happen immediately such as in the case of traces where stopping may collect rundown information. An operation in the `Stopping` state can still be cancelled using [Delete Operation](operations-delete.md).

## HTTP Route

```http
DELETE /operations/{operationId}?stop=true HTTP/1.1
```

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 202 Accepted |  | The operation was successfully queued to stop. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
DELETE /operations/67f07e40-5cca-4709-9062-26302c484f18?stop=true HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 202 OK
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

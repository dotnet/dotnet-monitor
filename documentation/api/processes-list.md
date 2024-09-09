# Processes - List

Lists the processes that are available from which diagnostic information can be obtained.

## HTTP Route

```http
GET /processes HTTP/1.1
```

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

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
| 200 OK | [ProcessIdentifier](definitions.md#processidentifier)[] | An array of process identifier objects. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /processes HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
    {
        "pid": 15000,
        "uid": "7b03fa5a-88ef-4630-899d-418bc0a3eb76"
    },
    {
        "pid": 21632,
        "uid": "cd4da319-fa9e-4987-ac4e-e57b2aac248b"
    },
    {
        "pid": 3380,
        "uid": "38f3eab1-c172-48b8-8dfd-b26986b37741"
    }
]
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

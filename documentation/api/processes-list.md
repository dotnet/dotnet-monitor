# Processes - List

Lists the processes that are available from which diagnostic information can be obtained.
```http
GET https://localhost:52323/processes
```

## Responses

| Name | Type | Description |
|---|---|---|
| 200 OK | [ProcessIdentifier](definitions.md#ProcessIdentifier)[] | An array of process identifier objects. |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) |  |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](authentication.md) for further information. |

## Examples

### Sample Request

```http
GET https://localhost:52323/processes
Authorization: MonitorApiKey QmFzZTY0RW5jb2RlZERvdG5ldE1vbml0b3JBcGlLZXk=
```

### Sample Response

```json
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
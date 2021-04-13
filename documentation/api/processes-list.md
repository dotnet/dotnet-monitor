# Processes - List

Lists the processes that are available from which diagnostic information can be obtained.
```http
GET https://localhost:52323/processes
```

## Responses

| Name | Type | Description |
|---|---|---|
| 200 OK | [ProcessIdentifier](#ProcessIdentifier)[] | An array of process identifier objects. |
| 400 Bad Request | ValidationProblemDetails |  |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](authentication.md) for further information. |

## Examples

### Sample Request

```http
GET https://localhost:52323/processes
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

## Definitions

### ProcessIdentifier

Object with process identifying information. The properties on this object describe indentifying aspects for a found process; these values can be used in other API calls to perform operations on specific processes.

| Name | Type | Description |
|---|---|---|
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` A 'null' value: `00000000-0000-0000-0000-000000000000` |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).
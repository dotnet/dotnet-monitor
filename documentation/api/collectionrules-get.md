# Collection Rules - Get

First Available: 6.3

Get the detailed state of the specified collection rule for all processes or for the specified process.

## HTTP Route

```http
GET /collectionrules/{collectionrulename}?pid={pid}&uid={uid}&name={name} HTTP/1.1
```

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

The default host address for these routes is `https://localhost:52323`. This route is only available on the addresses configured via the `--urls` command line parameter and the `DOTNETMONITOR_URLS` environment variable.

## URI Parameters

| Name | In | Required | Type | Description |
|---|---|---|---|---|
| `collectionrulename` | path | true | string | The name of the collection rule for which a detailed description should be provided. |
| `pid` | query | false | int | The ID of the process. |
| `uid` | query | false | guid | A value that uniquely identifies a runtime instance within a process. |
| `name` | query | false | string | The name of the process. |

See [ProcessIdentifier](definitions.md#processidentifier) for more details about the `pid`, `uid`, and `name` parameters.

If none of `pid`, `uid`, or `name` are specified, the detailed description of the collection rule for the [default process](defaultprocess.md) will be provided. Attempting to get the detailed description from the default process when the default process cannot be resolved will fail.

## Authentication

Authentication is enforced for this route. See [Authentication](./../authentication.md) for further information.

Allowed schemes:
- `Bearer`
- `Negotiate` (Windows only, running as unelevated)

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | [CollectionRuleDetailedDescription](definitions.md#collectionruledetaileddescription-63) | The detailed information about the current state of the specified collection rule. | `application/json` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#validationproblemdetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |

## Examples

### Sample Request

```http
GET /collectionrules?pid=21632 HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

or

```http
GET /collectionrules?uid=cd4da319-fa9e-4987-ac4e-e57b2aac248b HTTP/1.1
Host: localhost:52323
Authorization: Bearer fffffffffffffffffffffffffffffffffffffffffff=
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "state":"Running",
  "stateReason":"This collection rule is active and waiting for its triggering conditions to be satisfied.",
  "lifetimeOccurrences":0,
  "slidingWindowOccurrences":0,
  "actionCountLimit":2,
  "actionCountSlidingWindowDurationLimit":"00:01:00",
  "slidingWindowDurationCountdown":null,
  "ruleFinishedCountdown":"00:03:00"
}
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET 5+ |
| Linux | .NET 5+ |
| MacOS | .NET 5+ |

## Additional Notes

### When to use `pid` vs `uid`

See [Process ID `pid` vs Unique ID `uid`](pidvsuid.md) for clarification on when it is best to use either parameter.

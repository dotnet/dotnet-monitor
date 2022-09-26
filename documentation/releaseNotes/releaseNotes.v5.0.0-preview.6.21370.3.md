
Today we are releasing the next official preview of the `dotnet-monitor` tool. This release includes:
- `MonitorApiKey` now supports a configurable key length and hash algorithm. Both the key length (`--key-length`) and hash algorithm (`--hash-algorithm`) can also be supplied to the `generatekey` command via command line parameters.   (#361)
- The `/logs` endpoint now supports egressing logs with the [`application/json-seq`](https://datatracker.ietf.org/doc/html/rfc7464) Content-Type. (#468)
- ⚠️ Process selection uses query string instead of path segments. For example, a call to collect a gcdump of process id 21632 would now look like `GET /gcdump?pid=21632`. In addition to`pid` all other process selectors (`uid` and `name`) can also be specified via query string. This change impacts the `/process`, `/dump`, `/gcdump`, `/trace`, and `/logs` endpoint. (#496)
- ⚠️ Change in configuration for egress providers to allow for easier discoverability and greater extensibility. Each provider has a top-level configuration node under the `Egress` section. (#515)
- New `/info` API endpoint for diagnostics that provides the `dotnet monitor` version number, the runtime version, and the diagnostic port settings used to connect to a target process. (#540)
- ⚠️ New operational-style API by endpoints that produce a diagnostic artifact when egressed (`/dump`, `/gcdump`, `/trace`, and `/logs`). Prior to this change, long-running operations synchronously waited until the diagnostic artifact had been handled by an egress provider (e.g., a call to `/dump` would not return a HTTP response until the dump was written to Azure blob storage). The new API, will immediately return a 202 response with an operation id. Clients can then use this operation id to query the newly introduced `/operations` API to query `dotnet monitor` about the status of the operation. (#425)

\*⚠️ **_indicates a breaking change_**

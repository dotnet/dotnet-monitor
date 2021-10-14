Today we are releasing the official release canidate build of the `dotnet-monitor` tool. This release includes:

- ⚠️ Replaced `text/event-stream` with `text/plain` for `/logs` routes. (#71)
- ⚠️ Do not write byte order marker (BOM) in logs artifacts. (#1006)
- Add `CollectLogs` collection rule action. (#955)
- Stop collection rules on process when process is no longer detected. (#988)
- Add token-based parameterization to Arguments property of `Execute` collection rule action. (#966)
- Set `metricUrls` command line parameter to always use localhost by default. (#990)
- Fix `AspNetRequestDuration` collection rule trigger polling interval to use global counter interval. (#967)
- Handle all exceptions when enumerating processes. (#977)

\*⚠️ **_indicates a breaking change_**

Today we are releasing the official 8.0.0 Release Candidate of the `dotnet monitor` tool. This release includes:

- Exceptions: Fix throwing frame for call stack of eclipsing exceptions ([#5429](https://github.com/dotnet/dotnet-monitor/pull/5429))
- 🔬 Add support for formatting parameters using their [`DebuggerDisplayAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.debuggerdisplayattribute) when using `POST /parameters`. ([#5423](https://github.com/dotnet/dotnet-monitor/pull/5423))
- ⚠️ Update `GET /livemetrics` route to use configuration from Metrics section instead of only using the default event counters providers. Update `CollectLiveMetrics` action to use configuration from Metrics section by default. ([#5397](https://github.com/dotnet/dotnet-monitor/pull/5397))
- Add the ability to filter which exceptions are included with the `CollectExceptions` action. ([#5391](https://github.com/dotnet/dotnet-monitor/pull/5391))
- ⚠️ Rename `callStack` to `stack` on `ExceptionInstance` data. ([#5384](https://github.com/dotnet/dotnet-monitor/pull/5384))
- ⚠️ Rename `className` to `typeName` on `CallStackFrame` data. ([#5379](https://github.com/dotnet/dotnet-monitor/pull/5379))
- Add the ability to filter which exceptions are collected by `dotnet-monitor`. ([#5217](https://github.com/dotnet/dotnet-monitor/pull/5217))

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**
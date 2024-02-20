Today we are releasing the 8.0.0 build of the `dotnet monitor` tool.

Changes since 8.0.0-rc.2 release:

- Disabled features with HTTP routes will return HTTP 400 ([#5527](https://github.com/dotnet/dotnet-monitor/pull/5527))
- Remove `net6.0` TFM build and packaging. There is no impact on the ability to monitor .NET 6 (or any other version) applications. ([#5501](https://github.com/dotnet/dotnet-monitor/pull/5501))

Changes since 7.3.2 release:

- Disabled features with HTTP routes will return HTTP 400 ([#5527](https://github.com/dotnet/dotnet-monitor/pull/5527))
- Remove `net6.0` TFM build and packaging. There is no impact on the ability to monitor .NET 6 (or any other version) applications. ([#5501](https://github.com/dotnet/dotnet-monitor/pull/5501))
- Exceptions: Fix throwing frame for call stack of eclipsing exceptions ([#5429](https://github.com/dotnet/dotnet-monitor/pull/5429))
- 🔬 Add support for formatting parameters using their [`DebuggerDisplayAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.debuggerdisplayattribute) when using `POST /parameters`. ([#5423](https://github.com/dotnet/dotnet-monitor/pull/5423))
- ⚠️ Update `GET /livemetrics` route to use configuration from Metrics section instead of only using the default event counters providers. Update `CollectLiveMetrics` action to use configuration from Metrics section by default. ([#5397](https://github.com/dotnet/dotnet-monitor/pull/5397))
- Add the ability to filter which exceptions are included with the `CollectExceptions` action. ([#5391](https://github.com/dotnet/dotnet-monitor/pull/5391))
- ⚠️ Rename `callStack` to `stack` on `ExceptionInstance` data. ([#5384](https://github.com/dotnet/dotnet-monitor/pull/5384))
- ⚠️ Rename `className` to `typeName` on `CallStackFrame` data. ([#5379](https://github.com/dotnet/dotnet-monitor/pull/5379))
- Add the ability to filter which exceptions are collected by `dotnet-monitor`. ([#5217](https://github.com/dotnet/dotnet-monitor/pull/5217))
- 🔬 Enable capturing method parameters with `POST /parameters`. See [documentation/api/parameters.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/parameters.md) for more information. ([#5145](https://github.com/dotnet/dotnet-monitor/pull/5145))
- Before an operation has started running, it now has a state of `Starting` in the `/operations` API. ([#5142](https://github.com/dotnet/dotnet-monitor/pull/5142))
- Add the ability to filter which exceptions are displayed on the `/exceptions` route ([#5131](https://github.com/dotnet/dotnet-monitor/pull/5131))
- Add exception history egress and tagging support ([#5066](https://github.com/dotnet/dotnet-monitor/pull/5066))
- Add `CollectExceptions` collection rule action ([#5064](https://github.com/dotnet/dotnet-monitor/pull/5064))
- Add first chance exceptions history feature and `/exceptions` route. ([#4901](https://github.com/dotnet/dotnet-monitor/pull/4901))
- Make best effort to dynamically determine portable runtime identifier without explicit setting. ([#4777](https://github.com/dotnet/dotnet-monitor/pull/4777))
- Enable call stacks and exceptions as supported features. ([#4764](https://github.com/dotnet/dotnet-monitor/pull/4764))
- Enable UpDownCounter For Dotnet-Monitor ([#4310](https://github.com/dotnet/dotnet-monitor/pull/4310))
- Refactor AzureBlobStorage and S3Storage egress into extensions ([#4133](https://github.com/dotnet/dotnet-monitor/pull/4133))
- Added EventMeter Trigger for Collection Rules. ([#3812](https://github.com/dotnet/dotnet-monitor/pull/3812))
- Add support of egress provider to deliver data to a S3 storage ([#2016](https://github.com/dotnet/dotnet-monitor/pull/2016)) -- Thanks to `@Egyptmaster`

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**

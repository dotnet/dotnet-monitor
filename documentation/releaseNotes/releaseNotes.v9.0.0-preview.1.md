Today we are releasing the next official preview version of the `dotnet monitor` tool. This release includes:

- 🔬 Support capturing the following parameter types: generics, tuples, and nullable value types ([#5812](https://github.com/dotnet/dotnet-monitor/pull/5812))
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
- Prevent overreporting of the collection rule trigger type in console output ([#4883](https://github.com/dotnet/dotnet-monitor/pull/4883))
- Enable operation tracking for trigger actions ([#4826](https://github.com/dotnet/dotnet-monitor/pull/4826))
- Adds new `--exit-on-stdin-disconnect` command line switch to `collect` command ([#4792](https://github.com/dotnet/dotnet-monitor/pull/4792))
- Make best effort to dynamically determine portable runtime identifier without explicit setting. ([#4777](https://github.com/dotnet/dotnet-monitor/pull/4777))
- Enable call stacks and exceptions and supported features. ([#4764](https://github.com/dotnet/dotnet-monitor/pull/4764))
- 🔬 Enable call stacks feature to work with processes configured with the `nosuspend` option on their diagnostic port. ([#4733](https://github.com/dotnet/dotnet-monitor/pull/4733))
- ⚠️ Disable azure developer cli creds ([#4347](https://github.com/dotnet/dotnet-monitor/pull/4347))
- Enable workflow identity ([#4313](https://github.com/dotnet/dotnet-monitor/pull/4313))
- Enable UpDownCounter For Dotnet-Monitor ([#4310](https://github.com/dotnet/dotnet-monitor/pull/4310))
- Refactor AzureBlobStorage and S3Storage egress into extensions ([#4133](https://github.com/dotnet/dotnet-monitor/pull/4133))
- Represent System.Diagnostics.Metrics Counters as Gauges in Prometheus Exposition Format. ([#4098](https://github.com/dotnet/dotnet-monitor/pull/4098))
- Fixes issue where trace operations with an egress provider and rundown on Linux would cause dotnet-monitor to lose connectivity with the target application. ([#3960](https://github.com/dotnet/dotnet-monitor/pull/3960))
- Added EventMeter Trigger for Collection Rules. ([#3812](https://github.com/dotnet/dotnet-monitor/pull/3812))
- Allow per provider interval specification ([#4144](https://github.com/dotnet/dotnet-monitor/pull/4144))

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**

If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).
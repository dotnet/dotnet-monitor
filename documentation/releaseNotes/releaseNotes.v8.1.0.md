Today we are releasing the 8.1.0 build of the `dotnet monitor` tool.

Changes since 8.0.8 release:

- Add Support for Meter Tags and Instrument Tags for System Diagnostics Metrics ([#5802](https://github.com/dotnet/dotnet-monitor/pull/5802))
- Add `GcCollect` trace profile for collecting lightweight GC collection events, same as the `gc-collect` profile in the `dotnet-trace` tool. ([#6348](https://github.com/dotnet/dotnet-monitor/pull/6348))
- Adds support for overriding the artifact name for `Collect*` actions and adds new placeholders for `HostName` and `UnixTime`. ([#6675](https://github.com/dotnet/dotnet-monitor/pull/6675))
- The S3 egress provider now provides the ability to use KMS encryption keys to encrypt artifacts placed in S3 buckets ([#6831](https://github.com/dotnet/dotnet-monitor/pull/6831))
- CallStackFrame JSON results now include method token and module version ID. ([#6839](https://github.com/dotnet/dotnet-monitor/pull/6839))
- Use the `DOTNET` prefix when detecting environment variables and prefer it over the `COMPlus` prefix. ([#7434](https://github.com/dotnet/dotnet-monitor/pull/7434))
- Hide frames with [`StackTraceHiddenAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.stacktracehiddenattribute) from `/exceptions` and `/stacks`. When retrieving json data these frames are still included but are identified by a new `hidden` field. ([#7532](https://github.com/dotnet/dotnet-monitor/pull/7532))
- Fix an issue where `/stacks` route can fail to collect call stacks after `/parameters` route is used ([#7917](https://github.com/dotnet/dotnet-monitor/pull/7917))
- 🔬 Support capturing the following parameter types: generics, tuples, and nullable value types ([#5812](https://github.com/dotnet/dotnet-monitor/pull/5812))
- 🔬 Update `POST /parameters` to support stopping after a certain number of times parameters are captured. ([#6060](https://github.com/dotnet/dotnet-monitor/pull/6060))
- ⚠️ 🔬 Parameters can now be sent to an egress provider and are no longer logged inside the target application. ([#6272](https://github.com/dotnet/dotnet-monitor/pull/6272))

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**

If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).

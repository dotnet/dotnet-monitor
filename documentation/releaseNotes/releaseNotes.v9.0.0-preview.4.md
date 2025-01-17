Today we are releasing the next official preview version of the `dotnet monitor` tool. This release includes:

- Fix an issue that could cause monitored processes to fail to resume when in listen mode: [#6569](https://github.com/dotnet/dotnet-monitor/issues/6569) ([#6570](https://github.com/dotnet/dotnet-monitor/pull/6570))
- ⚠️ Add issuer and expiration validation for API keys ([#6456](https://github.com/dotnet/dotnet-monitor/pull/6456))
- Add `GcCollect` trace profile for collecting lightweight GC collection events, same as the `gc-collect` profile in the `dotnet-trace` tool. ([#6348](https://github.com/dotnet/dotnet-monitor/pull/6348))
- ⚠️ 🔬 Parameters can now be sent to an egress provider and are no longer logged inside the target application. ([#6272](https://github.com/dotnet/dotnet-monitor/pull/6272))

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**

If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).
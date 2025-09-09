Today we are releasing the official 10.0.0 Release Candidate of the `dotnet monitor` tool. This release includes:

- Added support for resetting in process features when dotnet-monitor is restarted. ([#8154](https://github.com/dotnet/dotnet-monitor/pull/8154))
- Add `ManagedEntryPointAssemblyName` to process info and collection rule filters ([#7984](https://github.com/dotnet/dotnet-monitor/pull/7984))
- Add `Capabilities` to `/info` route. ([#7977](https://github.com/dotnet/dotnet-monitor/pull/7977))
- 🔬 Added `ModuleVersionId` and `MethodToken` Fields to Parameter Capture Output ([#7974](https://github.com/dotnet/dotnet-monitor/pull/7974))
- Fix an issue where `/stacks` route can fail to collect call stacks after `/parameters` route is used ([#7914](https://github.com/dotnet/dotnet-monitor/pull/7914))
- Fixed an issue with custom tags not showing up for `Histogram` and `UpDownCounter` meters ([#7697](https://github.com/dotnet/dotnet-monitor/pull/7697))
- Hide frames with [`StackTraceHiddenAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.stacktracehiddenattribute) from `/exceptions` and `/stacks`. When retrieving json data these frames are still included but are identified by a new `hidden` field. ([#7532](https://github.com/dotnet/dotnet-monitor/pull/7532))
- Use the `DOTNET` prefix when detecting environment variables and prefer it over the `COMPlus` prefix. ([#7434](https://github.com/dotnet/dotnet-monitor/pull/7434))

\*🔬 **_indicates an experimental feature_**

If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).

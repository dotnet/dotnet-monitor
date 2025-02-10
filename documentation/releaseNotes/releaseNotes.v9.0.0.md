Today we are releasing the 9.0.0 build of the `dotnet monitor` tool. This release includes:

- Hide frames with [`StackTraceHiddenAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.stacktracehiddenattribute) from `/exceptions` and `/stacks`. When retrieving json data these frames are still included but are identified by a new `hidden` field. ([#7532](https://github.com/dotnet/dotnet-monitor/pull/7532))
- Use the `DOTNET` prefix when detecting environment variables and prefer it over the `COMPlus` prefix. ([#7434](https://github.com/dotnet/dotnet-monitor/pull/7434))



If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).
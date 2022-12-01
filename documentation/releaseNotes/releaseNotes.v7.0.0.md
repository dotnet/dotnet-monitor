Today we are releasing the 7.0.0 build of the `dotnet monitor` tool. This release includes:

- Add support for gracefully stopping a requested trace on demand while also collecting rundown, via [Stop Operation](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations-stop.md). All artifact requests are now also recorded under the [Operations API](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations.md), regardless of if they have an egress provider specified. ([#2893](https://github.com/dotnet/dotnet-monitor/pull/2893))
- Add the option to specify a `StoppingEvent` on the `CollectTrace` action. The trace will be stopped once either the duration is reached or the specified event occurs. ([#2884](https://github.com/dotnet/dotnet-monitor/pull/2884))
- Handle exceptions when determining additional information about discovered processes. ([#2813](https://github.com/dotnet/dotnet-monitor/pull/2813))

\*🔬 **_indicates an experimental feature_** \
\*⚠️ **_indicates a breaking change_**
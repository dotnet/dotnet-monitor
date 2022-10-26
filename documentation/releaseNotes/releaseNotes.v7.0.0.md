Today we are releasing the 7.0.0 build of the `dotnet monitor` tool. This release includes:

- Fixed an issue where AspNet* collection rules could stop functioning after collecting a trace with the Http profile. The fix was made in [dotnet/diagnostics#3425](https://github.com/dotnet/diagnostics/pull/3425). ([#2764](https://github.com/dotnet/dotnet-monitor/pull/2764))
- Change the default Garbage Collector mode to Workstation, except when running inside one of the official `dotnet monitor` docker images with more than 1 logical CPU core available. Learn more [here](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#garbage-collector-mode). ([#2729](https://github.com/dotnet/dotnet-monitor/pull/2729))
- Add the option to specify a `StoppingEvent` on the `CollectTrace` action. The trace will be stopped once either the duration is reached or the specified event occurs. ([#2557](https://github.com/dotnet/dotnet-monitor/pull/2557))
- Change the default Garbage Collector mode to Workstation, except when running inside one of the official `dotnet monitor` docker images with more than 1 logical CPU core available. Learn more [here](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#garbage-collector-mode). ([#2737](https://github.com/dotnet/dotnet-monitor/pull/2737))

\*🔬 **_indicates an experimental feature_** \
\*⚠️ **_indicates a breaking change_**
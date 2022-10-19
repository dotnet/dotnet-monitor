Today we are releasing the 7.0.0 build of the `dotnet monitor` tool. This release includes:

- Change the default Garbage Collector mode to Workstation, except when running inside one of the official `dotnet monitor` docker images with more than 1 logical CPU core available. Learn more [here](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#garbage-collector-mode). ([#2737](https://github.com/dotnet/dotnet-monitor/pull/2737))

\*🔬 **_indicates an experimental feature_** \
\*⚠️ **_indicates a breaking change_**
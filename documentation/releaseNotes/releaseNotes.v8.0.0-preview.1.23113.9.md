Today we are releasing the next official preview version of the `dotnet monitor` tool. This release includes:

- Produce RID-specific, TFM-specific, framework dependent archives ([#3507](https://github.com/dotnet/dotnet-monitor/pull/3507))
- Add support for System.Diagnostics.Metrics ([#3479](https://github.com/dotnet/dotnet-monitor/pull/3479))
- Add Swagger UI to dotnet-monitor as the default endpoint. This will provide documentation of endpoints, and also the ability to use them. ([#3241](https://github.com/dotnet/dotnet-monitor/pull/3241))
- 🔬 Add support for thread names stack artifacts. ([#2941](https://github.com/dotnet/dotnet-monitor/pull/2941))
- Add Metadata Support For Metrics ([#2939](https://github.com/dotnet/dotnet-monitor/pull/2939))
- Add Tags For API Requests ([#2919](https://github.com/dotnet/dotnet-monitor/pull/2919))
- Add support for gracefully stopping a requested trace on demand while also collecting rundown, via [Stop Operation](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations-stop.md). All artifact requests are now also recorded under the [Operations API](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations.md), regardless of if they have an egress provider specified. ([#2828](https://github.com/dotnet/dotnet-monitor/pull/2828))
- Handle exceptions when determining additional information about discovered processes. ([#2806](https://github.com/dotnet/dotnet-monitor/pull/2806))
- 🔬 Add support for [speedscope](https://speedscope.app) format when capturing stacks. ([#2795](https://github.com/dotnet/dotnet-monitor/pull/2795))
- Add support of egress provider to deliver data to a S3 storage ([#2016](https://github.com/dotnet/dotnet-monitor/pull/2016))
- Add the option to specify a `StoppingEvent` on the `CollectTrace` action. The trace will be stopped once either the duration is reached or the specified event occurs. ([#2884](https://github.com/dotnet/dotnet-monitor/pull/2884))

\*🔬 **_indicates an experimental feature_**
Today we are releasing the 7.1.1 build of the `dotnet monitor` tool. This release includes:

- Represent System.Diagnostics.Metrics Counters as Gauges in Prometheus Exposition Format. ([#4101](https://github.com/dotnet/dotnet-monitor/pull/4101))
- Fixes issue where trace operations with an egress provider and rundown on Linux would cause dotnet-monitor to lose connectivity with the target application. ([#3960](https://github.com/dotnet/dotnet-monitor/pull/3960))
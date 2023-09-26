Today we are releasing the next official preview version of the `dotnet monitor` tool. This release includes:

- Represent System.Diagnostics.Metrics Counters as Gauges in Prometheus Exposition Format. ([#4098](https://github.com/dotnet/dotnet-monitor/pull/4098))
- Fixes issue where trace operations with an egress provider and rundown on Linux would cause dotnet-monitor to lose connectivity with the target application. ([#3960](https://github.com/dotnet/dotnet-monitor/pull/3960))
- Added EventMeter Trigger for Collection Rules. ([#3812](https://github.com/dotnet/dotnet-monitor/pull/3812))
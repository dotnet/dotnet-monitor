Today we are releasing the 7.2.0 build of the `dotnet monitor` tool. This release includes:

- Fixes issue where trace operations with an egress provider and rundown on Linux would cause dotnet-monitor to lose connectivity with the target application. ([#3960](https://github.com/dotnet/dotnet-monitor/pull/3960))
- ⚠️ Disable azure developer cli credentials ([#4479](https://github.com/dotnet/dotnet-monitor/pull/4479))
- Enable workflow identity ([#4473](https://github.com/dotnet/dotnet-monitor/pull/4473))
- Allow per provider interval specification ([#4144](https://github.com/dotnet/dotnet-monitor/pull/4144))
- Represent System.Diagnostics.Metrics Counters as Gauges in Prometheus Exposition Format. ([#4100](https://github.com/dotnet/dotnet-monitor/pull/4100))

\*⚠️ **_indicates a breaking change_**
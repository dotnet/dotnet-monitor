Today we are releasing the 7.3.0 build of the `dotnet monitor` tool. This release includes:

- Enable operation tracking for trigger actions ([#4826](https://github.com/dotnet/dotnet-monitor/pull/4826))
- 🔬 Enable call stacks feature to work with processes configured with the `nosuspend` option on their diagnostic port. ([#4733](https://github.com/dotnet/dotnet-monitor/pull/4733))
- Fixes issue where trace operations with an egress provider and rundown on Linux would cause dotnet-monitor to lose connectivity with the target application. ([#3960](https://github.com/dotnet/dotnet-monitor/pull/3960))
- Prevent overreporting of the collection rule trigger type in console output ([#4889](https://github.com/dotnet/dotnet-monitor/pull/4889))
- ⚠️ Disable azure developer cli creds ([#4479](https://github.com/dotnet/dotnet-monitor/pull/4479))
- Enable workflow identity ([#4473](https://github.com/dotnet/dotnet-monitor/pull/4473))
- Allow per provider interval specification ([#4144](https://github.com/dotnet/dotnet-monitor/pull/4144))
- Represent System.Diagnostics.Metrics Counters as Gauges in Prometheus Exposition Format. ([#4100](https://github.com/dotnet/dotnet-monitor/pull/4100))

\*⚠️ **_indicates a breaking change_** \
\*🔬 **_indicates an experimental feature_**
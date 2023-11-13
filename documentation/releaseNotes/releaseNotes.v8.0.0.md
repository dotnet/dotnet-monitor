Today we are releasing the 8.0.0 build of the `dotnet monitor` tool. This release includes:

- Disabled features with HTTP routes will return HTTP 400 ([#5527](https://github.com/dotnet/dotnet-monitor/pull/5527))
- Remove `net6.0` TFM build and packaging. There is no impact on the ability to monitor .NET 6 (or any other version) applications. ([#5501](https://github.com/dotnet/dotnet-monitor/pull/5501))
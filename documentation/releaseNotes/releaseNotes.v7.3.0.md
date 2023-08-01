Today we are releasing the 7.3.0 build of the `dotnet monitor` tool. This release includes:

- Enable operation tracking for trigger actions ([#4826](https://github.com/dotnet/dotnet-monitor/pull/4826))
- Adds new `--exit-on-stdin-disconnect` command line switch to `collect` command ([#4792](https://github.com/dotnet/dotnet-monitor/pull/4792))
- 🔬 Enable call stacks feature to work with processes configured with the `nosuspend` option on their diagnostic port. ([#4733](https://github.com/dotnet/dotnet-monitor/pull/4733))
- Prevent overreporting of the collection rule trigger type in console output ([#4889](https://github.com/dotnet/dotnet-monitor/pull/4889))

\*🔬 **_indicates an experimental feature_**
Today we are releasing the next official preview version of the `dotnet monitor` tool. This release includes:

- Add first chance exceptions history feature and `/exceptions` route. ([#4901](https://github.com/dotnet/dotnet-monitor/pull/4901))
- Prevent overreporting of the collection rule trigger type in console output ([#4883](https://github.com/dotnet/dotnet-monitor/pull/4883))
- Enable operation tracking for trigger actions ([#4826](https://github.com/dotnet/dotnet-monitor/pull/4826))
- Adds new `--exit-on-stdin-disconnect` command line switch to `collect` command ([#4792](https://github.com/dotnet/dotnet-monitor/pull/4792))
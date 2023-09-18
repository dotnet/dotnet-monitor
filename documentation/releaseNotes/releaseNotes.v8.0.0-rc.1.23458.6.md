Today we are releasing the official 8.0 Release Candidate of the `dotnet monitor` tool. This release includes:

- 🔬 Enable capturing method parameters with `POST /parameters`. See [documentation/api/parameters.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/parameters.md) for more information. (#5145)
- Before an operation has started running, it now has a state of `Starting` in the `/operations` API. (#5142)
- Add the ability to filter which exceptions are displayed on the `/exceptions` route (#5131)
- Add exception history egress and tagging support (#5066)
- Add CollectExceptions collection rule action (#5064)

\*🔬 **_indicates an experimental feature_**

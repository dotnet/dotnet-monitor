Today we are releasing the official 9.0.0 Release Candidate of the `dotnet monitor` tool. This release includes:

- ⚠️ The `TenantId` property is now required when configuring `AzureAd` authentication. ([#7365](https://github.com/dotnet/dotnet-monitor/pull/7365))
- Fixed an issue that could sometimes result in exceptions having incomplete stack information reported in exception history. ([#7342](https://github.com/dotnet/dotnet-monitor/pull/7342))
- Fixed an issue that could sometimes result in exceptions not being reported in exception history. ([#7301](https://github.com/dotnet/dotnet-monitor/pull/7301))

\*⚠️ **_indicates a breaking change_**

If you would like to provide additional feedback to the team [please fill out this survey](https://aka.ms/dotnet-monitor-survey?src=rn).
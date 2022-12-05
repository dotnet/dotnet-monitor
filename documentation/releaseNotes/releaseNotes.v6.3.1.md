Today we are releasing the 6.3.1 build of the `dotnet monitor` tool. This release includes:

- Fixed an issue where AspNet* collection rules could stop functioning after collecting a trace with the Http profile. The fix was made in [dotnet/diagnostics#3425](https://github.com/dotnet/diagnostics/pull/3425). ([#2786](https://github.com/dotnet/dotnet-monitor/pull/2786))
- Handle exceptions when determining additional information about discovered processes. ([#2814](https://github.com/dotnet/dotnet-monitor/pull/2814))
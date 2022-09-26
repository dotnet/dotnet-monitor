
Today we are releasing the official 6.0.0 GA build of the `dotnet monitor` tool. This release includes:

- Add `netcoreapp3.1` target framework to allow tool to run on .NET Core 3.1 and .NET 5 (#1080).
- Prevent process enumeration from pruning processes that are capturing dumps (#1059)
- Fix metrics URL to respect values from configuration (#1070)
- Validate egress provider before starting egress operation (#1071)
- Fix root route to respond with HTTP 404 instead of HTTP 500 (#1072)

\*⚠️ **_indicates a breaking change_**

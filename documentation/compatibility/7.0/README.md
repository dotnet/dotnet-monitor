
# Breaking Changes in 7.0

If you are migrating your usage to `dotnet monitor` 7.0, the following changes might affect you. Changes are grouped together by areas within the tool.

## Installation

| Area | Title | Introduced |
|--|--|--|
| Deployment | The tool will not run on .NET Core 3.1 or .NET 5 due to removal of `netcoreapp3.1` target framework; **NOTE:** The tool will still be able to monitor applications running these .NET versions. | Preview 1 |
| Docker | Docker container entrypoint has been split among entrypoint and cmd instructions | Preview 3 |
| Egress | Built-in metadata keys for Azure Blob egress now prefixed with `DotnetMonitor_` | Preview 8 |

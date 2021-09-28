Today we are releasing the next official preview of the `dotnet-monitor` tool. This release includes:

- Added a new HTTP route (`/livemetrics`) to collect metrics on demand. (#68)
- Added collection rules for automated collection diagnostic artifacts based on trigger conditions in target applications.
- Updated process detection to cancel waiting on unresponsive processes.
- Documented recommended container limits. See [Running in Kubernetes](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/kubernetes.md)
- ⚠️ Upgraded runtime framework dependency from .NET Core 3.1 to .NET 6
- ⚠️ Reversioned from 5.0.0 to 6.0.0

\*⚠️ **_indicates a breaking change_**

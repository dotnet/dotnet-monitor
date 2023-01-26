Today we are releasing the next official preview of the `dotnet-monitor` tool. This release includes:

- Added a new HTTP route (`/livemetrics`) to collect metrics on demand. (#68)
- Added collection rules for automated collection of diagnostic artifacts based on trigger conditions in target applications. See [Collection Rules](https://github.com/dotnet/dotnet-monitor/blob/v6.0.0-preview.8.21503.3/documentation/collectionrules.md) for more details.
- Updated process detection to cancel waiting on unresponsive processes.
- Documented recommended container limits. See [Running in Kubernetes](https://github.com/dotnet/dotnet-monitor/blob/v6.0.0-preview.8.21503.3/documentation/kubernetes.md) for more details.
- ⚠️ Upgraded runtime framework dependency from .NET Core 3.1 to .NET 6
- ⚠️ Re-versioned from 5.0.0 to 6.0.0
- ⚠️ Fix all counter intervals to single global option (#923)

\*⚠️ **_indicates a breaking change_**

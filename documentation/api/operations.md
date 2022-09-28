
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Foperations)

# Operations

Operations are used to track long running operations in dotnet-monitor, specifically egressing data via egressProviders instead of directly to the client. This api is very similiar to [Azure asynchronous operations](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/async-operations#url-to-monitor-status).

| Operation | Description |
|---|---|
| [List Operations](operations-list.md) | Lists all the operations and their current state. |
| [Get Operation](operations-get.md) | Get detailed information about an operation. |
| [Delete Operation](operations-delete.md) | Cancels a running operation. |

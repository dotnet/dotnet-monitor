# Operations

Operations are used to track long running operations in dotnet-monitor, specifically egressing data via egressProviders instead of directly to the client. This api is very similar to [Azure asynchronous operations](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/async-operations#url-to-monitor-status).

| Operation | Description |
|---|---|
| [List Operations](operations-list.md) | Lists all the operations and their current state. |
| [Get Operation](operations-get.md) | Get detailed information about an operation. |
| [Delete Operation](operations-delete.md) | Cancels a running operation. |
| [Stop Operation](operations-stop.md) (7.1+) | Gracefully stops a running operation. |

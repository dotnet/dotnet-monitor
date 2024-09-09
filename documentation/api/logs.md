# Logs

The Logs API enables collecting logs that are logged to the [ILogger<> infrastructure](https://docs.microsoft.com/aspnet/core/fundamentals/logging) within a specified process.

> [!IMPORTANT]
> The [`LoggingEventSource`](https://docs.microsoft.com/aspnet/core/fundamentals/logging#event-source) provider must be enabled in the process in order to capture logs.

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

| Operation | Description |
|---|---|
| [Get Logs](logs-get.md) | Captures log statements from a process at a specified level or at the application-defined categories and levels. |
| [Get Custom Logs](logs-custom.md) | Captures log statements from a process using the settings specified in the request body. |

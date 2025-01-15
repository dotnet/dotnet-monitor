# Processes

The Processes API enables enumeration of the processes that `dotnet monitor` can detect and allows for obtaining their metadata (such as their names and environment variables).

> [!NOTE]
> Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

| Operation | Description |
|---|---|
| [Get Process](process-get.md) | Gets detailed information about a specified process. |
| [Get Process Environment](process-env.md) | `.NET 5+` Gets the environment block of a specified process.<br/>`.NET Core 3.1` Not supported. |
| [List Processes](processes-list.md) | Lists the processes that are available from which diagnostic information can be obtained. |

The `dotnet monitor` tool is able to detect .NET Core 3.1 and .NET 5+ applications. When connecting to a .NET Core 3.1 application, some information may not be available and is called out in the documentation.

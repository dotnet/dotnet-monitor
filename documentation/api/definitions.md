# Definitions

## ProcessIdentifier

Object with process identifying information. The properties on this object describe indentifying aspects for a found process; these values can be used in other API calls to perform operations on specific processes.

| Name | Type | Description |
|---|---|---|
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` An empty value: `00000000-0000-0000-0000-000000000000` |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).

## ProcessInfo

Object with detailed information about a specific process.

Some properties will have non-null values for procesess that are running on .NET 5 or newer (denoted with `.NET 5+`). These properties will be null for runtime versions prior to .NET 5.

| Name | Type | Description |
|---|---|---|
| `pid` | int | The ID of the process. |
| `uid` | guid | `.NET 5+` A value that uniquely identifies a runtime instance within a process.<br/>`.NET Core 3.1` A 'null' value: `00000000-0000-0000-0000-000000000000` |
| `name` | string | The name of the process. |
| `commandLine` | string | The command line of the process (includes process name and arguments) |
| `operatingSystem` | string | `.NET 5+` The operating system on which the process is running (e.g. `windows`, `linux`, `macos`).<br/>`.NET Core 3.1` A value of `null`. |
| `processArchitecture` | string | `.NET 5+` The architecture of the process (e.g. `x64`, `x86`).<br/>`.NET Core 3.1` A value of `null`. |

The `uid` property is useful for uniquely identifying a process when it is running in an environment where the process ID may not be unique (e.g. multiple containers within a Kubernetes pod will have entrypoint processes with process ID 1).

## ValidationProblemDetails

TBD
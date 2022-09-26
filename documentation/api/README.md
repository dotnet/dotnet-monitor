
# HTTP API Documentation

The HTTP API enables on-demand extraction of diagnostic information and artifacts from discoverable processes.

The following are the root routes on the HTTP API surface.

| Route | Description |
|---|---|
| [`/processes`](processes.md) | Gets detailed information about discoverable processes. |
| [`/dump`](dump.md) | Captures managed dumps of processes without using a debugger. |
| [`/gcdump`](gcdump.md) | Captures GC dumps of processes. |
| [`/trace`](trace.md) | Captures traces of processes without using a profiler. |
| [`/metrics`](metrics.md) | Captures metrics of a process in the Prometheus exposition format. |
| [`/livemetrics`](livemetrics.md) | Captures live metrics of a process. |
  [`/stacks`](stacks.md) | Gets the current callstacks of all .Net threads. |
| [`/logs`](logs.md) | Captures logs of processes. |
| [`/info`](info.md) | Gets info about Dotnet Monitor. |
| [`/operations`](operations.md) | Gets egress operation status or cancels operations. |

The `dotnet monitor` tool is able to detect .NET Core 3.1 and .NET 5+ applications. When connecting to a .NET Core 3.1 application, some information may not be available and is called out in the documentation.

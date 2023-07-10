
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2FREADME)

# HTTP API Documentation

The HTTP API enables on-demand extraction of diagnostic information and artifacts from discoverable processes.

>**Note**: Some features are [experimental](./../experimental.md) and are denoted as `**[Experimental]**` in this document.

The following are the root routes on the HTTP API surface.

| Route | Description | Version Introduced |
|---|---|---|
| [`/processes`](processes.md) | Gets detailed information about discoverable processes. | 6.0 |
| [`/dump`](dump.md) | Captures managed dumps of processes without using a debugger. | 6.0 |
| [`/gcdump`](gcdump.md) | Captures GC dumps of processes. | 6.0 |
| [`/trace`](trace.md) | Captures traces of processes without using a profiler. | 6.0 |
| [`/metrics`](metrics.md) | Captures metrics of a process in the Prometheus exposition format. | 6.0 |
| [`/livemetrics`](livemetrics.md) | Captures live metrics of a process. | 6.0 |
  [`/stacks`](stacks.md) | Gets the current callstacks of all .NET threads. | 8.0 Preview 7 |
| [`/logs`](logs.md) | Captures logs of processes. | 6.0 |
| [`/info`](info.md) | Gets info about `dotnet monitor`. | 6.0 |
| [`/operations`](operations.md) | Gets egress operation status or cancels operations. | 6.0 |
| [`/collectionrules`](collectionrules.md) | Gets the current state of collection rules. | 6.3 |

The `dotnet monitor` tool is able to detect .NET Core 3.1 and .NET 5+ applications. When connecting to a .NET Core 3.1 application, some information may not be available and is called out in the documentation.

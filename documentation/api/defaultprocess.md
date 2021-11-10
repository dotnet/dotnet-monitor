# Default Process

When using APIs to capture diagnostic artifacts, typically a `pid`, `uid`, or `name` is provided to perform the operation on a specific process. However, these parameters may be omitted if `dotnet monitor` is able to resolve a default process.

The tool is able to resolve a default process if there is one and only one observable process. If there are no processes or there is more than one process, any API that allows operating on the default process will fail when invoked.

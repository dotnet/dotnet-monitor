# Process ID `pid` vs Unique ID `uid`

Many of the HTTP routes allow specifying either the process ID `pid` or the unique ID `uid`. Which one to use depends on the target process and the environment in which the process is running.

The `uid` value of a process is guaranteed to be unique, regardless of environment, as long is the application is running on .NET 5+. Applications running on .NET Core 3.1 will have an empty value of `00000000-0000-0000-0000-000000000000` for `uid`.

Recommendations:
- For applications running on .NET 5+, use the `uid` parameter. This is especially beneficial when running in Docker or Kubernetes, since containerized .NET applications within the same pod will likely report process IDs of 1.
- For applications running on .NET Core 3.1, the `pid` parameter is the only option since .NET Core 3.1 processes have a `uid` of `00000000-0000-0000-0000-000000000000`.
- If `dotnet-monitor` is **NOT** set up to observe processes across process namespaces, then it is allowable to use the `pid` parameter regardless of the runtime version.
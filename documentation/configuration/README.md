# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Ordinarily, you are not required to specify most of this configuration and only exists if you wish to change the default behavior in `dotnet monitor`.

> [!NOTE]
> Some features are [experimental](../experimental.md) and are denoted as `**[Experimental]**` in these documents.

## Configuration Reference

- **[Configuration Sources](./configuration-sources.md)** - How to use JSON configuration files, Environment variables or Kubernetes for configuration
- **[Configuration Schema](#configuration-schema)** - How to get schema completion in supported editors
- **[View Merged Configuration](./view-merged-configuration.md)** - How to use a diagnostic command to show the merged configuration that will be applied
- **[Diagnostic Port Configuration](diagnostic-port-configuration.md)** - `dotnet monitor` communicates via .NET processes through their diagnostic port which can be changed if necessary
- **[Kestrel Configuration](#kestrel-configuration)** - Configure how dotnet monitor listens for http requests
- **[Storage Configuration](./storage-configuration.md)** Some diagnostic features (e.g. memory dumps, stack traces) require that a directory is shared between the `dotnet monitor` tool and the target applications. The `Storage` configuration section allows specifying these directories to facilitate this sharing.
- **[Default Process Configuration](./default-process-configuration.md)** - Used to determine which process is used for metrics and in situations where the process is not specified in the query to retrieve an artifact.
- **[Metrics Configuration](./metrics-configuration.md)** - Configuration of the `/metrics` endpoint for live metrics collection
- **[Egress Configuration](./egress-configuration.md)** - When `dotnet-monitor` is used to produce artifacts such as dumps or traces, an egress provider enables the artifacts to be stored in a manner suitable for the hosting environment rather than streamed back directly.]
- **[In-Process Features Configuration](./in-process-features-configuration.md)** - Some features of `dotnet monitor` require loading libraries into target applications that may have performance impact on memory and CPU utilization
- **[Garbage Collector Mode](#garbage-collector-mode)** - Configure which GC mode is used by the `dotnet monitor` process.

## Kestrel Configuration

// TODO

## Cross-Origin Resource Sharing (CORS) Configuration

// TODO

## Configuration Schema

`dotnet monitor`'s various configuration knobs have been documented via JSON schema. Using a modern editor like VS or VS Code that supports JSON Schema makes it trivial to author complex configuration objects with support for completions and rich descriptions via tooltips.

To get completion support in your editor, simply add the `$schema` property to the root JSON object as shown below:

```json
{
  "$schema": "https://aka.ms/dotnet-monitor-schema"
}
```

Once you've added the `$schema` property, you should have support for completions in your editor.

![completions](https://user-images.githubusercontent.com/4734691/115377729-bf2bb600-a184-11eb-9b8e-50f361c112f0.gif)

## Garbage Collector Mode

Starting in 7.0, by default `dotnet monitor` will use Workstation GC mode, unless running in one of the official [docker images](../docker.md) where it will use Server GC mode by default but will fallback to Workstation mode if only one logical CPU core is available.

You can learn more about the different GC modes [here](https://learn.microsoft.com/aspnet/core/performance/memory?view=aspnetcore-6.0#workstation-gc-vs-server-gc), and how to configure the default GC mode [here](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector#workstation-vs-server).

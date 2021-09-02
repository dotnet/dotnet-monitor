# Running in Kubernetes

## Recommended container limits

```yaml
resources:
    requests:
        memory: "32Mi"
        cpu: "50m"
    limits:
        memory: "256Mi"
        cpu: "250m"
```

How much memory and CPU is consumed by dotnet-monitor is dependant on which scenarios are being executed: 
- Metrics consume a negligible amount of resources, although using custom metrics can affect this.
- Operations such as traces and logs may require memory in the main application container that will automatically be allocated by the runtime.
- Resource consumption by trace operations is also dependent on which providers are enabled, as well as the [buffer size](./api/definitions.md#EventProvidersConfiguration) allocated in the runtime.
- It is not recommended to use highly verbose [log levels](./api/definitions.md#LogLevel) while under load. This causes a lot of CPU usage in the dotnet-monitor container and more memory pressure in the main application container.
- Dumps also temporarily increase the amount of memory consumed by the application container.
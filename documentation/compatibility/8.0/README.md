# Breaking Changes in 8.0

If you are migrating your usage to `dotnet monitor` 8.0, the following changes might affect you. Changes are grouped together by areas within the tool.

## Changes

| Area | Title | Introduced |
|--|--|--|
| Deployment | [The tool will not run on .NET 6 or 7 due to removal of `net 6.0` and `net 7.0` target frameworks](#deployment-only-1-tfm-for-dotnet-monitor); **Note**: The tool will still be able to monitor applications running these .NET versions. | GA |
| Docker | [Non-Root by default](#docker-non-root-by-default) | GA |
| Metrics | [Live metrics respects `Metrics` configuration](#metrics-live-metrics-respects-metrics-configuration) | GA |

## Details

### Deployment: Only 1 TFM for dotnet-monitor

The `dotnet monitor` tool now targets a single framework. See [PR 5501](https://github.com/dotnet/dotnet-monitor/pull/5501). This has no impact on what applications can be monitored nor any impact on the dotnet-monitor docker image but installation through `dotnet tool install` will require the dotnet 8 sdk installed.

### Docker: Non-root by default

In `dotnet monitor` 8, dotnet-monitor runs as a non-root distroless container by default. If the application is based on a distroless dotnet 8 ASP.NET image, no changes are needed. However, if the app is running as root, it may require one of the following mitigations:

> [!IMPORTANT]
> Set both `runAsUser` and `runAsGroup` for each mitigation.

1. Root application, non-root dotnet-monitor

> [!NOTE]
> In reverse server mode, this appears to work for basic scenarios because it's possible to connect from the app to dotnet-monitor. However, scenarios such as callstacks and dump copying will not work.

Elevate dotnet-monitor to match the application.

``` yaml
image: mcr.microsoft.com/dotnet/monitor:8
securityContext:
    runAsNonRoot: false
    runAsUser: 0
    runAsGroup: 0
```

2. Non-root application with a different user id than the default (the default uid/gid for .NET 8 distroless and all .NET Ubuntu Chiseled apps is 1654)

``` yaml
image: mcr.microsoft.com/dotnet/monitor:8
securityContext:
    runAsUser: 5000
    runAsGroup: 5000
    runAsNonRoot: true
```

3. Set the security context of _all_ the containers to be the same user. This is useful when looking at multiple containers with one dotnet-monitor

``` yaml
spec:
  securityContext:
    runAsUser: 5000
    runAsGroup: 5000
    runAsNonRoot: true
  containers:
```

4. Volume mount permissions

When using shared volume mounts, it may be necessary to set the [`fsGroup`](https://kubernetes.io/docs/tasks/configure-pod-container/security-context/#set-the-security-context-for-a-pod) for your deployment. This will grant secondary group permissions for shared volumes for both the main container and the dotnet-monitor side car.

``` yaml
spec:
  securityContext:
    fsGroup: 2000
  containers:
```

See [sample](../../../samples/AKS_Tutorial/deploy.yaml) for a sample deployment.


### Metrics: Live metrics respects metrics configuration

See https://github.com/dotnet/dotnet-monitor/pull/5397

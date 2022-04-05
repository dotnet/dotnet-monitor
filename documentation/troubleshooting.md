# Troubleshooting Guide

Here is our guide for diagnosing specific issues with `dotnet monitor`. This is an ongoing effort to add issues and solutions to this guide; you can help us by letting us know in the [Issues](https://github.com/dotnet/dotnet-monitor/issues) section if we should add something here.

## Problem: In Azure App Service on Linux, dotnet-monitor is consuming lots of CPU time

If you find that `dotnet monitor` is consuming lots of CPU time, this typically means that there is a high load of logging statements being emitted from the process. `dotnet monitor` is used to capture `AppServiceAppLogs` and `AllMetrics` from your process before sending them onto other systems; these settings are configured under Monitoring > Diagnostic Settings in the Azure Portal. `dotnet monitor` is also always enabled to collect a baseline level of metrics from the process to display in the Azure Portal. However, metrics monitoring is lightweight such that it should not show up as an impact on CPU time.

The expected CPU and memory usage of `dotnet monitor` is described under [Recommended container limits](./kubernetes.md#recommended-container-limits). The resource usage applies to all scenarios using `dotnet-monitor`, not just in Kubernetes.

> Solution 1 (temporary fix):
>
> Turn off `AppServiceAppLogs` under Monitoring > Diagnostic Settings in the Azure Portal.

> Solution 2:
>
> Adjust the logging being emitted in your app to be in-line with CPU performance.
> For ASP.NET you can use the configuration settings described in [ASP.NET Core 6.0 logging: Configure logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0#configure-logging).

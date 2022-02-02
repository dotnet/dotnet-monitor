# Troubleshooting Guide

Here is our guide for diagnosing specific issues with `Dotnet-Monitor`. This is an ongoing effort to add issues and solutions to this guide and is never complete but you can help us by letting us know in the issues sections if we should add something here

## Problem: In Azure App Service on Linux, dotnet-monitor is consuming lots of CPU time

If you find that the `dotnet-monitor` is consuming lots of CPU time, this typically means that there is a high load of logging statements being emitted from the process. `dotnet-monitor` is used to capture `AppServiceAppLogs` and `AllMetrics` from your process before sending them onto other systems; these settings are configured under Monitoring > Diagnostic Settings in the Azure Portal. `dotnet-monitor` is also always enabled to collect a baseline level of metrics from the process to display in the Azure Portal, however this activity is fast enough that it should not show up as an impact on CPU time.

> Solution #1 (temporary fix):
>
> Turn off `AppServiceAppLogs` under  Monitoring > Diagnostic Settings

> Solution #2:
>
> Adjust the logging being emitted in your app to be in-line with CPU performance.

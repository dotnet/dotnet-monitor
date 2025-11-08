# Running dotnet-monitor as an ephemeral container in Kubernetes

Running `dotnet-monitor` as an ephemeral container lets you bring powerful diagnostics tooling to a running .NET workload only when you need it,
without permanently increasing pod resource usage or attack surface. Instead of baking monitoring into every application image or running a sidecar 24/7,
you can inject a purpose-built container on demand to capture dumps, traces, metrics, logs, and other artifacts from a live process—even a hung or crash-looping one.

Key advantages:

* On-demand diagnostics: Start the container only when investigation is required; no steady-state overhead.
* Minimal performance impact: Eliminates continuous profiler / collector costs until activated.
* Reduced image complexity: Keeps app images lean (no extra tools or dependencies bundled).
* Smaller security surface: Tooling and elevated permissions exist for minutes instead of the lifetime of the pod.
* Post-mortem access: Ephemeral containers can attach after a failure or while the target process is unresponsive.
* Version agility: Use the `dotnet-monitor` image version independently of deployed app versions.
* Operational consistency: Same workflow across all pods without pre-provisioning sidecars.
* Cost optimization: Fewer always-on containers and lower baseline CPU/memory utilization.

The sections below show how to inject and use an ephemeral `dotnet-monitor` container to collect diagnostic data from .NET applications running in Kubernetes.

## Prerequisits
1. Kubernetes version >= 1.25
2. Pod deployed with the necessary settings: env, volume and volume mount. See [template](_dotnetmonitor.tpl).

## Use dotnet monitor in a k8s pod

First you need to inject the dotnet monitor as an ephemeral container. The variables in the next example should match the target .Net container you wish to inspect.
Here's an example of the [ConfigFile](config.yaml). Make sure the values match with the deployed [template](_dotnetmonitor.tpl). This step should be done only once
per pod. The ephemeral container will remain until the pod restarts.

```pwsh
kubectl debug -n $Namespace pod/$Pod `
    --image="mcr.microsoft.com/dotnet/monitor:8.0" `
    --container "debugger" `
    --target $AppContainer `
    --profile=general `
    --custom $ConfigFile
```

Once you have injected your ephemeral dotnet monitor you next need to start port-forwarding to use the API. In the case of [collection rules](../../documentation/api/collectionrules.md)
and [egress](../../documentation/egress.md) already being configured, this is not necessary but still a valid option for ad-hoc collections.

```pwsh
kubectl port-forward -n $Namespace pod/$Pod "${MonitorPort}:${MonitorPort}"
```

Finally from you should be able to make use of [dotnet monitor HTTP API](../../documentation/api/README.md), as the following example shows:

```pwsh
$MonitorPort = 52323
$ProcessId = 1
$ts = Get-Date -Format 'yyyyMMdd_HHmmss'
$file = "./diagnostics/gcdump_${ProcessId}_${ts}.gcdump"
# API: GET /gcdump?pid=PID
$uri = "http://127.0.0.1:$MonitorPort/gcdump?pid=$ProcessId"
Write-Host "[INFO] Collecting GC dump for PID $ProcessId";
Invoke-WebRequest -Method GET -UseBasicParsing `
    -Uri $uri `
    -Headers @{ Accept='application/octet-stream' } `
    -OutFile $file
```

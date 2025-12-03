# Running dotnet-monitor as an Ephemeral Container in Kubernetes

Running `dotnet-monitor` as an ephemeral container lets you attach diagnostics tooling to a live .NET workload only when you need it—without permanent resource, security, or operational overhead. Instead of baking tools into each application image or running a sidecar continuously, you temporarily inject a container to collect dumps, traces, logs, metrics, or other artifacts (even from hung or crash-looping processes) and then let it disappear.

### Why use an ephemeral container?
* On-demand: No steady-state CPU/memory cost; start only for investigations.
* Lightweight images: Keep app container images free of extra tooling.
* Smaller attack surface: Elevated permissions and tooling exist for minutes, not the lifetime of the pod.
* Post-mortem access: Attach after failures or while the target process is unresponsive.
* Version independence: Use the latest `dotnet-monitor` image regardless of app version.
* Consistent workflow: Same injection procedure across all pods; no pre-provisioned sidecars.
* Cost aware: Fewer always-on containers reduces baseline resource usage.

## Prerequisites
1. Kubernetes v1.25 or newer (ephemeral containers stable).
2. Target pod created with required env vars, volume, and volume mounts.

## Inject dotnet monitor into a Pod
Prepare a [config file](config.yaml) whose values match the target's deployment dotnet monitor configuration. This step is performed once per pod lifetime; the ephemeral container persists until the pod restarts.

```bash
Namespace="<target pod namespace>"
Pod="<target pod>"
AppContainer="<target container app>"
ConfigFile="./config.yaml"
MonitorPort=52323

kubectl debug -n "$Namespace" "pod/$Pod" \
    --image "mcr.microsoft.com/dotnet/monitor:10.0" \
    --container "debugger" \
    --profile "general" \
    --custom "$ConfigFile"
```

## Access the HTTP API
If you have existing [collection rules](../../documentation/api/collectionrules.md) and [egress](../../documentation/egress.md) configured, port-forwarding is optional; otherwise it enables ad-hoc requests.

```bash
kubectl port-forward -n $Namespace pod/$Pod "${MonitorPort}:${MonitorPort}"
```

## Example: Collect a GC Dump
After port-forwarding, call the [HTTP API](../../documentation/api/README.md):

```bash
ProcessId=1
ts=$(date +'%Y%m%d_%H%M%S')
file="./diagnostics/gcdump_${ProcessId}_${ts}.gcdump"
uri="http://127.0.0.1:${MonitorPort}/gcdump?pid=${ProcessId}"
echo "[INFO] Collecting GC dump for PID ${ProcessId}" >&2
mkdir -p "$(dirname "$file")"
curl -sS -H 'Accept: application/octet-stream' "$uri" -o "$file"
echo "[INFO] Saved GC dump to $file" >&2
```

## Next Steps
* Use other endpoints for traces (`/trace`), process dumps (`/dump`), or metrics.
* Configure secure [authentication](../../documentation/authentication.md).
* Automate common investigations with [collection rules](../../documentation/collectionrules/collectionrules.md) and [egress](../../documentation/egress.md) before incidents occur.

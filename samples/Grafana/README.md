# Configuring Managed Grafana Dashboard in Azure for Dotnet Monitor

This docs provides instructions on customizing metrics scraping for a Kubernetes cluster with the metrics addon in Azure Monitor.

## Step 1: Dotnet Monitor configuration

The following settings ensure that the [metrics endpoint are bound to an external addresses](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration/metrics-configuration.md#metrics-urls), and not localhost, which will prevent scraping from any agents. Today we recommend setting a fairly low scraping interval for metric count.

```yaml
Metrics__Endpoints: http://+:52325
Metrics__MetricCount: '1'
```

## Step 2: Include deployment annotations

```yaml
annotations:
    prometheus.io/scrape: 'true'
    prometheus.io/path: '/metrics'
    prometheus.io/port: '52325'
    prometheus.io/scheme: 'http'
```

## Step 3: Apply Configmap

You can download this [metrics settings config map file](https://github.com/Azure/prometheus-collector/blob/main/otelcollector/configmaps/ama-metrics-settings-configmap.yaml) and change the settings as required. Apply/deploy the configmap to kube-system namespace for your cluster as follows.

Once you have downloaded the Configmap  ensure that namespace configured for your app is updated in **podannotationnamespaceregex**. If your namespace is blank or undefined your namespace will become 'default' as follows.  

```yaml
podannotationnamespaceregex = "default"
```

Once your Configmap is saved run the following command, please note it only needs to be set once as it is applied to the kube system namespace.

``` powershell
kubectl apply -f .\ama-metrics-settings-configmap.yaml -n kube-system
```

This configures the prometheus agent to check the default namespace  for active pods and use the annotations in the pods to scrape for prometheus data on an interval defined in the Configmap.

## Step 4: Configuring Azure Managed Grafana Dashboard

After creating your [Azure Managed Grafana instance](https://learn.microsoft.com/en-us/azure/managed-grafana/quickstart-managed-grafana-portal) you can start creating dashboards based on the .NET metrics exposed via Dotnet monitor (and ultimately Prometheus).

Navigate to the 'dashboards' page and select *New->Import*. This navigates to the import page where you can load a [pre-configured dashboard for .NET metrics](https://grafana.com/grafana/dashboards/19297-dotnet-monitor-dashboard/).

IMAGE

Select a Folder for your newly imported dashboard along with the Managed Prometheus data source configured earlier.

IMAGE

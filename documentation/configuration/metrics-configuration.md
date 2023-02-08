
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fconfiguration%2Fmetrics-configuration)

# Metrics Configuration

## Global Counter Interval

Due to limitations in event counters, `dotnet monitor` supports only **one** refresh interval when collecting metrics. This interval is used for
Prometheus metrics, livemetrics, triggers, traces, and trigger actions that collect traces. The default interval is 5 seconds, but can be changed in configuration.

<details>
  <summary>JSON</summary>

  ```json
  {
      "GlobalCounter": {
        "IntervalSeconds": 10
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  GlobalCounter__IntervalSeconds: "10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_GlobalCounter__IntervalSeconds
    value: "10"
  ```
</details>

## Metrics Urls

In addition to the ordinary diagnostics urls that `dotnet monitor` binds to, it also binds to metric urls that only expose the `/metrics` endpoint. Unlike the other endpoints, the metrics urls do not require authentication. Unless you enable collection of custom providers that may contain sensitive business logic, it is generally considered safe to expose metrics endpoints. 

<details>
  <summary>Command Line</summary>

  ```cmd
  dotnet monitor collect --metricUrls http://*:52325
  ```
</details>

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Endpoints": "http://*:52325"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__Endpoints: "http://*:52325"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__Endpoints
    value: "http://*:52325"
  ```
</details>

## Customize collection interval and counts

In the default configuration, `dotnet monitor` requests that the connected runtime provides updated counter values every 5 seconds and will retain 3 data points for every collected metric. When using a collection tool like Prometheus, it is recommended that you set your scrape interval to `MetricCount` * `GlobalCounter:IntervalSeconds`. In the default configuration, we recommend you scrape `dotnet monitor` for metrics every 15 seconds.

You can customize the number of data points stored per metric via the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "MetricCount": 3
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__MetricCount: "3"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__MetricCount
    value: "3"
  ```
</details>

See [Global Counter Interval](#global-counter-interval) to change the metrics frequency.

## Custom Metrics

Additional metrics providers and counter names to return from this route can be specified via configuration. 

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Providers": [
        {
          "ProviderName": "Microsoft-AspNetCore-Server-Kestrel",
          "CounterNames": [
            "connections-per-second",
            "total-connections"
          ]
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__Providers__0__ProviderName: "Microsoft-AspNetCore-Server-Kestrel"
  Metrics__Providers__0__CounterNames__0: "connections-per-second"
  Metrics__Providers__0__CounterNames__1: "total-connections"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__Providers__0__ProviderName
    value: "Microsoft-AspNetCore-Server-Kestrel"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__0
    value: "connections-per-second"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__1
    value: "total-connections"
  ```
</details>

> **Warning:** In the default configuration, custom metrics will be exposed along with all other metrics on an unauthenticated endpoint. If your metrics contains sensitive information, we recommend disabling the [metrics urls](#metrics-urls) and consuming metrics from the authenticated endpoint (`--urls`) instead.

When `CounterNames` are not specified, all the counters associated with the `ProviderName` are collected.

[8.0+] Custom metrics support labels for metadata. Metadata cannot include commas (`,`); the inclusion of a comma in metadata will result in all metadata being removed from the custom metric.

[8.0+] `System.Diagnostics.Metrics` is now supported in a limited capacity for custom metrics. At this time, there are several known limitations:
 * `System.Diagnostics.Metrics` cannot have multiple sessions collecting metrics concurrently (i.e. `/metrics` and `/livemetrics` cannot both be looking for `System.Diagnostics.Metrics` at the same time). 
 * There is currently no trigger for `System.Diagnostics.Metrics` for collection rule scenarios.
 * When using `dotnet monitor` in `Listen` mode, `dotnet monitor` may be unable to collect `System.Diagnostics.Metrics` if the target app starts after `dotnet monitor` starts.

### Set [`MetricType`](../api/definitions.md)

By default, `dotnet monitor` is unable to determine whether a custom provider is an `EventCounter` or `System.Diagnostics.Metrics`, and will attempt to collect both kinds of metrics for the specified provider. To explicitly specify whether a custom provider is an `EventCounter` or `System.Diagnostics.Metrics`, set the appropriate `MetricType`:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Providers": [
        {
          "ProviderName": "MyCustomEventCounterProvider",
          "MetricType": "EventCounter"
        },
        {
          "ProviderName": "MyCustomSDMProvider",
          "MetricType": "Meter"
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__Providers__0__ProviderName: "MyCustomEventCounterProvider"
  Metrics__Providers__0__MetricType: "EventCounter"
  Metrics__Providers__1__ProviderName: "MyCustomSDMProvider"
  Metrics__Providers__1__MetricType: "Meter"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__Providers__0__ProviderName
    value: "MyCustomEventCounterProvider"
  - name: DotnetMonitor_Metrics__Providers__0__MetricType
    value: "EventCounter"
  - name: DotnetMonitor_Metrics__Providers__1__ProviderName
    value: "MyCustomSDMProvider"
  - name: DotnetMonitor_Metrics__Providers__1__MetricType
    value: "Meter"
  ```
</details>

## Limit How Many Histograms To Track (8.0+)

For System.Diagnostics.Metrics, `dotnet monitor` allows you to set the maximum number of histograms that can be tracked. Each unique combination of provider name, histogram name, and dimension values counts as one histogram. Tracking more histograms uses more memory in the target process so this bound guards against unintentional high memory use. `MaxHistograms` has a default value of `20`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "GlobalCounter": {
      "MaxHistograms": 5
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  GlobalCounter__MaxHistograms: "5"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_GlobalCounter__MaxHistograms
    value: "5"
  ```
</details>

## Limit How Many Time Series To Track (8.0+)

For System.Diagnostics.Metrics, `dotnet monitor` allows you to set the maximum number of time series that can be tracked. Each unique combination of provider name, metric name, and dimension values counts as one time series. Tracking more time series uses more memory in the target process so this bound guards against unintentional high memory use. `MaxTimeSeries` has a default value of `1000`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "GlobalCounter": {
      "MaxTimeSeries": 500
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  GlobalCounter__MaxTimeSeries: "500"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_GlobalCounter__MaxTimeSeries
    value: "500"
  ```
</details>

## Disable default providers

In addition to enabling custom providers, `dotnet monitor` also allows you to disable collection of the default providers. You can do so via the following configuration:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "IncludeDefaultProviders": false
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>
  
  ```yaml
  Metrics__IncludeDefaultProviders: "false"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>
  
  ```yaml
  - name: DotnetMonitor_Metrics__IncludeDefaultProviders
    value: "false"
  ```
</details>

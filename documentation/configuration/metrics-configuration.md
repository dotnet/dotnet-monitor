# Metrics Configuration

## Default Providers

The `/metrics` route (and starting in 8.0, the `/livemetrics` route and `CollectLiveMetrics` actions) will collect metrics from the default providers. The default providers are:
- `System.Runtime`
- `Microsoft.AspNetCore.Hosting`
- `Grpc.AspNetCore.Server`

These providers are collected by default for the above mentioned features, even when specifying [custom metrics](#custom-metrics) collection. The default providers can be excluded by [disabling](#disable-default-providers) them.

## Global Counter Interval

Due to limitations in event counters, `dotnet monitor` supports only **one** refresh interval when collecting metrics. This interval is used for
Prometheus metrics, livemetrics, triggers, traces, and trigger actions that collect traces. The default interval is 5 seconds, but can be changed in configuration.

[7.1+] For EventCounter providers, is possible to specify a different interval for each provider. See [Per provider intervals](#per-provider-intervals).

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

## Per Provider Intervals

First Available: 7.1

It is possible to override the global interval on a per provider basis. Note this forces all scenarios (triggers, live metrics, prometheus metrics, traces) that use a particular provider to use that interval. Metrics that are `System.Diagnostics.Metrics` based always use global interval.

<details>
  <summary>JSON</summary>

  ```json
  {
      "GlobalCounter": {
        "IntervalSeconds": 5,
        "Providers": {
            "System.Runtime": {
              "IntervalSeconds": 10
            }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  GlobalCounter__IntervalSeconds: "5"
  GlobalCounter__Providers__System.Runtime__IntervalSeconds: "10"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_GlobalCounter__IntervalSeconds
    value: "5"
  - name: DotnetMonitor_GlobalCounter__Providers__System.Runtime__IntervalSeconds
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

[8.0+] `System.Diagnostics.Metrics` is now supported for custom metrics. At this time, there are the following known limitations:
 * `dotnet monitor` may fail to collect `System.Diagnostics.Metrics` if it begins collecting the metric before the target app creates the Meter ([note that this is fixed for .NET 8+ apps](https://github.com/dotnet/runtime/pull/76965)).

### Adding Meters/Instruments for `System.Diagnostics.Metrics`

Specifying a `Meter` is done differently than for `EventCounter` providers. The following example uses `MyCounter1` and `MyCounter2` on an `EventCounter` named `MyProvider`, as well as the `MyInstrument` instrument on `MyCustomMeter` and all instruments on the `AnotherMeter` meter:

<details>
  <summary>JSON</summary>

  ```json
  {
    "Metrics": {
      "Providers": [
        {
          "ProviderName": "MyProvider",
          "CounterNames": ["MyCounter1", "MyCounter2"]
        }
      ],
      "Meters": [
        {
          "MeterName": "MyCustomMeter",
          "InstrumentNames": ["MyInstrument"]
        },
        {
          "MeterName": "AnotherMeter"
        }
      ]
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Metrics__Providers__0__ProviderName: "MyProvider"
  Metrics__Providers__0__CounterNames__0: "MyCounter1"
  Metrics__Providers__0__CounterNames__1: "MyCounter2"
  Metrics__Meters__0__MeterName: "MyCustomMeter"
  Metrics__Meters__0__InstrumentNames__0: "MyInstrument"
  Metrics__Meters__1__MeterName: "AnotherMeter"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Metrics__Providers__0__ProviderName
    value: "MyProvider"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__0
    value: "MyCounter1"
  - name: DotnetMonitor_Metrics__Providers__0__CounterNames__1
    value: "MyCounter2"
  - name: DotnetMonitor_Metrics__Meters__0__MeterName
    value: "MyCustomMeter"
  - name: DotnetMonitor_Metrics__Meters__0__InstrumentNames__0
    value: "MyInstrument"
  - name: DotnetMonitor_Metrics__Meters__1__MeterName
    value: "AnotherMeter"
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

## Disable Default Providers

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

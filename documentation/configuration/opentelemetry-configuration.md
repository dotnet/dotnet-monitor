# OpenTelemetry Configuration

`dotnet monitor` can collect diagnostics from a target .NET application and forward them as OpenTelemetry signals (logs, metrics, and traces) to any OTLP-compatible backend. This enables out-of-process telemetry collection without requiring any changes to the target application.

> [!NOTE]
> OpenTelemetry forwarding works in both `Connect` and `Listen` diagnostic port connection modes. See [Diagnostic Port Configuration](./diagnostic-port-configuration.md) for details on each mode.

## Overview

When OpenTelemetry is configured, `dotnet monitor` connects to a target process via EventPipe and subscribes to telemetry event streams. It converts the incoming data into OpenTelemetry SDK objects and exports them via OTLP to a configured backend (e.g., Jaeger, Grafana, Datadog, or any OpenTelemetry Collector).

The three supported signal types are:

- **Logs** — Forwarded from the target application's `ILogger` output
- **Metrics** — Collected from .NET `Meter` and `EventCounter` instruments
- **Traces** — Collected from `System.Diagnostics.ActivitySource` activity data

## Prerequisites

- A [Default Process](./default-process-configuration.md) must be configured so `dotnet monitor` knows which process to collect from.
- An OTLP-compatible backend or collector must be reachable from `dotnet monitor`.

## Exporter Configuration

The exporter defines where telemetry data is sent. Currently, only the OpenTelemetry Protocol (OTLP) exporter is supported.

```json
{
  "OpenTelemetry": {
    "Exporter": {
      "Type": "otlp",
      "Settings": {
        "Defaults": {
          "Protocol": "HttpProtobuf",
          "BaseUrl": "http://localhost:4318"
        }
      }
    }
  }
}
```

| Property | Type | Description |
|---|---|---|
| `Exporter.Type` | string | The exporter type. Currently only `"otlp"` is supported. |
| `Exporter.Settings.Defaults.Protocol` | string | The OTLP protocol to use (e.g., `"HttpProtobuf"`). |
| `Exporter.Settings.Defaults.BaseUrl` | string | The base URL of the OTLP endpoint. Also accepts `Url` as an alternative key. |
| `Exporter.Settings.Defaults.Headers` | object | Optional dictionary of headers to send with OTLP export requests. |

Per-signal endpoint overrides can be configured under `Settings.Logs`, `Settings.Metrics`, and `Settings.Traces`:

```json
{
  "OpenTelemetry": {
    "Exporter": {
      "Type": "otlp",
      "Settings": {
        "Defaults": {
          "BaseUrl": "http://localhost:4318"
        },
        "Logs": {
          "Url": "http://logs-collector:4318/v1/logs"
        }
      }
    }
  }
}
```

## Logs Configuration

Configure log forwarding using the `Logs` section. Categories are specified as a dictionary where the key is the category prefix and the value is the log level. Use the `Default` key to set the default log level.

```json
{
  "OpenTelemetry": {
    "Logs": {
      "Categories": {
        "Default": "Information",
        "Microsoft": "Warning",
        "System.Net.Http": "Debug"
      },
      "IncludeScopes": true,
      "Batch": {
        "ExportIntervalMilliseconds": 5000,
        "MaxQueueSize": 2048,
        "MaxExportBatchSize": 512
      }
    }
  }
}
```

| Property | Type | Description |
|---|---|---|
| `Categories` | dictionary | A dictionary of category prefixes to log levels. The special key `"Default"` sets the default log level for all categories. Valid log levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. Defaults to `Warning` if not specified or invalid. |
| `IncludeScopes` | bool | Whether to include logging scopes in exported log records. Default: `false`. |
| `Batch` | object | Optional batch export settings. See [Batch Options](#batch-options). |

## Metrics Configuration

Configure which meters and instruments to collect using the `Metrics` section. Meters are specified as a dictionary where the key is the meter name and the value is an array of instrument names (use an empty array to collect all instruments from a meter).

```json
{
  "OpenTelemetry": {
    "Metrics": {
      "Meters": {
        "System.Runtime": [],
        "Microsoft.AspNetCore.Hosting": [
          "http.server.request.duration",
          "http.server.active_requests"
        ]
      },
      "PeriodicExporting": {
        "ExportIntervalMilliseconds": 10000
      },
      "MaxHistograms": 2000,
      "MaxTimeSeries": 1000
    }
  }
}
```

| Property | Type | Description |
|---|---|---|
| `Meters` | dictionary | A dictionary where keys are meter names and values are arrays of instrument names to collect. An empty array (`[]`) collects all instruments from that meter. |
| `PeriodicExporting.ExportIntervalMilliseconds` | int | How often (in milliseconds) to export collected metrics. Default: `60000`. |
| `PeriodicExporting.ExportTimeoutMilliseconds` | int | Timeout for each export attempt. Default: `30000`. |
| `PeriodicExporting.AggregationTemporalityPreference` | string | Aggregation temporality: `"Cumulative"` or `"Delta"`. Default: `"Cumulative"`. |
| `MaxHistograms` | int | Maximum number of histogram aggregations to track. Default: `10`. |
| `MaxTimeSeries` | int | Maximum number of time series (unique dimension combinations) to track per metric. Default: `1000`. |

## Tracing Configuration

Configure which activity sources to trace and how sampling is applied using the `Traces` section.

```json
{
  "OpenTelemetry": {
    "Traces": {
      "Sources": [
        "System.Net.Http",
        "Microsoft.AspNetCore"
      ],
      "Sampler": {
        "Type": "ParentBased",
        "Settings": {
          "RootSampler": {
            "Type": "TraceIdRatio",
            "Settings": {
              "SamplingRatio": 0.5
            }
          }
        }
      },
      "Batch": {
        "ExportIntervalMilliseconds": 5000
      }
    }
  }
}
```

The `Sampler` also supports a shorthand format — set it to a double value to use a `ParentBased` sampler with a `TraceIdRatio` root sampler at the specified ratio:

```json
{
  "OpenTelemetry": {
    "Traces": {
      "Sources": ["Microsoft.AspNetCore"],
      "Sampler": 1.0
    }
  }
}
```

| Property | Type | Description |
|---|---|---|
| `Sources` | array | List of `ActivitySource` names to collect traces from. |
| `Sampler` | object or double | Sampling configuration. Use a double (e.g., `1.0`) as shorthand for `ParentBased` with `TraceIdRatio`. Or use the object format with `Type` and `Settings` properties. Supported types: `"ParentBased"`, `"TraceIdRatio"`. |
| `Batch` | object | Optional batch export settings. See [Batch Options](#batch-options). |

## Batch Options

The `Batch` section is available for Logs and Traces. It controls how telemetry records are batched before export.

| Property | Type | Default | Description |
|---|---|---|---|
| `MaxQueueSize` | int | `2048` | Maximum number of items in the export queue. |
| `MaxExportBatchSize` | int | `512` | Maximum number of items per export batch. |
| `ExportIntervalMilliseconds` | int | `5000` | How often (in milliseconds) to export a batch. |
| `ExportTimeoutMilliseconds` | int | `30000` | Timeout for each export attempt. |

## Resource Configuration

Configure resource attributes that are attached to all exported telemetry. Values can reference environment variables from the target process using `${env:VARIABLE_NAME}` syntax.

```json
{
  "OpenTelemetry": {
    "Resource": {
      "service.name": "my-service",
      "service.version": "1.0.0",
      "deployment.environment": "${env:ASPNETCORE_ENVIRONMENT}"
    }
  }
}
```

> [!NOTE]
> The `service.name` and `service.instance.id` attributes are automatically populated from the target process name and process ID if not explicitly set.

## Using Connect Mode

In `Connect` mode (the default), `dotnet monitor` connects to a running process's diagnostic port. No special diagnostic port configuration is needed—just ensure the target process is running and a [Default Process](./default-process-configuration.md) filter is configured.

```json
{
  "DefaultProcess": {
    "Filters": [{
      "ProcessName": "MyApp"
    }]
  },
  "OpenTelemetry": {
    "Exporter": {
      "Type": "otlp",
      "Settings": {
        "Defaults": {
          "BaseUrl": "http://localhost:4318"
        }
      }
    },
    "Metrics": {
      "Meters": {
        "System.Runtime": []
      }
    }
  }
}
```

Start `dotnet monitor`:

```bash
dotnet monitor collect
```

> [!NOTE]
> In `Connect` mode, `dotnet monitor` attaches to the target process after it has started. Telemetry events that occur during process startup will not be captured.

## Using Listen Mode

In `Listen` mode, `dotnet monitor` creates a diagnostic port endpoint and waits for target processes to connect. This enables capturing telemetry from the very start of a process, including assembly load events and early traces.

```json
{
  "DiagnosticPort": {
    "ConnectionMode": "Listen",
    "EndpointName": "/diag/port.sock"
  },
  "DefaultProcess": {
    "Filters": [{
      "ProcessName": "MyApp"
    }]
  },
  "OpenTelemetry": {
    "Exporter": {
      "Type": "otlp",
      "Settings": {
        "Defaults": {
          "BaseUrl": "http://localhost:4318"
        }
      }
    },
    "Logs": {
      "Categories": {
        "Default": "Information"
      }
    },
    "Metrics": {
      "Meters": {
        "System.Runtime": []
      }
    },
    "Traces": {
      "Sources": ["System.Net.Http"]
    }
  }
}
```

Start `dotnet monitor`:

```bash
dotnet monitor collect
```

Configure the target .NET application to connect to `dotnet monitor`:

```bash
export DOTNET_DiagnosticPorts="/diag/port.sock,suspend"
```

> [!IMPORTANT]
> In `Listen` mode with `suspend`, the target process will pause at startup until `dotnet monitor` connects and resumes it. Use `nosuspend` instead if the process should start immediately regardless of whether `dotnet monitor` is available:
> ```bash
> export DOTNET_DiagnosticPorts="/diag/port.sock,nosuspend"
> ```

See [Diagnostic Port Configuration](./diagnostic-port-configuration.md) for more details on connection modes.

## Full Example

The following shows a complete configuration that collects all three signal types and exports them to an OpenTelemetry Collector running locally:

```json
{
  "DiagnosticPort": {
    "ConnectionMode": "Listen",
    "EndpointName": "/diag/port.sock"
  },
  "DefaultProcess": {
    "Filters": [{
      "ProcessName": "MyApp"
    }]
  },
  "OpenTelemetry": {
    "Exporter": {
      "Type": "otlp",
      "Settings": {
        "Defaults": {
          "Protocol": "HttpProtobuf",
          "BaseUrl": "http://localhost:4318"
        }
      }
    },
    "Logs": {
      "Categories": {
        "Default": "Warning",
        "MyApp": "Information"
      },
      "IncludeScopes": true,
      "Batch": {
        "ExportIntervalMilliseconds": 5000
      }
    },
    "Metrics": {
      "Meters": {
        "System.Runtime": [],
        "Microsoft.AspNetCore.Hosting": ["http.server.request.duration"]
      },
      "PeriodicExporting": {
        "ExportIntervalMilliseconds": 10000
      },
      "MaxHistograms": 2000,
      "MaxTimeSeries": 1000
    },
    "Traces": {
      "Sources": [
        "System.Net.Http",
        "Microsoft.AspNetCore"
      ],
      "Sampler": 1.0,
      "Batch": {
        "ExportIntervalMilliseconds": 5000
      }
    }
  }
}
```

## Validation

At least one signal must be configured for OpenTelemetry forwarding to start:

- **Logs** — Set a `Default` entry in `Categories` or provide other category entries
- **Metrics** — Provide at least one entry in `Meters`
- **Traces** — Provide at least one entry in `Sources`

If none of these are configured, `dotnet monitor` will log a warning and wait for a configuration change.

> [!NOTE]
> OpenTelemetry configuration supports dynamic reloading. Changes to the configuration are applied without restarting `dotnet monitor`.

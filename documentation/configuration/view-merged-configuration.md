# View Merged Configuration

`dotnet monitor` includes a diagnostic command that allows you to output the resulting configuration after merging the configuration from all the various sources.

To view the merged configuration, run the following command:

```cmd
dotnet monitor config show
```
The output of the command should resemble the following JSON object:

```json
Tell us about your experience with dotnet monitor: https://aka.ms/dotnet-monitor-survey

{
  "urls": "https://localhost:52323",
  "Kestrel": ":NOT PRESENT:",
  "CorsConfiguration": ":NOT PRESENT:",
  "DiagnosticPort": {
    "ConnectionMode": "Connect",
    "EndpointName": null
  },
  "Metrics": {
    "Enabled": "True",
    "Endpoints": "http://*:52325",
    "IncludeDefaultProviders": "True",
    "MetricCount": "3",
    "Providers": {
      "0": {
        "CounterNames": {
          "0": "connections-per-second",
          "1": "total-connections"
        },
        "ProviderName": "Microsoft-AspNetCore-Server-Kestrel"
      }
    },
  },
  "Storage": {
    "DumpTempFolder": "C:\\Users\\shirh\\AppData\\Local\\Temp\\"
  },
  "Authentication": {
    "MonitorApiKey": {
      "Subject": "2c866b1a-38c5-4454-a686-1e022e38a7f6",
      "PublicKey": ":REDACTED:"
    }
  },
  "Egress": ":NOT PRESENT:"
}
```

To view the loaded configuration providers, run the following command:

```cmd
dotnet-monitor config show --show-sources
```

# Running on a local machine

`dotnet monitor` can be installed as global tool providing observability, diagnostics artifact collection, and triggering in local development and testing scenarios [(more details here)](./setup.md#net-core-global-tool).

### Local authentication options

If you are using `dotnet monitor` as local development tool on Windows you have the option to use Windows Authentication which requires no further configuration, [additional security details can be reviewed here](./authentication.md#windows-authentication).

### Local configuration

In order to efficiently debug a specific local process one of the possible options inclulde defining a default process. This will ensure `dotnet monitor` automatically collects artifacts and logs based on, for example, a specific process name.

Defining a default process on Windows requires define settings filee is in the following user path `%USERPROFILE%\.dotnet-monitor\settings.json`. In the following example `dotnet monitor` default process is configured to look for a process named __BuggyDemoCode__.

```json
{
"$schema": "https://aka.ms/dotnet-monitor-schema",
"DefaultProcess": {
    "Filters": [{
        "Key": "ProcessName",
        "Value": "BuggyDemoWeb"
    }]
    }
}
```

### dotnet monitor collection

To start using `dotnet monitor` run the following command from a Powershell or Command prompt:

```cmd
dotnet-monitor collect
```

Assuming your default process is running you can use the endpoints exposed by `dotnet-monitor` to view metrics, logs or download traces and memory dumps.

When using Windows Authentication, your browser will automatically handle the Windows authentication challenge and as such you can navigate to this endpoints directly. 

```http
/dump 
```

```http
/trace
```

Alternatively, if you are using an API Key, you must [specify it via the Authorization header](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/authentication.md#authenticating-requests), you can accomplish that with CLI tool like CURL.

Artifacts will be eggressed to the local download folder by default ('%USERPROFILE%\Downloads'), however, an [egress file provider](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#filesystem-egress-provider) can be defined in the settings file.

### Triggers


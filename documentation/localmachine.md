# Running on a local machine

`dotnet monitor` can be installed as global tool providing observability, diagnostics artifact collection, and triggering in local development and testing scenarios [(more details here)](./setup.md#net-core-global-tool).

### Local authentication options

If you are using `dotnet monitor` as local development tool on Windows you have the option to use Windows Authentication which requires no additional configuration.

There are a number of local development scenarios that are much more efficient without authentication, `dotnet monitor` can explicitly configured to [disable authentication](./authentication.md#disabling-authentication). Additional [security details can be reviewed here](./authentication.md#windows-authentication).

### Local configuration

To monitor a specific local process you can use the settings file to define a default process. This ensures `dotnet monitor` automatically collects artifacts and logs based on a process criteria you have identified (e.g. process name, processs id, etc.).

Defining a default process on Windows requires creating a settings file in the user path (`%USERPROFILE%\.dotnet-monitor\settings.json`). In the following example the default process has a process name of __BuggyDemoCode__.

```json
{
    "$schema": "https://aka.ms/dotnet-monitor-schema",
    "DefaultProcess": {
        "Filters": [{
            "Key": "ProcessName",
            "Value": "BuggyDemoWeb"
        }]
}
```

### dotnet monitor collection

To start using `dotnet monitor` run the following command from a Powershell or Command prompt:

```cmd
dotnet-monitor collect
```

Assuming your default process is running you can use the endpoints exposed by `dotnet monitor` to view metrics and logs or to download traces and memory dumps. When using Windows Authentication, your browser will automatically handle the Windows authentication challenge and as such you can navigate to these endpoints directly. 

```http
/dump 
```

```http
/trace
```

Alternatively, if you are using an API Key, you must [specify it via the Authorization header](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/authentication.md#authenticating-requests), you can accomplish that with a CLI tool like CURL.

In addition to downloading artifacts directly over HTTP, artifacts can be output to specific local directories by configuring [egress file providers](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#filesystem-egress-provider) in the settings file.

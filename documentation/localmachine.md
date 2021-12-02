# Running on a local machine

`dotnet monitor` can be installed as global tool [(more details here)](./setup.md#net-core-global-tool) providing observability, diagnostics artifact collection, and triggering in local development and testing scenarios.

### Local authentication options

If you are using `dotnet monitor` as local development tool on Windows you have the option to use Windows Authentication which requires no further configuration, [additional security details can be reviewed here](./authentication.md#windows-authentication).


### Local configuration

If you a specific local process to debug one option is to define a default process.This will ensure `dotnet monitor` automatically collects artifacts and logs based on, for example, its process name.

On Windows the settings path is as follows `%USERPROFILE%\.dotnet-monitor\settings.json` and with the following example `dotnet monitor` will filter will attempt to execute commands against a process named __BuggyDemoCode__.

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

Artifacts will be eggressed to the download folder ('%USERPROFILE%\Downloads'), however, an [egress file provider](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md#filesystem-egress-provider) can be defined in the settings file.

### dotnet monitor collection

To start `dotnet monitor` run the following command:

```cmd
dotnet-monitor collect
```


https://localhost:52323/dump

https://localhost:52323/dump&egressProvider=artifactStorage




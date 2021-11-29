# Running on a local machine

`dotnet monitor` can be [installed aa global tool](./setup.md#net-core-global-tool) providing observability, diagnostics artifact collection, and triggering in local development and testing scenarios.

### Local authentication

If you are using `dotnet monitor` as local development tool on Windows you have the option to use Windows Authentication which requires no additional configuration, [additional details here](./authentication.md#windows-authentication).

### Local setup


```cmd
%USERPROFILE%\.dotnet-monitor\settings.json
```

### dotnet mointor collect


```cmd
dotnet-monitor collect
```




https://localhost:52323/processes
https://localhost:52323/gcdump?pid=10356




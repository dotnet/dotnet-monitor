# AKS

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators, such as Kubernetes. To learn more about using `dotnet monitor` with Kubernetes, you can check out our [example deployment](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/kubernetes.md) and our [video tutorial](https://github.com/dotnet/dotnet-monitor/tree/main/samples/AKS_Tutorial). This section covers how to test your development version of `dotnet monitor` in AKS; however, we recommend having some basic experience with AKS before following this workflow.

## What This Workflow Does

This workflow takes your local development copy of `dotnet-monitor`, patches it with a local development copy of the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), and makes it available as an image for you to consume in an ACR (Azure Container Registry). Note that there are many other ways to do this - this is meant to serve as a basic template that can be adapted to match your needs.

1. Open `pwsh` and run the [generate-dev-sln script](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/generate-dev-sln.ps1), providing a path to your local copy of the diagnostics repo.

> [!NOTE]
> If your changes do not involve the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), you don't need to complete this step.

```ps1
cd C:\your-path\dotnet-monitor
.\generate-dev-sln.ps1 C:\your-path\diagnostics
```

2. Publish `dotnet monitor` to your desired (local) target location (TEMP is used throughout this example)

```ps1
dotnet publish .\src\Tools\dotnet-monitor -o $env:TEMP\dotnet-monitor -c Release -f net8.0
```

3. Pull the latest copy of the base image to avoid using a cached version (this should be the same as the REPO used in the next step)

```ps1
docker pull mcr.microsoft.com/dotnet/aspnet:8.0-alpine-amd64
```

4. Add a Dockerfile to the `dotnet monitor` directory created in the previous step. Below is an example of the contents of the Dockerfile:

```dockerfile
ARG REPO=mcr.microsoft.com/dotnet/aspnet
FROM $REPO:8.0-alpine-amd64

WORKDIR /app

ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Unset ASPNETCORE_HTTP_PORTS from aspnet base image (.NET 8+)
    ASPNETCORE_HTTP_PORTS= \
    # Disable debugger and profiler diagnostics to avoid diagnosing self.
    COMPlus_EnableDiagnostics=0 \
    # Default Filter
    DefaultProcess__Filters__0__Key=ProcessId \
    DefaultProcess__Filters__0__Value=1 \
    # Remove Unix Domain Socket before starting diagnostic port server
    DiagnosticPort__DeleteEndpointOnStartup=true \
    # Server GC mode
    DOTNET_gcServer=1 \
    # Logging: JSON format so that analytic platforms can get discrete entry information
    Logging__Console__FormatterName=simple \
    # Logging: Use round-trip date/time format without timezone information (always logged in UTC)
    Logging__Console__FormatterOptions__TimestampFormat=yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z' \
    # Logging: Write timestamps using UTC offset (+0:00)
    Logging__Console__FormatterOptions__UseUtcTimestamp=true \
    # Add dotnet-monitor path to front of PATH for easier, prioritized execution
    PATH="/app:${PATH}"

COPY . .

ENTRYPOINT [ "dotnet-monitor" ]
CMD [ "collect", "--urls", "http://+:52323", "--metricUrls", "http://+:52325" ]

```

5. Log in to your ACR

```ps1
az account set -s <subscription_id>
az aks get-credentials --resource-group <name_of_resource_group> --name <name_of_aks>
az acr login --resource-group <name_of_resource_group> --name <name_of_acr>
```

6. Build the Docker image locally

```ps1
docker build $env:TEMP\dotnet-monitor -f $env:TEMP\dotnet-monitor\Dockerfile.localagent -t <name_of_acr>.azurecr.io/localagent
```

7. Push the Docker image to your ACR

```ps1
docker push <name_of_acr>.azurecr.io/localagent:latest
```

8. Update the aks deployment file to use your image instead of the published version of `dotnet monitor`

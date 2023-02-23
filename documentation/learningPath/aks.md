
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Faks)

# AKS

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators, such as Kubernetes. To learn more about using `dotnet monitor` with Kubernetes, you can check out our [example deployment](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/kubernetes.md) and our [video tutorial](https://github.com/dotnet/dotnet-monitor/tree/main/samples/AKS_Tutorial). This section covers how to test your development version of `dotnet monitor` in AKS; however, we recommend having some basic experience with AKS before following this workflow.

## What This Workflow Does

This workflow takes your local development copy of `dotnet-monitor`, patches it with a local development copy of the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), and makes it available as an image for you to consume in an ACR (Azure Container Registry). Note that there are many other ways to do this - this is meant to serve as a basic template that can be adapted to match your needs.

1. Open `Powershell` and run the [generate-dev-sln script](https://github.com/dotnet/dotnet-monitor/blob/main/generate-dev-sln.ps1), providing a path to your local copy of the diagnostics repo.

>**Note**: If your changes do not involve the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), you don't need to complete this step.

```bash
cd C:\your-path\dotnet-monitor 
.\generate-dev-sln.ps1 C:\your-path\diagnostics
```

2. Publish `dotnet monitor` to your desired (local) target location (TEMP is used throughout this example)

```bash
dotnet publish .\src\Tools\dotnet-monitor -o $env:TEMP\dotnet-monitor -c Release -f net6.0
```

3. Add a Dockerfile to the `dotnet monitor` directory created in the previous step. Below is an example of the contents of the Dockerfile:

```bash
FROM mcr.microsoft.com/dotnet/nightly/aspnet:6.0-alpine 
ENV COMPlus_EnableDiagnostics 0
ENV ASPNETCORE_URLS=
ENV DOTNET_MONITOR_VERSION=6
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "dotnet-monitor.dll", "collect", "--no-auth"]
```

4. Log in to your ACR

```bash
az account set --subscription xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
az aks get-credentials --resource-group <name_of_resource_group> --name <name_of_aks>
az acr login --resource-group <name_of_resource_group> --name <name_of_acr>
```

5. Build the Docker Image Locally

```bash
docker build $env:TEMP\dotnet-monitor -f $env:TEMP\dotnet-monitor\Dockerfile.localagent -t name_of_acr.azurecr.io/localagent
```

6. Push the Docker Image To Your ACR

```bash
docker push name_of_acr.azurecr.io/localagent:latest
```

7. Update the Dockerfile to use your image instead of the published version of `dotnet monitor`

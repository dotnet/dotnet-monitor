
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Faks)

# AKS

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators, such as Kubernetes. To learn more about using `dotnet monitor` with Kubernetes, you can check out our [example deployment](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/kubernetes.md) and our [video tutorial](https://github.com/dotnet/dotnet-monitor/tree/main/samples/AKS_Tutorial). This section covers how to test your development version of `dotnet monitor` in AKS; however, we recommend having some basic experience with AKS before following this workflow.

## What This Workflow Does

This workflow takes your local development copy of `dotnet-monitor`, patches it with a local development copy of the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), and makes it available as an image for you to consume in an ACR (Azure Container Registry). Note that there are many other ways to do this - this is meant to serve as a basic template that can be adapted to match your needs.

## Patching `dotnet monitor`

>**Note**: If your changes do not involve the [.NET Core Diagnostics Repo](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo), feel free to skip this step.

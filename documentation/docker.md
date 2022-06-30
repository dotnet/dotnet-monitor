# Running in Docker

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators.

## Dockerfile Source

### Shipping Dockerfile

| Version | Platform | Architecture | Link |
|---|---|---|---|
| 7.0 (Preview) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/7.0/alpine/amd64/Dockerfile |
| 7.0 (Preview) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/7.0/alpine/arm64v8/Dockerfile |
| 6.2 (Current, Latest) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.2/alpine/amd64/Dockerfile |
| 6.2 (Current, Latest) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.2/alpine/arm64v8/Dockerfile |
| 6.1 | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.1/alpine/amd64/Dockerfile |

### Nightly Dockerfiles

| Version | Platform | Architecture | Link |
|---|---|---|---|
| 7.0 (Preview, Latest) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/alpine/amd64/Dockerfile |
| 7.0 (Preview, Latest) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/alpine/arm64v8/Dockerfile |
| 6.2 (Current) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.2/alpine/amd64/Dockerfile |
| 6.2 (Current) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.2/alpine/arm64v8/Dockerfile |
| 6.1 | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.1/alpine/amd64/Dockerfile |

## Image Repositories

### Shipping Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/monitor/tags/list

### Nightly Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-nightly-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list

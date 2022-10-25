
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fdocker)

# Running in Docker

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators.

## Dockerfile Source

### Shipping Dockerfile

| Version | Platform | Architecture | Link |
|---|---|---|---|
| 7.0 (RC) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/7.0/alpine/amd64/Dockerfile |
| 7.0 (RC) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/7.0/alpine/arm64v8/Dockerfile |
| 6.3 (Current, Latest) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.3/alpine/amd64/Dockerfile |
| 6.3 (Current, Latest) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.3/alpine/arm64v8/Dockerfile |
| 6.2 | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.2/alpine/amd64/Dockerfile |
| 6.2 | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/main/src/monitor/6.2/alpine/arm64v8/Dockerfile |

### Nightly Dockerfiles

| Version | Platform | Architecture | Link |
|---|---|---|---|
| 7.0 (RC, Latest) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/alpine/amd64/Dockerfile |
| 7.0 (RC, Latest) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/alpine/arm64v8/Dockerfile |
| 7.0 (RC, Latest) | Linux (Ubuntu, Chiseled) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/ubuntu-chiseled/amd64/Dockerfile |
| 7.0 (RC, Latest) | Linux (Ubuntu, Chiseled) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/7.0/ubuntu-chiseled/arm64v8/Dockerfile |
| 6.3 (Current) | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.3/alpine/amd64/Dockerfile |
| 6.3 (Current) | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.3/alpine/arm64v8/Dockerfile |
| 6.3 (Current) | Linux (Ubuntu, Chiseled) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.3/ubuntu-chiseled/amd64/Dockerfile |
| 6.3 (Current) | Linux (Ubuntu, Chiseled) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.3/ubuntu-chiseled/arm64v8/Dockerfile |
| 6.2 | Linux (Alpine) | amd64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.2/alpine/amd64/Dockerfile |
| 6.2 | Linux (Alpine) | arm64 | https://github.com/dotnet/dotnet-docker/blob/nightly/src/monitor/6.2/alpine/arm64v8/Dockerfile |

## Image Repositories

### Shipping Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/monitor/tags/list

### Nightly Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-nightly-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list

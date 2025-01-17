# Running in Docker

In addition to its availability as a .NET CLI tool, the `dotnet monitor` tool is available as a prebuilt Docker image that can be run in container runtimes and orchestrators. See [Releases](releases.md) for information on how long each image version will be supported.

## Dockerfile Source
- [Shipping Dockerfiles](https://github.com/dotnet/dotnet-docker/tree/main/src/monitor)
- [Nightly Dockerfiles](https://github.com/dotnet/dotnet-docker/tree/nightly/src/monitor)

## Image Repositories

### Shipping Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/monitor/tags/list

### Nightly Repository
- Docker Hub: https://hub.docker.com/_/microsoft-dotnet-nightly-monitor
- Microsoft Container Registry: https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list

## Sample Usage

- For a Docker Compose sample, see [Running in Docker Compose](./docker-compose.md)
- For a Kubernetes sample, see [Running in Kubernetes](./kubernetes.md)
# Alpine Dev Container

The current codespace is using an Alpine dev container, which is only recommended if you need to test changes to `dotnet-monitor` on musl libc. For most other workflows the [Debian dev container](../devcontainer.json) is recommended instead as it has more tooling available.

## The following feature are **NOT** currently supported in `dotnet-monitor` Alpine dev containers
- docker-in-docker
- az cli
- nodejs
- kubectl, helm, and minikube
- powershell

# 📖 `dotnet monitor` documentation

`dotnet monitor` is a tool that makes it easier to get access to diagnostics information in a dotnet process.

When running a dotnet application, differences in diverse local and production environments can make collecting diagnostics artifacts (e.g., logs, traces, process dumps) challenging. `dotnet monitor` aims to simplify the process by exposing a consistent HTTP API regardless of where your application is run.

## Table of contents

- [Setup](./setup.md)
- [Getting Started](#)
    - [Running on a local machine](#)
    - [Running in Docker](#)
    - [Running in a kubernetes cluster](./kubernetes.md)
    - [Enabling SSL](#)
- [API Endpoints](./api/README.md)
    - [OpenAPI document](./openapi.json)
    - [`/processes`](./api/processes.md)
    - [`/dump`](./api/dump.md)
    - [`/gcdump`](./api/gcdump.md)
    - [`/trace`](./api/trace.md)
    - [`/metrics`](./api/metrics.md)
    - [`/livemetrics`](./api/livemetrics.md)
    - [`/logs`](./api/logs.md)
    - [`/info`](./api/info.md)
    - [`/operations`](.api/operations.md)
- [Configuration](./configuration.md)
    - [JSON Schema](./schema.json)
- [Authentication](./authentication.md)
    - [API Key (Recommended)](./authentication.md#api-key-authentication)
    - [Windows](./authentication.md#windows-authentication)
- [Collection Rules](./collectionrules.md)
- [Egress Providers](./egress.md)
- [Troubleshooting](./troubleshooting.md)
- [Clone, build, and test the repo](./building.md)
- [Official Build Instructions](./official-build-instructions.md)
- [Release Process](./release-process.md)

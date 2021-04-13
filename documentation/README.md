# 📖 `dotnet-monitor` documentation

`dotnet-monitor` is a tool that makes it easier to get access to diagnostics information in a dotnet process.

When running a dotnet application differences in diverse local and production environments can make collecting diagnostics artifacts (e.g., logs, traces, process dumps) challenging. `dotnet-monitor` aims to simplify the process by exposing a consistent HTTP API regardless of where your application is run.

## Table of contents

- [Setup](./setup.md)
- [Getting Started](#)
    - [Running on a local machine](#)
    - [Running in Docker](#)
    - [Running in a kubernetes cluster](#)
- [API Endpoints](#)
    - [OpenAPI document](./openapi.json)
    - [`/processes`](#)
    - [`/dump`](#)
    - [`/gcdump`](#)
    - [`/trace`](#)
    - [`/metrics`](#)
    - [`/logs`](#)
- [Configuration](#)
    - [JSON Schema](./schema.json)
- [Authentication](#)
    - [Windows](#)
    - [API Key](#)
- [Egress Providers](#)
- [Troubleshooting](#)
- [Clone, build, and test the repo](./building.md)
- [Official Build Instructions](./official-build-instructions.md)
# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.238.1/containers/dotnet/.devcontainer/base.Dockerfile

ARG VARIANT="6.0-bullseye"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

RUN apt update && export DEBIAN_FRONTEND=noninteractive \
     && apt install -y --no-install-recommends cmake clang

# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.238.1/containers/dotnet/.devcontainer/base.Dockerfile

ARG VARIANT="6.0-bullseye"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

RUN apt update && export DEBIAN_FRONTEND=noninteractive \
     && apt install -y --no-install-recommends cmake clang llvm

ARG INSTALL_NODE="true"
ARG INSTALL_AZURITE="true"
ARG NODE_VERSION="lts/*"

RUN if [ "${INSTALL_NODE}" = "true" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi
RUN if [ "${INSTALL_AZURITE}" = "true" ]; then npm install -g azurite; fi

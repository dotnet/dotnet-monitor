#!/usr/bin/env bash

set -e

# Install SDK and tool dependencies before container starts
# Also run the full restore on the repo so that go-to definition
# and other language features will be available in C# files
./restore.sh -ci

# Add the .NET dev certs by default for dotnet-monitor's usage on launch.
# Do **NOT** do this in base images.
dotnet dev-certs https

# Install ytt
repoRoot=$(git rev-parse --show-toplevel)
tmpDir=$(mktemp -d)
pushd "$tmpDir"
curl -sSLO https://github.com/vmware-tanzu/carvel-ytt/releases/download/v0.44.1/ytt-linux-amd64
shasum -a 256 -c "$repoRoot/.devcontainer/shared/ytt-linux-amd64.sha"
chmod +x ./ytt-linux-amd64
sudo -n mv ./ytt-linux-amd64 /usr/local/bin/ytt
popd

# The container creation script is executed in a new Bash instance
# so we exit at the end to avoid the creation process lingering.
exit

# Release Process

## Merge to Release Branch

1. Merge from the `main` branch to the appropriate release branch (e.g. `release/5.0`).
1. In `/eng/Versions.props`, update `dotnet/diagnostics` dependencies to versions from the corresponding release of the `dotnet/diagnostics` repo.
1. In `/eng/Version.props`, ensure that `<BlobGroupBuildQuality>` is set appropriately. See the documentation next to this setting for the appropriate values. In release branches, its value should either be `prerelease` or `release`. This setting, in combination with the version settings, determine for which 'channel' the aks.ms links are created.
4. Complete at least one successful build.

## Build Release Branch

The official build will not automatically trigger for release branches. Each time a new build is needed, the pipeline will need to be invoked manually.

1. Wait for changes to be mirrored from [GitHub repository](https://github.com/dotnet/dotnet-monitor) to the [internal repository](https://dev.azure.com/dnceng/internal/_git/dotnet-dotnet-monitor).
1. Invoke the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) for the release branch.

The result of the successful build pushes packages to the [dotnet-tools](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json) feed, pushes symbols to symbol feeds, and generates aka.ms links for the following:
- `aka.ms/dotnet/diagnostics/monitor{channel}/dotnet-monitor.nupkg.version`
- `aka.ms/dotnet/diagnostics/monitor{channel}/dotnet-monitor.nupkg.sha512`

The `channel` value is used by the `dotnet-docker` repository to consume the correct latest version. This value is:
- `{major}.{minor}/daily` for builds from non-release branches. For example, `channel` is `5.0/daily` for the `main` branch.
- `{major}.{minor}/{preReleaseVersionLabel}.{preReleaseVersionIteration}` for non-final releases in release branches. For example, `channel` is `5.0/preview.5` for the `release/5.0` branch.
- `{majorVersion}.{minorVersion}/release` for final release in release branches. For example, `channel` is `5.0/release` for the `release/5.0` if its `<BlobGroupBuildQuality>` is set to `release`.

## Update Nightly Docker Ingestion

### Update Pipeline Variable for Release

The `dotnet-docker` repository runs an update process each day that detects the latest version of a given `dotnet-monitor` channel. During the stabilization/testing/release period for a release of `dotnet-monitor`, the update process should be changed to pick up builds for the release branch.

The `monitorChannel` pipeline variable of the [dotnet-docker-update-dependencies](https://dev.azure.com/dnceng/internal/_build?definitionId=470) pipeline instructs the update process of which channel to use in order to update the `nightly` branch. Normally, its value is `6.0/daily` to pull the latest daily build. However, during the stabilization and release of dotnet-monitor, it should be set to the prerelease channel (e.g. `6.0/preview.8`, `6.0/rc.1`) or release channel (e.g. `6.0/release`) value.

### Revert Pipeline Variable After Release

After the release has been completed, this pipeline variable should be changed to the appropriate daily channel (e.g. `6.0/daily`).

### Image Update Process

The `dotnet-docker` repository typically updates the `nightly` branch with newer versions each morning by creating a pull request that targets the `nightly` branch with the new version information which needs to be approved by the `dotnet-docker` team. Upon completion, the `nightly` branch build will automatically run and create the new nightly images.

The nightly image is `mcr.microsoft.com/dotnet/nightly/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list.

## Stabilization

1. Fix issues for the release in the release branch. Backport fixes to `main` branch and other prior release branches as needed.
1. Invoke [build](<#Build Release Branch>) pipeline as needed.
1. After successful build, test changes from [dotnet-tools](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json) feed. Images from the `nightly` branch of the `dotnet-docker` repository will be recreated the next day after the successful build of the release branch.

## Release to nuget.org and Add GitHub Release

1. Grab the file [/documentation/releaseNotes/releaseNotes.md](https://github.com/dotnet/dotnet-monitor/blob/release/6.0/documentation/releaseNotes/releaseNotes.md) from the release branch and check this file into [main](https://github.com/dotnet/dotnet-monitor/tree/main) as [/documentation/releaseNotes/releaseNotes.v{NugetVersionNumber}.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/releaseNotes/releaseNotes.v6.0.0-preview.8.21503.3.md).
>**Note:** this file comes from the **release** branch and is checked into the **main** branch.
2. Start [release pipeline](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=105). During creation of the release you must select the dotnet-monitor build to release from the list of available builds. This must be a build with the tag `MonitorRelease` and the associated `MonitorRelease` artifact.
3. The release will start the stage "Pre-Release Verification" this will check that the above steps were done as expected.
4. Approve the sign-off step the day before the release after 8:45 AM PDT, when ready to publish.
>**Note:** After sign-off of the "Pre-Release Verification" environment the NuGet and GitHub release steps will automatically wait 8:45 AM PDT the next day to correspond with the typical docker release time of 9:00 AM PDT.

The remainder of the release will automatically push NuGet packages to nuget.org, [tag](https://github.com/dotnet/dotnet-monitor/tags) the commit from the build with the release version, and add a new [GitHub release](https://github.com/dotnet/dotnet-monitor/releases).

## Release Docker Images

1. Contact `dotnet-docker` team with final version that should be released. This version should be latest version in the `nightly` branch.
1. The `dotnet-docker` team will merge from `nightly` branch to `main` branch and wait for `dotnet-monitor` team approval. Typically, these changes are completed the day before the release date.
1. The `dotnet-docker` team will start the build ahead of the release and wait for the all-clear from `dotnet-monitor` team before publishing the images.

The release image is `mcr.microsoft.com/dotnet/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/monitor/tags/list.
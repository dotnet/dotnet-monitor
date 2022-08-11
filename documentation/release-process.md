# Release Process

## Merge to Release Branch

1. Merge from the `main` branch to the appropriate release branch (e.g. `release/5.0`). Note that for patch releases, fixes should be made directly to the appropriate release branch and we do not merge from the `main` branch. Note that it is acceptable to use a release/major.x branch. Alternatively, you can create a new release branch for the minor version. See [additional branch steps](#additional-steps-when-creating-a-new-release-branch) below.

1. In `/eng/Versions.props`, update `dotnet/diagnostics` dependencies to versions from the corresponding release of the `dotnet/diagnostics` repo. Note this should be done using darc. See [updating dependencies](#updating-dependencies).
1. In `/eng/Version.props`, ensure that `<BlobGroupBuildQuality>` is set appropriately. See the documentation next to this setting for the appropriate values. In release branches, its value should be `release`. This setting, in combination with the version settings, determine for which 'channel' the aks.ms links are created. You may also need to update `PreReleaseVersionLabel` to `rtm`, the `DotnetFinalVersionKind` to 'release' or 'servicing' and remove the `PreReleaseVersionIteration`. See https://github.com/dotnet/dotnet-monitor/pull/1970/files for an example.

1. Complete at least one successful [release build](#build-release-branch).
1. [Update dotnet-docker pipeline variables](#update-pipeline-variable-for-release) to pick up builds from the release branch.
1. Bump the version number in the `main` branch and reset release notes. [Example Pull Request](https://github.com/dotnet/dotnet-monitor/pull/1560). 

## Additional steps when creating a new release branch

1. When creating a new release branch (such as `release/8.x`) there are additional steps that need to be taken.
1. Follow [these steps](https://github.com/dotnet/arcade/blob/main/Documentation/Darc.md#setting-up-your-darc-client) to install darc.
1. Be sure to call [darc authenticate](https://github.com/dotnet/arcade/blob/main/Documentation/Darc.md#authenticate). You will need to create the requested tokens.
1. You will need to add the branch to a channel. E.g.
`darc add-default-channel --channel ".NET Core Tooling Release" --branch release/8.x --repo https://github.com/dotnet/dotnet-monitor`

- It can be helpful to create test release branches (e.g. release/test/8.x). Note these branches will trigger warnings because they are considered unprotected release branches and should be deleted as soon as possible.
- If you created a build from a newly created release branch without a channel, you will get the message 'target build already exists on all channels'. To use this build you need to add it to a channel: `darc add-build-to-channel --id <Build BAR ID> --channel "General Testing"`.

## Build Release Branch

The official build will not automatically trigger for release branches. Each time a new build is needed, the pipeline will need to be invoked manually.

1. Wait for changes to be mirrored from [GitHub repository](https://github.com/dotnet/dotnet-monitor) to the [internal repository](https://dev.azure.com/dnceng/internal/_git/dotnet-dotnet-monitor).
1. Invoke the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) for the release branch.
1. Bump the versions across feature branches. See https://github.com/dotnet/dotnet-monitor/pull/1973/files for an example.

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

**Known issues**
* You may not have permissions to change these variables.
* Currently docker only supports updating one major version. We have to manually update any additional versions. See [instructions](#manually-updating-docker-versions) for manually updating.

The following variables for [dotnet-docker-update-dependencies](https://dev.azure.com/dnceng/internal/_build?definitionId=470) need to be updated for release:
* `monitorXMinorVersion`: Make sure these are set to the correct values.
* `monitorXQuality`: Normally this is daily, but should be set to release.
* `monitorXStableBranding`: Normally this is false, but should be set to true when the package version is stable e.g. `dotnet-monitor.8.0.0.nupkg` (does not have a prerelease label on it such as `-preview.X` or `-rtm.X`).
* `update-monitor-enabled`: Make sure this is true.
* `update-dotnet-enabled`: When doing an ad-hoc run, make sure to **disable** this.

### Updating tags

If you are releasing a new minor version, you may need to update the current/preview tags as well as the shared tag pool.
1. Update https://github.com/dotnet/dotnet-docker/blob/nightly/eng/mcr-tags-metadata-templates/monitor-tags.yml.
1. Update https://github.com/dotnet/dotnet-docker/blob/nightly/manifest.json.
1. Run update-dependencies as described [here](#manually-updating-docker-versions).
1. See https://github.com/dotnet/dotnet-docker/pull/3830/files for an example.

### Revert Pipeline Variable After Release

After the release has been completed, this pipeline variable should be changed to the appropriate daily channel (e.g. `6.0/daily`).

### Manually updating docker versions
1. Run `\eng\Set-DotnetVersions.ps1`. Example:
``` powershell
.\Set-DotnetVersions.ps1 6.1 -MonitorVersion 6.1.2-servicing.22306.3 -UseStableBranding
.\Set-DotnetVersions.ps1 6.2 -MonitorVersion 6.2.0-rtm.22306.2 -UseStableBranding
.\Set-DotnetVersions.ps1 7.0 -MonitorVersion 7.0.0-preview.5.22306.5
```
1. See https://github.com/dotnet/dotnet-docker/pull/3828 for sample result.

### Updating dependencies

If necessary, update dependencies in the release branch. Most commonly this means picking up a new version of diagnostics packages.

1. For new branches only, you need to setup a subscription using darc: `darc add-subscription --channel ".NET Core Tooling Release" --source-repo https://github.com/dotnet/diagnostics --target-repo https://github.com/dotnet/dotnet-monitor --target-branch release/8.x --update-frequency None --standard-automerge`
1. Use `darc get-subscriptions --target-repo monitor` to see existing subscriptions.
1. Use `darc trigger-subscriptions` to trigger an update. This will create a pull request that will update the Versions.details.xml file.

### Image Update Process

The `dotnet-docker` repository typically updates the `nightly` branch with newer versions each morning by creating a pull request that targets the `nightly` branch with the new version information which needs to be approved by the `dotnet-docker` team. Upon completion, the `nightly` branch build will automatically run and create the new nightly images.

The nightly image is `mcr.microsoft.com/dotnet/nightly/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list.

## Stabilization

1. Fix issues for the release in the release branch. Backport fixes to `main` branch and other prior release branches as needed.
1. Invoke [build](#build-release-branch) pipeline as needed.
1. After successful build, test changes from [dotnet-tools](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json) feed. Images from the `nightly` branch of the `dotnet-docker` repository will be recreated the next day after the successful build of the release branch.

## Release to nuget.org and Add GitHub Release

1. Grab the file [/documentation/releaseNotes/releaseNotes.md](https://github.com/dotnet/dotnet-monitor/blob/release/6.0/documentation/releaseNotes/releaseNotes.md) from the release branch and check this file into [main](https://github.com/dotnet/dotnet-monitor/tree/main) as [/documentation/releaseNotes/releaseNotes.v{NugetVersionNumber}.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/releaseNotes/releaseNotes.v6.0.0-preview.8.21503.3.md).
>**Note:** this file comes from the **release** branch and is checked into the **main** branch.
2. Start [release pipeline](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=105). Allow the stages to trigger automatically (do not check the boxes in the associated dropdown). During creation of the release you must select the dotnet-monitor build to release from the list of available builds. This must be a build with the tag `MonitorRelease` and the associated `MonitorRelease` artifact (set `dotnet-monitor_build` to the pipeline run of `dotnet monitor` that is being released; set `dotnet-monitor_source` to the latest commit from `main`).
3. The release will start the stage "Pre-Release Verification"; this will check that the above steps were done as expected. The name of the release will be updated automatically.
4. Approve the sign-off step the day before the release after 8:45 AM PDT, when ready to publish.
>**Note:** After sign-off of the "Pre-Release Verification" environment the NuGet and GitHub release steps will automatically wait until 8:45 AM PDT the next day to correspond with the typical docker release time of 9:00 AM PDT.

The remainder of the release will automatically push NuGet packages to nuget.org, [tag](https://github.com/dotnet/dotnet-monitor/tags) the commit from the build with the release version, and add a new [GitHub release](https://github.com/dotnet/dotnet-monitor/releases).

## Release to Storage Accounts

1. Approximately 3 days before Docker image release, execute a dry-run of the [dotnet-monitor-release](https://dev.azure.com/dnceng/internal/_build?definitionId=1103) pipeline (`Branch` should be set to `main`; `IsDryRun` should be checked; uncheck `IsTestRun`; under `Resources`, select the `dotnet monitor` build from which assets will be published). This will validate that the nupkg files can be published to the `dotnetcli` storage account and checksums can be published to the `dotnetclichecksums` storage account.
1. The day before Docker image release, execute run of the [dotnet-monitor-release](https://dev.azure.com/dnceng/internal/_build?definitionId=1103) pipeline (`Branch` should be set to `main`; uncheck `IsDryRun`; uncheck `IsTestRun`; under `Resources`, select the `dotnet monitor` build from which assets will be published). This will publish the nupkg files to the `dotnetcli` storage account and the checksums to the `dotnetclichecksums` storage account.

## Release Docker Images

1. Contact `dotnet-docker` team with final version that should be released. This version should be latest version in the `nightly` branch.
1. Docker image build from main branch requires assets to be published to `dotnetcli` and `dotnetclichecksums` storage accounts. See [Release to Storage Accounts](#release-to-storage-accounts).
1. The `dotnet-docker` team will merge from `nightly` branch to `main` branch and wait for `dotnet-monitor` team approval. Typically, these changes are completed the day before the release date.
1. The `dotnet-docker` team will start the build ahead of the release and wait for the all-clear from `dotnet-monitor` team before publishing the images.

The release image is `mcr.microsoft.com/dotnet/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/monitor/tags/list.

## After the Release

1. Update [releases.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/releases.md) with the latest version.
1. When necessary, update [docker.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/docker.md).
1. When necessary, update this document if its instructions were unclear or incorrect.
1. When releasing a new minor version, include an announcement that the previous version will soon be out of support. For example, https://github.com/dotnet/dotnet-monitor/discussions/1871
1. Make sure you [Revert](#revert-pipeline-variable-after-release) the nightly build pipeline.


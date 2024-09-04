# Release Process

## Prepare the release branch

1. Update the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) variables to prevent consumption of nightly builds into dotnet-docker. Update `AutoUpdateDockerBranches` variable on the pipeline itself, not just on a new build, to the list of branch references (e.g. `refs/heads/main`) that you want to automatically update dotnet/dotnet-docker when scheduled builds are run; likely want to clear the variable when preparing for release.
1. Merge from the `main` branch to the appropriate release branch (e.g. `release/5.0`). Note that for patch releases, fixes should be made directly to the appropriate release branch and we do not merge from the `main` branch. Note that it is acceptable to use a release/major.x branch. Alternatively, you can create a new release branch for the minor version. See [additional branch steps](#additional-steps-when-creating-a-new-release-branch) below.
1. Review and merge in any outstanding dependabot PRs for the release branch.
1. Run the [Update release version](https://github.com/dotnet/dotnet-monitor/actions/workflows/update-release-version.yml) workflow, setting `Use workflow from` to the release branch and correctly setting the `Release type` and `Release version` options. (*NOTE:* Release version should include only major.minor.patch, without any extra labels). Review and merge in the PR created by this workflow.
1. If you merged from `main` in step 1, repeat the above step for the `main` branch with the appropriate `Release type` and `Release version`.
1. Ensure dependencies are updated appropriately e.g. the corresponding .NET versions are flowed into the release branch. See [Updating dependencies](#updating-dependencies).
1. Complete at least one successful [release build](#build-release-branch).

## Additional steps when creating a new release branch

1. When creating a new release branch (such as `release/8.x`) there are additional steps that need to be taken.
1. Follow [these steps](https://github.com/dotnet/arcade/blob/main/Documentation/Darc.md#setting-up-your-darc-client) to install darc.
1. Be sure to call [darc authenticate](https://github.com/dotnet/arcade/blob/main/Documentation/Darc.md#authenticate). You will need to create the requested tokens.
1. You will need to add the branch to a channel. E.g.
`darc add-default-channel --channel ".NET Core Tooling Release" --branch release/8.x --repo https://github.com/dotnet/dotnet-monitor`
1. Ensure that `UseMicrosoftDiagnosticsMonitoringShippedVersion` is set appropriately. See [Updating dependencies](#updating-dependencies).
1. Ensure that dependabot configuration is updated at [../.github/dependabot.template.yml](../.github/dependabot.template.yml).

- It can be helpful to create test release branches (e.g. release/test/8.x). Note these branches will trigger warnings because they are considered unprotected release branches and should be deleted as soon as possible.
- If you created a build from a newly created release branch without a channel, you will get the message 'target build already exists on all channels'. To use this build you need to add it to a channel: `darc add-build-to-channel --id <Build BAR ID> --channel "General Testing"`.

## Updating dependencies

If necessary, update dependencies in the release branch.
> [!NOTE]
> This is typically not needed for the diagnostics packages. They are kept up-to-date by dependabot if `UseMicrosoftDiagnosticsMonitoringShippedVersion` in [../eng/Versions.props](../eng/Versions.props) is set to `true`. It might be set to `false` if feature development requiring unreleased diagnostics libraries was merged into the branch. Official releases should use the released diagnostics libraries per agreed upon policy.

1. For new branches only, you need to setup a subscription using darc: `darc add-subscription --channel ".NET Core Tooling Release" --source-repo https://github.com/dotnet/diagnostics --target-repo https://github.com/dotnet/dotnet-monitor --target-branch release/8.x --update-frequency None --standard-automerge`
1. Use `darc get-subscriptions --target-repo monitor` to see existing subscriptions.
1. Use `darc trigger-subscriptions` to trigger an update. This will create a pull request that will update the Versions.details.xml file.
1. Sometimes an existing subscription needs to be updated. For example, when updating from Preview 5 to Preview 6:

```
darc get-subscriptions --target-repo https://github.com/dotnet/dotnet-monitor --target-branch release/8.x
https://github.com/dotnet/installer (.NET 8.0.1xx SDK Preview 5) ==> 'https://github.com/dotnet/dotnet-monitor' ('release/8.x')
  - Id: 2f528213-5355-43ec-0bf5-08db410c84fe

darc update-subscription --id 2f528213-5355-43ec-0bf5-08db410c84fe

darc get-subscriptions --target-repo https://github.com/dotnet/dotnet-monitor --target-branch release/8.x
https://github.com/dotnet/installer (.NET 8.0.1xx SDK Preview 6) ==> 'https://github.com/dotnet/dotnet-monitor' ('release/8.x')
  - Id: 2f528213-5355-43ec-0bf5-08db410c84fe

```

## Build Release Branch

The official build will not automatically trigger for release branches. Each time a new build is needed, the pipeline will need to be invoked manually.

1. Wait for changes to be mirrored from [GitHub repository](https://github.com/dotnet/dotnet-monitor) to the [internal repository](https://dev.azure.com/dnceng/internal/_git/dotnet-dotnet-monitor).
1. Invoke the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) for the release branch. Make sure the `Update dotnet-docker?` parameter is set to true. Setting this will cause a successful build to trigger an update in the `dotnet-docker` repository.

> [!NOTE]
> If the release is part of security servicing, build the `internal/release/*` branch instead of the `release/*` branch and set `Update dotnet-docker?` parameter to false. Ensure that all public changes have been mirrored into the release branch before starting the build.

## Update Nightly Docker Ingestion

### Updating tags

If you are releasing a new minor version, you may need to update the current/preview tags as well as the shared tag pool.
1. Update https://github.com/dotnet/dotnet-docker/blob/nightly/eng/mcr-tags-metadata-templates/monitor-tags.yml.
1. Update https://github.com/dotnet/dotnet-docker/blob/nightly/manifest.json.
1. Run update-dependencies as described [here](#manually-updating-docker-versions).
1. See https://github.com/dotnet/dotnet-docker/pull/3830/files for an example.

### Manually updating docker versions

> [!NOTE]
> This only applies for public releases e.g. ones from a `release/*` branch.

1. Run `\eng\Set-DotnetVersions.ps1`. Example:
``` powershell
.\Set-DotnetVersions.ps1 6.1 -MonitorVersion 6.1.2-servicing.22306.3
.\Set-DotnetVersions.ps1 6.2 -MonitorVersion 6.2.0-rtm.22306.2
.\Set-DotnetVersions.ps1 7.0 -MonitorVersion 7.0.0-preview.5.22306.5
```
1. See https://github.com/dotnet/dotnet-docker/pull/3828 for sample result.

### Image Update Process

The `dotnet-docker` repository typically updates the `nightly` branch with newer versions each morning by creating a pull request that targets the `nightly` branch with the new version information which needs to be approved by the `dotnet-docker` team. Upon completion, the `nightly` branch build will automatically run and create the new nightly images.

The nightly image is `mcr.microsoft.com/dotnet/nightly/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/nightly/monitor/tags/list.

## Stabilization

1. Fix issues for the release in the release branch. Backport fixes to `main` branch and other prior release branches as needed.
1. Invoke [build](#build-release-branch) pipeline as needed.
1. After successful build, test changes from [dotnet-tools](https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json) feed. Images from the `nightly` branch of the `dotnet-docker` repository will be recreated the next day after the successful build of the release branch.

## Release to nuget.org and Add GitHub Release

1. Run the [Generate release notes](https://github.com/dotnet/dotnet-monitor/actions/workflows/generate-release-notes.yml) workflow, setting `Use workflow from` to the release branch. Review and merge in the PR created by this workflow.
1. Start [release pipeline](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=105). Allow the stages to trigger automatically (do not check the boxes in the associated dropdown). During creation of the release you must select the dotnet-monitor build to release from the list of available builds. This must be a build with the tag `MonitorRelease` and the associated `MonitorRelease` artifact (set `dotnet-monitor_build` to the pipeline run of `dotnet monitor` that is being released; set `dotnet-monitor_source` to the latest commit from `main`).
1. The release will start the stage "Pre-Release Verification"; this will check that the above steps were done as expected. The name of the release will be updated automatically.
1. Approve the sign-off step the day before the release after 8:15 AM PT, when ready to publish.
> [!NOTE]
> After sign-off of the "Pre-Release Verification" environment the NuGet and GitHub release steps will automatically wait until 8:15 AM PT the next day.

The remainder of the release will automatically push NuGet packages to nuget.org, [tag](https://github.com/dotnet/dotnet-monitor/tags) the commit from the build with the release version, and add a new [GitHub release](https://github.com/dotnet/dotnet-monitor/releases).

**For internal/release/\* build**: Before the `Create GitHub Release` job executes, the sources for an internal release build must be merged into the corresponding public release branch, otherwise GitHub release creation will fail (it requires the commit of the build, which would not be public). Open a PR that merges the commit chain for the release into corresponding public release branch e.g. [example PR](https://github.com/dotnet/dotnet-monitor/pull/5326) demonstrating the `internal/release/8.x -> release/8.x` merge. Depending on when this PR is opened, its builds may fail to acquire the runtimes since the aka.ms links for them may not have been updated by the .NET release yet. (Dev opportunity: Consider automating this step before `Create GitHub Release` is executed)

## Release to Storage Accounts

1. Approximately 3 days before Docker image release, execute a dry-run of the [dotnet-monitor-release](https://dev.azure.com/dnceng/internal/_build?definitionId=1103) pipeline (`Branch` should be set to `main`; `IsDryRun` should be checked; uncheck `IsTestRun`; under `Resources`, select the `dotnet monitor` build from which assets will be published). This will validate that the `.nupkg` files can be published to the `dotnetcli` storage account and checksums can be published to the `dotnetclichecksums` storage account.
1. **For release/\* build**: The **day before** Docker image release, execute run of the [dotnet-monitor-release](https://dev.azure.com/dnceng/internal/_build?definitionId=1103) pipeline (`Branch` should be set to `main`; uncheck `IsDryRun`; uncheck `IsTestRun`; under `Resources`, select the `dotnet monitor` build from which assets will be published). This will publish the archive files to the `dotnetcli` storage account and the checksums to the `dotnetclichecksums` storage account.
1. **For internal/release/\* build**: The **morning of** Docker image release, execute run of the [dotnet-monitor-release](https://dev.azure.com/dnceng/internal/_build?definitionId=1103) pipeline (`Branch` should be set to `main`; uncheck `IsDryRun`; uncheck `IsTestRun`; under `Resources`, select the `dotnet monitor` build from which assets will be published). This will publish the archive files to the `dotnetcli` storage account and the checksums to the `dotnetclichecksums` storage account. Make sure that .NET has started to officially release. Be sure to coordinate the timing of this publish process with the .NET Containers team.

## Release Docker Images

1. Contact .NET Containers team with final version that should be released. This version should be latest version in the `nightly` branch.
1. Docker image build from main branch requires assets to be published to `dotnetcli` and `dotnetclichecksums` storage accounts. See [Release to Storage Accounts](#release-to-storage-accounts).
1. The .NET Containers team will merge from `nightly` branch to `main` branch and wait for `dotnet-monitor` team approval. Typically, these changes are completed the day before the release date.
1. The .NET Containers team will start the build ahead of the release and wait for the all-clear from `dotnet-monitor` team before publishing the images.

**For internal/release/\* build**: A PR will need to be opened directly into the `main` branch that updates the .NET Monitor versions and checksums as appropriate since these values do not exist publicly until [Release to Storage Accounts](#release-to-storage-accounts) has been performed: [Example PR](https://github.com/dotnet/dotnet-docker/pull/4862). Coordinate with .NET Containers team for creating this PR.

The release image is `mcr.microsoft.com/dotnet/monitor`. The tag list is https://mcr.microsoft.com/v2/dotnet/monitor/tags/list.

## After the Release

1. Update the `AutoUpdateDockerBranches` variable to `refs/heads/main` in the [internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=954) to begin the consumption of nightly builds into dotnet-docker. Note this should not necessarily be done right after the release, but after the merge from main to nightly in the dotnet-docker repo (such as https://github.com/dotnet/dotnet-docker/pull/4741). Include additional branch references and semi-colon delimit each value e.g. `refs/heads/main;refs/heads/feature/9.x`.
1. Review and merge the automatically create `Register new release information` PR.
1. For each release, push its corresponding tag to the `shipped/v<version>` branch in the [internal repository](https://dev.azure.com/dnceng/internal/_git/dotnet-dotnet-monitor) e.g `v8.0.0-rc.1.23458.6 -> shipped/v8.0`. If done correctly, this should be a fast-forward merge.
1. When necessary, update this document if its instructions were unclear or incorrect.

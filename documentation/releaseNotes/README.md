
# Release Notes
This folder contains the release notes that appear here: [https://github.com/dotnet/dotnet-monitor/releases](https://github.com/dotnet/dotnet-monitor/releases).

During development, notes for current features can be added to [`releaseNotes.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.md)

## File Naming
Release notes will be named one of 3 different things based on their use:

- `releaseNotes.[fullVersionNumber].md` for a released version. This is set to archive release notes from a version that has been released. `fullVersionNumber` should be the version assigned on the github release page, for example `releaseNotes.v5.0.0-preview.6.21370.3.md` would be the name of the Preview 6 release notes (if they were archived).
- [`releaseNotes.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.md) represents release notes for the code in the current branch that hasn't been released yet.
- [`releaseNotes.template.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.template.md) is the template to be used to create new `releaseNotes.md` in new versions.

## Rotation
These are the steps during a release describing when and how release notes should be modified:

- The `main` branch will merge into `release/*.*`.
- [`releaseNotes.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.md) (in `main`) can be overwritten by [`releaseNotes.template.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.template.md).
- A final build should be produced from the internal official release pipeline. This will have a version like `v5.0.0-preview.6.21370.3`
- Release notes from [`releaseNotes.md`](https://github.com/dotnet/dotnet-monitor/tree/release/5.0/documentation/releaseNotes/releaseNotes.md) (in `release/5.0` or the current release branch) should be copied into `main` as `releaseNotes.[fullVersionNumber].md` where `fullVersionNumber` is the same as the source tag that will be created during release, like `v5.0.0-preview.6.21370.3`.

# Release Notes
This folder contains the release notes that appear here: [https://github.com/dotnet/dotnet-monitor/releases](https://github.com/dotnet/dotnet-monitor/releases).

## File Naming
Release notes will be named one of 4 different things based on their use:

- `releaseNotes.[fullVersionNumber].md` for a released version. This is set to archive release notes from a version that has been released. `fullVersionNumber` should be the version assigned on the github release page, for example `releaseNotes.v5.0.0-preview.6.21370.3.md` would be the name of the Preview 6 release notes (if they were archived).
- [`releaseNotes.current.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.current.md) represents the upcoming release that will be released next. During the release process, the contents of [`releaseNotes.current.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.current.md) will be used to populate the release details.
- `releaseNotes.[friendlyVersionName].md` for a future release. This is document new features in an upcoming release. `friendlyVersionName` will match the milestone labels on github. Example is [`releaseNotes.Preview8.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.Preview8.md) is the next-next release and will become the next release.
- [`releaseNotes.template.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.template.md) is the template copy to use in new versions.

## Rotation
Release notes will remain in their current state in the [`main`](https://github.com/dotnet/dotnet-monitor/tree/main) branch until a release is shipped. Once a release is shipped, the following steps should happen:
- [`releaseNotes.current.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.current.md) will be renamed to `releaseNotes.[fullVersionNumber].md` where `fullVersionNumber` is the version number released to on [GitHub Releases](https://github.com/dotnet/dotnet-monitor/releases).
- The next release notes, `releaseNotes.[friendlyVersionName].md`, will be renamed to [`releaseNotes.current.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.current.md).
- [`releaseNotes.template.md`](https://github.com/dotnet/dotnet-monitor/tree/main/documentation/releaseNotes/releaseNotes.template.md) will be copied to `releaseNotes.[friendlyVersionName].md` where `friendlyVersionName` is the next-next version to be released.
- 
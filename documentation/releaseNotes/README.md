# Release Notes
This folder contains the release notes that appear here: [https://github.com/dotnet/dotnet-monitor/releases](https://github.com/dotnet/dotnet-monitor/releases).

## Creation
Release notes can be created by running the [Generate release notes](https://github.com/dotnet/dotnet-monitor/actions/workflows/generate-release-notes.yml) workflow. This workflow will generate release notes for a given branch, correctly name it according to the format described in [File Naming](#file-naming), and open a PR with the new file.

## File Naming
Release notes are named in the format `releaseNotes.v[fullVersionNumber].md` for a released version. This is set to archive release notes from a version that has been released. `fullVersionNumber` should be the version assigned on the github release page, for example `releaseNotes.v5.0.0-preview.6.21370.3.md` would be the name of the 5.0 Preview 6 release notes (if they were archived).

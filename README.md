# .NET Monitor Repo

This repository contains the source code for dotnet-monitor, a diagnostic tool for capturing diagnostic artifacts in an operator-driven or automated manner.

## Overview

Running a .NET application in diverse environments can make collecting diagnostics artifacts (e.g., logs, traces, process dumps) challenging. dotnet monitor is a tool that provides an unified way to collect these diagnostic artifacts regardless of where your application is run.

There are two different mechanisms for collection of these diagnostic artifacts:

- An [HTTP API](documentation/api/README.md) for on demand collection of artifacts. You can call these API endpoints when you already know your application is experiencing an issue and you are interested in gathering more information.
- [Triggers](documentation/collectionrules/collectionrules.md) for rule-based configuration for always-on collection of artifacts. You may configure rules to collect diagnostic artifacts when a desired condition is met, for example, collect a process dump when you have sustained high CPU.

## Releases

See [Releases](documentation/releases.md) for the release history.

## Docs

[Docs](documentation/README.md) - Learn how to install, configure, and use dotnet-monitor.

## Survey

[Survey](https://aka.ms/dotnet-monitor-survey) - Provide anonymous feedback on your experience using dotnet-monitor.

## Building the Repository

See [building instructions](documentation/building.md) in our documentation directory.

## Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

## Useful Links

Blog Post: [Announcing dotnet-monitor](https://devblogs.microsoft.com/dotnet/announcing-dotnet-monitor-in-net-6/)

See [Videos and Tutorials](documentation/videos-and-tutorials.md) for walkthroughs on how to use dotnet monitor.

## .NET Foundation

.NET Monitor is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

- [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
- [ASP.NET Core home](https://docs.microsoft.com/aspnet/core/?view=aspnetcore-3.1) - the best place to start learning about ASP.NET Core.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

General .NET OSS discussions: [.NET Foundation Discussions](https://github.com/dotnet-foundation/Home/discussions)

## License

.NET monitor  is licensed under the [MIT](LICENSE.TXT) license.

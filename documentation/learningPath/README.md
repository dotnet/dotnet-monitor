
# Overview

Our learning path is designed to help you get up-and-running with your first contribution to `dotnet-monitor` through a hands-on explanation of the inner workings of the tool. We recommend that new contributors begin by following the main learning path, and then optionally pursue branches that are relevant to the parts of the tool you want to work on. If you find any pages that are out of date, let us know by clicking the `Was This Helpful` button at the top of the page, and we'll update the page as soon as possible.

## Main Learning Path

```mermaid
graph TD
    B[Start Here!] --> C[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/building.md'>Building Locally</a>]
    C --> D[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/basic-usage.md'>Basic Usage</a>]
    D --> E[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/architecture.md'>Architecture Overview</a>]
    E --> G[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/sample-pull-request.md'>A Sample Pull Request</a>]
    G --> |OPTIONAL| H[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/collectionrules.md'>Collection Rules</a>]
    G --> |OPTIONAL| I[<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/api.md'>API</a>]
    G --> |OPTIONAL| J["<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/testing.md'>Testing</a>"]
    G --> |OPTIONAL| K["<a href='https://github.com/dotnet/dotnet-monitor/blob/feature/LearningPaths/documentation/learningPath/configuration.md'>Configuration</a>"]
    G --> |OPTIONAL| L[Egress -> COMING SOON]
```

*TODO*: Need to update links to point to main branch of dotnet-monitor repo, since it appears that [we can't do relative links in GitHub Mermaid]( https://gist.github.com/ChristopherA/bffddfdf7b1502215e44cec9fb766dfd#flowchart-with-hyperlinks).

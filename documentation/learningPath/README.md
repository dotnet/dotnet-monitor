
# Overview

Our learning path is designed to help you get up-and-running with your first contribution to `dotnet-monitor` through a hands-on explanation of the inner workings of the tool. We recommend that new contributors begin by following the main learning path, and then optionally pursue branches that are relevant to the parts of the tool you want to work on. If you find any pages that are out of date, let us know by clicking the `Was This Helpful` button at the top of the page, and we'll update the page as soon as possible.

## Main Learning Path

```mermaid
graph TD
    B[Start Here!] --> C[<a href='https://github.com/dotnet/dotnet-monitor/blob/main/documentation/building.md'>Building Locally</a>]
    C --> D[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/architecture.md'>Architecture Overview</a>]
    D --> G[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/architecture.md'>A Sample Pull Request</a>]
    G --> |OPTIONAL| H[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/collectionrules.md'>Collection Rules</a>]
    G --> |OPTIONAL| I[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/api.md'>API</a>]
    G --> |OPTIONAL| J["<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/testing.md'>Testing</a>"]
    G --> |OPTIONAL| K["<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/configuration.md'>Configuration</a>"]
    H --> Q[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/api.md'>Tutorial</a>]
    I --> R[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/api.md'>Tutorial</a>]
    J --> S[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/api.md'>Tutorial</a>]
    K --> T[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/api.md'>Tutorial</a>]
```

*Note*: Need to update links to point to dotnet-monitor repo, not my branch

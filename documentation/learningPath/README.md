
# Overview

Our learning path is designed to help you get up-and-running with your first contribution to `dotnet-monitor` through a hands-on explanation of the inner workings of the tool. We recommend that new contributors begin by following the main learning path, and then optionally pursue branches that are relevant to the parts of the tool you want to work on. If you find any pages that are out of date, let us know by clicking the `Was This Helpful` button at the top of the page, and we'll update the page as soon as possible.

## Main Learning Path

```mermaid
graph TD
    B[Start Here!] --> C[<a href='https://github.com/dotnet/dotnet-monitor/blob/main/documentation/building.md'>Building Locally</a>]
    C --> D[<a href='https://github.com/kkeirstead/dotnet-monitor/blob/kkeirstead/LearningPath/documentation/learningPath/architecture.md'>Architecture Overview</a>]
    D --> E["Go through VS Diagnostics Wiki (this)"]
    E --> |Profiler New-Hire| F[<a href='https://microsoft.sharepoint.com/:o:/r/teams/VisualStudioProductTeam/_layouts/15/Doc.aspx?sourcedoc=%7B1B956597-4B0E-4150-A3AF-1C8327205C26%7D&file=Diagnostics%20Hub&action=edit&mobileredirect=true&wdorigin=Sharepoint'>Diagnostics Hub OneNote</a>]
    E --> |Production Diagnostics New-Hire| G[<a href='https://microsoft.sharepoint.com/teams/VisualStudioProductTeam/Shared%20Documents/Forms/AllItems.aspx?id=%2Fteams%2FVisualStudioProductTeam%2FShared%20Documents%2FVS%20Diagnostics%2FProduction%20Diagnostics%2F%5FOneNote&viewid=fc0feb05%2D8384%2D4621%2Da946%2D71f556fd73ec'>Production Diagnostics OneNote</a>]
    E --> |Debugger New-Hire| H["<a href='https://microsoft.sharepoint.com/teams/VisualStudioProductTeam/_layouts/15/Doc.aspx?sourcedoc={62985f92-d727-4d94-a965-15931f382492}'>Debugger OneNote</a>"]
```

*Note*: Need to update links to point to dotnet-monitor repo, not my branch

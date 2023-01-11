
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Fcollectionrules)

# Collection Rules

## How Collection Rules Work (As A User)

You can learn more about how collection rules work [here](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/collectionrules/collectionrules.md#collection-rules), with usage examples [here](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/collectionrules/collectionruleexamples.md). If you're unfamiliar with collection rules in `dotnet monitor`, we recommend taking a look at how to use collection rules before continuing with this learning path.

## Collection Rule Architecture

### General

A lot of set-up and registration takes place here; if adding a new trigger/action, it needs to be registered here
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/ServiceCollectionExtensions.cs#L100

Representation of a collection rule - note that the only required piece is the Trigger
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/CollectionRuleOptions.cs

Where rules are applied, removed, restarted, and monitored for the API endpoint
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/CollectionRuleService.cs

Responsible for running the collection rule pipeline for a single collection rule:
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/CollectionRulePipeline.cs#L54


### Triggers

### Actions

### Filters

### Limits

## Miscellaneous

### Trigger Shortcuts

### Templates

### Collection Rule Defaults

### Collection Rule API Endpoint

API for getting information about all collection rules or a specific one
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L546
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L571

Descriptions and any calculated components are generated in
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/CollectionRuleService.cs

Tracks information about state of a pipeline for one collection rule
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/CollectionRulePipelineState.cs#L11

## Testing

## Keeping Documentation Up-To-Date

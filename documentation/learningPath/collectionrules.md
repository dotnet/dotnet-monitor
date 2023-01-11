
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

Filter options - used not only for collection rules but also for default process filter for all of dotnet monitor
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.Options/ProcessFilterOptions.cs

### Limits

Limits options
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/CollectionRuleLimitsOptions.cs

## Miscellaneous

### Trigger Shortcuts

Directory that holds all of the EventCounter shortcuts
https://github.com/dotnet/dotnet-monitor/tree/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/Triggers/EventCounterShortcuts

### Templates



### Collection Rule Defaults

Defaults can be set for certain properties on Triggers, Actions, and Limits.
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/CollectionRuleDefaultsOptions.cs

Checks to see if a value is provided for a property - if it has a default and no value, the default is used
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/DefaultCollectionRulePostConfigureOptions.cs

### Collection Rule API Endpoint

API for getting information about all collection rules or a specific one
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L546
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L571

Descriptions and any calculated components are generated in
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/CollectionRuleService.cs

Tracks information about state of a pipeline for one collection rule
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/CollectionRulePipelineState.cs#L11

## Testing

Tests to confirm options behave as expected -> if adding new trigger/action, add this to make sure options are validated correctly
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTests/CollectionRuleOptionsTests.cs

Tests for Collection Rule Defaults
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTests/CollectionRuleDefaultsTests.cs

Tests for collection rules endpoint
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTests/CollectionRuleDescriptionPipelineTests.cs
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/CollectionRuleDescriptionTests.cs

Tests for collection rules
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTests/CollectionRulePipelineTests.cs
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/CollectionRuleTests.cs

## Keeping Documentation Up-To-Date

Information about how to write configuration
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/documentation/configuration.md#collection-rule-configuration

More Info and examples
https://github.com/dotnet/dotnet-monitor/tree/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/documentation/collectionrules

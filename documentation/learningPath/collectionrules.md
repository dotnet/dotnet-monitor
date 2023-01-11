
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

Action options - the Settings are going to be one of https://github.com/dotnet/dotnet-monitor/tree/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/Actions
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/CollectionRuleActionOptions.cs

Interface for all actions instead of needing to know/use a specific type of action. Lets you start the action, wait for it to complete, and get its output values. https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Actions/ICollectionRuleAction.cs

Base class for all real actions (non-testing)
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Actions/CollectionRuleActionBase.cs

Test actions that directly implement ICollectionRuleAction
https://github.com/dotnet/dotnet-monitor/tree/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTestCommon/CollectionRules/Actions



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

Template Options
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/TemplateOptions.cs

Checks to see if a trigger/action/filter/limit is a name (string), instead of an object with children - if so, populates from the correspondingly named template
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Configuration/CollectionRulePostConfigureNamedOptions.cs

Binding triggers/actions
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Configuration/TemplatesConfigureNamedOptions.cs

### Collection Rule Defaults

Defaults can be set for certain properties on Triggers, Actions, and Limits.
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/CollectionRuleDefaultsOptions.cs

Checks to see if a value is provided for a property - if it has a default and no value, the default is used
https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/Options/DefaultCollectionRulePostConfigureOptions.cs

### Collection Rule API Endpoint

The Collection Rule API Endpoint allows users to get information about the state of their collection rules, providing general information [here](https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L546) and more specific information about a particular rule [here](https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/DiagController.cs#L571). **This API is solely for viewing the current state of rules, not altering state**.

Each [collection rule pipeline](https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Tools/dotnet-monitor/CollectionRules/CollectionRulePipeline.cs) has a [state holder](https://github.com/dotnet/dotnet-monitor/blob/ac10d93babcc5388a3c19d19e6c58258c2e21eb8/src/Microsoft.Diagnostics.Monitoring.WebApi/CollectionRulePipelineState.cs) that keeps track of the rule's execution. By keeping track of the pipeline's state in real-time, this state doesn't need to be calculated in response to the `/collectionrules` endpoint. However, other user-facing information, such as countdowns, are calculated on-demand - these values are solely for display purposes and not used by `dotnet-monitor` when determining when to change state (see [NEED THIS]() for information on how `RuleDuration` and `ActionCountSlidingWindowDuration` work).

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

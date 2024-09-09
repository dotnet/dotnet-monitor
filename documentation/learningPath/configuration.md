# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Here, we'll walk through the basics of how configuration works and keeping the documentation up to date.

## How Configuration Works

`dotnet-monitor` accepts configuration from several different sources, and must [combine these sources for the host builder](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/HostBuilder/HostBuilderHelper.cs#L47). Configuration sources are added in the order of lowest to highest precedence - meaning that if there is a conflict between a property in two configuration sources, the property found in the latter configuration source will be used.

To see the merged configuration, the user can run the `config show` command (see [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Program.cs#L71) and [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Commands/ConfigShowCommandHandler.cs)); the `--show-sources` flag can be used to reveal which configuration source is responsible for each property. The `config show` command's output is [written out as JSON](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/ConfigurationJsonWriter.cs); this section must be manually updated whenever new options are added (or existing options are changed).

Once configuration has been merged, any singletons that have been added to the `IServiceCollection` (see [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/ServiceCollectionExtensions.cs) and [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Commands/CollectCommandHandler.cs#L84)), such as `IConfigureOptions`, `IPostConfigureOptions`, and `IValidateOptions`, are called when an object of that type is first used, **not on startup**. This step is often used to incorporate defaults for properties that were not explicitly set by configuration, or to validate that options were set correctly.

Any changes to the configuration need to be propagated to the [schema](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/documentation/schema.json). **The updated schema should be generated automatically; you should never need to manually edit the JSON.** To update the schema in Visual Studio:
* Set [Microsoft.Diagnostics.Monitoring.ConfigurationSchema](https://github.com/dotnet/dotnet-monitor/tree/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema) as the startup project
* Build the project, with a single command-line argument for the schema's absolute path
* Validate that the schema was correctly updated using the tests in [Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests](https://github.com/dotnet/dotnet-monitor/tree/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests)

## Keeping Documentation Up-To-Date

Our configuration is primarily documented [here](https://github.com/dotnet/dotnet-monitor/tree/b5bf953026d47318e521e5580524866ef0aab764/documentation/configuration). Sections are typically comprised of:
* A brief overview of the feature that is being configured
* Configuration samples in all supported formats
* A list of properties with descriptions, types, and whether a property is required

Types are defined in [definitions.md](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/documentation/api/definitions.md), and additional information about configuring collection rules can be found in the [collection rules](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/documentation/collectionrules) directory. Where appropriate, indicate if configuration only pertains to a specific version of `dotnet-monitor` (e.g. `7.0+`).

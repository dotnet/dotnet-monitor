# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Here, we'll walk through the basics of how configuration works, how to update it, and keeping the documentation up to date.

## How Configuration Works

`dotnet-monitor` accepts configuration from several different sources, and must combine it from these sources for the host builder. Configuration sources are added in the order of lowest to highest precedence - meaning that if there is a conflict between a property in two configuration sources, the property found in the latter configuration source will be used.

https://github.com/kkeirstead/dotnet-monitor/blob/698970a7158040114f8477fa2c4b6780111c7de8/src/Tools/dotnet-monitor/HostBuilder/HostBuilderHelper.cs#L46
https://github.com/kkeirstead/dotnet-monitor/blob/698970a7158040114f8477fa2c4b6780111c7de8/src/Tools/dotnet-monitor/ServiceCollectionExtensions.cs
https://github.com/kkeirstead/dotnet-monitor/blob/698970a7158040114f8477fa2c4b6780111c7de8/src/Tools/dotnet-monitor/ConfigurationJsonWriter.cs
https://github.com/kkeirstead/dotnet-monitor/blob/698970a7158040114f8477fa2c4b6780111c7de8/src/Tools/dotnet-monitor/Program.cs#L69
https://github.com/kkeirstead/dotnet-monitor/blob/698970a7158040114f8477fa2c4b6780111c7de8/src/Tools/dotnet-monitor/Commands/ConfigShowCommandHandler.cs

## How To Update Configuration

## Keeping Documentation Up-To-Date

Our configuration is primarily documented in [configuration.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md). Sections are typically comprised of:
* A brief overview of the feature that is being configured
* Configuration samples in all supported formats
* A list of properties with descriptions, types, and whether a property is required

Types are defined in [definitions.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/definitions.md), and additional information about configuring collection rules can be found in the [collection rules](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/collectionrules) directory. Where appropriate, indicate if configuration only pertains to a specific version of `dotnet-monitor` (e.g. `7.0+`).

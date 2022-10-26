# Configuration

`dotnet monitor` has extensive configuration to control various aspects of its behavior. Here, we'll walk through the basics of how configuration works, how to update it, and keeping the documentation up to date.

## How Configuration Works

## How To Update Configuration

## Keeping Documentation Up-To-Date

Our configuration is primarily documented in [configuration.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration.md). Sections are typically comprised of:
* A brief overview of the feature that is being configured
* Configuration samples in all supported formats
* A list of properties with descriptions, types, and whether a property is required

Types are defined in [definitions.md](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/definitions.md), and additional information about configuring collection rules can be found in the [collection rules](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/collectionrules) directory. Where appropriate, indicate if configuration only pertains to a specific version of `dotnet-monitor` (e.g. `7.0+`).

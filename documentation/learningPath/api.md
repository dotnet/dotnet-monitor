
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Fapi)

# API

dotnet-monitor's web API is built upon [ASP.NET Core](https://dotnet.microsoft.com/learn/aspnet/what-is-aspnet-core). Code for the [API surface](../../src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers), [models](../../src/Microsoft.Diagnostics.Monitoring.WebApi/Models), and [authentication](../../src/Tools/dotnet-monitor/Auth). # JSFIX


## Adding APIs

[Controllers](../../src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers)


### OpenAPI generation

For more information see [OpenAPI generation](./testing.md#openapi-generation).

### Testing

For more information see [Functional Tests](./testing.md#functional-tests).

## Models


## Authentication

dotnet-monitor support multiple different [authentication modes](../authentication.md).


### Determining Authentication Mode

When dotnet-monitor starts, the command line arguments are first inspected to see if a particular authentication mode was set, referred to as `StartupAuthenticationMode` such as `--no-auth` ([here](../../src/Tools/dotnet-monitor/Commands/CollectCommandHandler.cs#L27)). If no modes were explicitly set by the command line, dotnet-monitor will select `Deferred` as the authentication mode. This indicates that the user configuration should be looked at to determine the authentication mode. [here](../../src/Tools/dotnet-monitor/Auth/AuthConfiguratorFactory.cs).




`src/Tools/dotnet-monitor/Auth`
`src/Tools/dotnet-monitor/Auth/AuthConfiguratorFactory.cs`


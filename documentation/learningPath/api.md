
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Fapi)

# API

dotnet-monitor exposes functionality through both [collection rules](./collectionrules.md) and a web API surface. The web api is built using [ASP.NET Core](https://dotnet.microsoft.com/learn/aspnet/what-is-aspnet-core).

## Adding New APIs

The API surface is defined by a series of controllers [here](../../src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/). It's common for an API to expose functionality also available via [Actions](./collectionrules.md#actions) and so methods in these controllers are often wrappers around a shared implementation.

Controllers with `[Authorize(Policy = AuthConstants.PolicyName)]` class attribute will require authentication on all routes defined within. Learn more about how Authentication is handled [here](#authentication).

If the new API needs to either accept or return structured data, a model should be used. Models are defined [here](../../src/Microsoft.Diagnostics.Monitoring.WebApi/Models/).

When adding a new API, it's important to also update the [openapi.json](../openapi.json) spec which describes the API surface. There are CI tests that will ensure this file has been updated to reflect any API changes. Learn more about updating `openapi.json` [here](./testing.md#openapi-generation).

### Adding Tests

Web APIs in dotnet-monitor are typically tested using functional tests that leverage the [ApiClient](../../src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/HttpApi/ApiClient.cs) to call a specific API. Learn more about how the functional tests are defined and operate [here](./testing.md#functional-tests).

## Authentication

dotnet-monitor supports multiple different [authentication modes](../authentication.md) that can be configured by the user. Authentication will be required on any route beloning to a controller with the `[Authorize(Policy = AuthConstants.PolicyName)]` class attribute.

### Determining Authentication Mode

When dotnet-monitor starts, the command line arguments are first inspected to see if a specific authentication mode was set (such as `--no-auth`), referred to as the `StartupAuthenticationMode` ([here](../../src/Tools/dotnet-monitor/Commands/CollectCommandHandler.cs#L27)). If no modes were explicitly set via a command line argument, dotnet-monitor will select `Deferred` as the `StartupAuthenticationMode`. This indicates that the user configuration should be looked at to determine the authentication mode later on in the startup process (described below)

After determining the `StartupAuthenticationMode` mode, the relevant [IAuthenticationConfigurator](../../src/Tools/dotnet-monitor/Auth/IAuthenticationConfigurator.cs) is created by the [AuthConfiguratorFactory](../../src/Tools/dotnet-monitor/Auth/AuthConfiguratorFactory.cs). This factory also handled deciding what authentication mode to use when `StartupAuthenticationMode` is `Deffered`. The selected configurator is used to configure various parts of dotnet-monitor that are specific to authentication, such as protecting the web APIs, add authentication-mode specific logging, and configuring the built-in Swagger UI.

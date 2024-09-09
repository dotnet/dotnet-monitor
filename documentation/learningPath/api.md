# API

dotnet-monitor exposes functionality through both [collection rules](./collectionrules.md) and a web API surface. The web api is built using [ASP.NET Core](https://dotnet.microsoft.com/learn/aspnet/what-is-aspnet-core) and enables on-demand extraction of diagnostic information and artifacts from discoverable processes.

## Adding New APIs

The web API surface is defined by a series of controllers [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/). It's common for an API to expose functionality also available via [Actions](./collectionrules.md#actions) and so methods in these controllers are often wrappers around a shared implementation. Each controller may have one or more attributes that configure how and where it is exposed, you can learn more about the notable controller attributes [here](#notable-controller-attributes).

If the new API needs to either accept or return structured data, a dedicated model should be used. Models are defined [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Microsoft.Diagnostics.Monitoring.WebApi/Models/).

When adding a new API, it's important to also update the [`openapi.json`](../openapi.json) spec which describes the API surface. There are CI tests that will ensure this file has been updated to reflect any API changes. Learn more about updating `openapi.json` [here](./testing.md#openapi-generation).

### Adding Tests

Web APIs in dotnet-monitor are typically tested using functional tests that leverage the [ApiClient](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/HttpApi/ApiClient.cs) to call a specific API. Learn more about how the functional tests are defined and operate [here](./testing.md#functional-tests).

## Notable Controller Attributes

### Authorization

Controllers with `[Authorize(Policy = AuthConstants.PolicyName)]` class attribute will require authentication on all routes defined within. Learn more about how Authentication is handled [here](#authentication).

### HostRestriction

In addition to the default URLs that dotnet-monitor will accept API requests on, there are also metrics urls which do not require any authentication and are generally considered safe to expose as they don't serve any sensitive data (unless explicitly configured so by the user). Learn more about the metrics urls [here](../configuration/metrics-configuration.md#metrics-urls).

If an API may potentially serve sensitive data, such as logs or dumps, then it must be in a controller with the `[HostRestriction]` attribute to avoid exposing it on the unauthenticated metrics urls.

## Authentication

dotnet-monitor supports multiple different [authentication modes](../authentication.md) that can be configured by the user. Authentication will be required on any route belonging to a controller with the `[Authorize(Policy = AuthConstants.PolicyName)]` class attribute.

### Determining Authentication Mode

When dotnet-monitor starts, the command line arguments are first inspected to see if a specific authentication mode was set (such as `--no-auth`), referred to as the `StartupAuthenticationMode`, this is calculated [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Commands/CollectCommandHandler.cs#L28). If no modes were explicitly set via a command line argument, dotnet-monitor will select `Deferred` as the `StartupAuthenticationMode`. This indicates that the user configuration should be looked at to determine the authentication mode later on in the startup process.

After determining the `StartupAuthenticationMode` mode, the relevant [IAuthenticationConfigurator](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Auth/IAuthenticationConfigurator.cs) is created by the [AuthConfiguratorFactory](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tools/dotnet-monitor/Auth/AuthConfiguratorFactory.cs). This factory also handles deciding what authentication mode to use when `StartupAuthenticationMode` is `Deferred`. The selected configurator is used to configure various parts of dotnet-monitor that are specific to authentication, such as protecting the web APIs and adding authentication-mode specific logging.

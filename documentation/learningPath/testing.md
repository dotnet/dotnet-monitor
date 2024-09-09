# Testing

## Running Tests

Tests can be executed with the command line (via [build.cmd](../../Build.cmd) -test), as part of the PR build, or in Visual Studio. Note that because of limited resources in the build pool, tests ran from the command line or in the build pool are serialized. This avoids test failures associated with parallel testing. Visual Studio does not have such restrictions and is best used for individual tests and test investigations. When running from the command line, using the `-testgroup` parameter can be used to limit the amount of tests executed. For example `build.cmd -test -testgroup PR` will run the same tests as the PR build.

The framework of the test assemblies is controlled by [TestTargetFrameworks](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/eng/Versions.props). The test itself is attributed with a particular framework based on the [TargetFrameworkMonikerTraitAttribute](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.TestCommon/TargetFrameworkMonikerTraitAttribute.cs).

## Unit Tests

- [Microsoft.Diagnostics.Monitoring.Tool.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTests)
- [Microsoft.Diagnostics.Monitoring.WebApi.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.WebApi.UnitTests/)
- [CollectionRuleActions.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/CollectionRuleActions.UnitTests/)

Unit test assemblies directly reference types from various dotnet-monitor assemblies. However, since most of dotnet-monitor heavily relies on code injection, there are utility classes to simplify unit test creation.

- [TestHostHelper](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTestCommon/TestHostHelper.cs) can be used to setup a basic unit test scenario using dependency injection.
- [CollectionRuleOptionsExtensions](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTestCommon/Options/CollectionRuleOptionsExtensions.cs) can be used to easily create collection rules from configuration.

## Functional Tests

- [Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests)
- [Microsoft.Diagnostics.Monitoring.UnitTestApp](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/)

Functional tests are composed of 3 main parts:
1. The test itself, which sets up and validates the results.
1. An instance of dotnet-monitor
1. An instance of an application that is being monitored (from the UnitTestApp assembly)

* [ScenarioRunner](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/Runners/ScenarioRunner.cs) is typically used to orchestrate test runs. The class will spawn both an instance of dotnet-monitor and an instance of test application. The app and the test communicate via stdio. The test communicates with dotnet-monitor via its Api surface.
* The dotnet-monitor Api surface can be accessed through the [ApiClient](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/HttpApi/ApiClient.cs).
* New scenarios can be added [here](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/Scenarios/).
* The [AsyncWaitScenario](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/Scenarios/AsyncWaitScenario.cs) is sufficient for most tests.
* Coordination of the scenario and the test is done via message passing (json over stdio) between the test and the app. To send messages to the app from the test, [AppRunner](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.TestCommon/Runners/AppRunner.cs)'s `SendCommandAsync` is used. In the scenario definition, [ScenarioHelpers](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/ScenarioHelpers.cs)'s `WaitForCommandAsync` is used. This can be used to synchronize various points of the test application with the execution of the dotnet-monitor Api from the test itself.

## Native/Profiler Tests

- [Microsoft.Diagnostics.Monitoring.Profiler.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Profiler.UnitTests/)
- [Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp/)

This test assembly provides a test to make sure the dotnet-monitor profiler can load into a target app.

## Schema Generation

- [Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests/)
- [Microsoft.Diagnostics.Monitoring.ConfigurationSchema](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema/)
- [Microsoft.Diagnostics.Monitoring.Options](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Microsoft.Diagnostics.Monitoring.Options)

Dotnet-monitor generates [schema.json](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/documentation/schema.json) using unit tests. If dotnet-monitor's configuration changes, the schema.json file needs to be updated.
Note that it is possible to compile option classes directly into the `ConfigurationSchema` project. This may be necessary in order to attribute properties appropriately for schema generation. See [Microsoft.Diagnostics.Monitoring.ConfigurationSchema.csproj](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema/Microsoft.Diagnostics.Monitoring.ConfigurationSchema.csproj). See the [Configuration](./configuration.md#how-configuration-works) learning path for more details.

## OpenAPI generation

- [Microsoft.Diagnostics.Monitoring.OpenApiGen.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.OpenApiGen.UnitTests/)
- [Microsoft.Diagnostics.Monitoring.OpenApiGen](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.OpenApiGen/)

These assemblies and tests are used to generate the [OpenAPI spec](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/documentation/openapi.json) for the dotnet-monitor API. Changes to the dotnet-monitor api surface require updating `openapi.json`.

If using VSCode or Codespaces, you can also use the `Regenerate openapi.json` task.

## Startup hooks / hosting startup

- [Microsoft.Diagnostics.Monitoring.Tool.TestStartupHook](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.TestStartupHook/)

This assembly is injected into a dotnet-monitor runner (using `DOTNET_STARTUP_HOOKS`) to facilitate Assembly resolution during test runs.

- [Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup/)

Uses `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` to inject a service into dotnet-monitor during test time. This allows tests to locate files that are not normally part of the test deployment,
such as the native profiler.

- [Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests/)

Unit tests around features that are injected via `DOTNET_STARTUP_HOOKS` into the target application. This currently includes the Exceptions History feature.

## Misc test assemblies

- [Microsoft.Diagnostics.Monitoring.TestCommon](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.TestCommon/)

Utility classes that are shared between Unit Tests and Functional Tests.

- [Microsoft.Diagnostics.Monitoring.Tool.UnitTestCommon](https://github.com/dotnet/dotnet-monitor/blob/b5bf953026d47318e521e5580524866ef0aab764/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.UnitTestCommon/)

Utility classes shared between unit test assemblies.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal sealed class ParameterCapturingScenario
    {
        public static Command Command()
        {
            Command expectLogStatementsCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.ExpectLogStatement);
            expectLogStatementsCommand.SetAction(ExpectLogStatementAsync);

            Command doNotExpectLogStatementsCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.DoNotExpectLogStatement);
            doNotExpectLogStatementsCommand.SetAction(DoNotExpectLogStatementAsync);

            Command aspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp);
            aspNetAppCommand.SetAction(AspNetAppAsync);

            Command nonAspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.NonAspNetApp);
            nonAspNetAppCommand.SetAction(NonAspNetAppAsync);

            Command scenarioCommand = new(TestAppScenarios.ParameterCapturing.Name);
            scenarioCommand.Subcommands.Add(expectLogStatementsCommand);
            scenarioCommand.Subcommands.Add(doNotExpectLogStatementsCommand);
            scenarioCommand.Subcommands.Add(aspNetAppCommand);
            scenarioCommand.Subcommands.Add(nonAspNetAppCommand);

            return scenarioCommand;
        }

        public static Task<int> ExpectLogStatementAsync(ParseResult result, CancellationToken token)
        {
            return LogStatementCoreAsync(result, expectLogs: true, token);
        }

        public static Task<int> DoNotExpectLogStatementAsync(ParseResult result, CancellationToken token)
        {
            return LogStatementCoreAsync(result, expectLogs: false, token);
        }

        private static Task<int> LogStatementCoreAsync(ParseResult result, bool expectLogs, CancellationToken token)
        {
            LogRecord logRecord = new();

            return ScenarioHelpers.RunWebScenarioAsync<Startup>(
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Validate, logger);

                    SampleMethods.StaticTestMethodSignatures.NoArgs();

                    bool didFindLogs = logRecord.Events.Where(e => e.Category == typeof(DotnetMonitor.ParameterCapture.UserCode).FullName).Any();
                    Assert.Equal(expectLogs, didFindLogs);
                    return 0;

                },
                token,
                configureServices: (services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddProvider(new TestLoggerProvider(logRecord));
                    });
                });
        }

        public static Task<int> AspNetAppAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunWebScenarioAsync<Startup>(
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);
                    return 0;
                }, token);
        }

        public static Task<int> NonAspNetAppAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);
                    return 0;
                }, token);
        }


        private sealed class Startup
        {
            public static void ConfigureServices(IServiceCollection services)
            {
                services.AddControllers();
            }

            public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", Responses.Ok);
                });
            }

            public static class Responses
            {
                public static IResult Ok() => Results.Ok();
            }
        }
    }
}

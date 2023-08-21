﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal sealed class ParameterCapturingScenario
    {
        public static CliCommand Command()
        {
            CliCommand expectLogStatementsCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.ExpectLogStatement);
            expectLogStatementsCommand.SetAction(ExpectLogStatementAsync);

            CliCommand doNotExpectLogStatementsCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.DoNotExpectLogStatement);
            doNotExpectLogStatementsCommand.SetAction(DoNotExpectLogStatementAsync);

            CliCommand aspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp);
            aspNetAppCommand.SetAction(AspNetAppAsync);

            CliCommand nonAspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.NonAspNetApp);
            nonAspNetAppCommand.SetAction(NonAspNetAppAsync);

            CliCommand scenarioCommand = new(TestAppScenarios.ParameterCapturing.Name);
            scenarioCommand.Subcommands.Add(expectLogStatementsCommand);
            scenarioCommand.Subcommands.Add(doNotExpectLogStatementsCommand);
            scenarioCommand.Subcommands.Add(aspNetAppCommand);
            scenarioCommand.Subcommands.Add(nonAspNetAppCommand);

            return scenarioCommand;
        }

        public static Task<int> ExpectLogStatementAsync(ParseResult result, CancellationToken token)
        {
            LogRecord logRecord = new();

            return ScenarioHelpers.RunWebScenarioAsync<Startup>(
                configureServices: (services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddProvider(new TestLoggerProvider(logRecord));
                    });
                },
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);

                    int testValue = int.MaxValue;
                    SampleMethods.StaticTestMethodSignatures.Basic(testValue);

                    LogRecordEntry logEntry = logRecord.Events.First(e => e.Category == typeof(DotnetMonitor.ParameterCapture.UserCode).FullName);
                    Assert.NotNull(logEntry);
                    Assert.Contains(Convert.ToString(testValue), logEntry.Message);

                    return 0;
                }, token);
        }

        public static Task<int> DoNotExpectLogStatementAsync(ParseResult result, CancellationToken token)
        {
            LogRecord logRecord = new();

            return ScenarioHelpers.RunWebScenarioAsync<Startup>(
                configureServices: (services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddProvider(new TestLoggerProvider(logRecord));
                    });
                },
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);

                    SampleMethods.StaticTestMethodSignatures.Basic(0);

                    bool didFindLogs = logRecord.Events.Where(e => e.Category == typeof(DotnetMonitor.ParameterCapture.UserCode).FullName).Any();
                    Assert.False(didFindLogs);
                    return 0;

                }, token);
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
            LogRecord logRecord = new();

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
                    endpoints.MapGet("/privacy", Responses.Ok);
                    endpoints.MapGet("/slowresponse", Responses.SlowResponseAsync);
                });
            }

            public static class Responses
            {
                public static IResult Ok() => Results.Ok();

                public static async Task<IResult> SlowResponseAsync()
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));

                    return Results.Ok();
                }
            }
        }
    }
}

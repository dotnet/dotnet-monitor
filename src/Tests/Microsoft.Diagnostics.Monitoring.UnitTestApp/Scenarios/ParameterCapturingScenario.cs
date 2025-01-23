// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal sealed class ParameterCapturingScenario
    {
        public static Command Command()
        {
            Command aspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp);
            aspNetAppCommand.SetAction(AspNetAppAsync);

            Command nonAspNetAppCommand = new(TestAppScenarios.ParameterCapturing.SubScenarios.NonAspNetApp);
            nonAspNetAppCommand.SetAction(NonAspNetAppAsync);

            Command scenarioCommand = new(TestAppScenarios.ParameterCapturing.Name);
            scenarioCommand.Subcommands.Add(aspNetAppCommand);
            scenarioCommand.Subcommands.Add(nonAspNetAppCommand);

            return scenarioCommand;
        }

        public static Task<int> AspNetAppAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunWebScenarioAsync<Startup>(
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);

                    SampleMethods.StaticTestMethodSignatures.NoArgs();

                    return 0;
                }, token);
        }

        public static Task<int> NonAspNetAppAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(
                func: async logger =>
                {
                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue, logger);

                    SampleMethods.StaticTestMethodSignatures.NoArgs();

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

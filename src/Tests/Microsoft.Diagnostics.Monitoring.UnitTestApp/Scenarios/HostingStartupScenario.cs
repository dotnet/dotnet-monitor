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
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal sealed class HostingStartupScenario
    {
        public static Command Command()
        {
            Command aspnetAppNoHostingStartupCommand = new(TestAppScenarios.HostingStartup.SubScenarios.VerifyAspNetAppWithoutHostingStartup);
            aspnetAppNoHostingStartupCommand.SetAction(VerifyAspNetAppWithoutHostingStartupAsync);

            Command aspnetAppCommand = new(TestAppScenarios.HostingStartup.SubScenarios.VerifyAspNetApp);
            aspnetAppCommand.SetAction(VerifyAspNetAppAsync);

            Command nonAspNetAppCommand = new(TestAppScenarios.HostingStartup.SubScenarios.VerifyNonAspNetAppNotImpacted);
            nonAspNetAppCommand.SetAction(VerifyNonAspNetAppNotImpactedAsync);

            Command scenarioCommand = new(TestAppScenarios.HostingStartup.Name);
            scenarioCommand.Subcommands.Add(aspnetAppNoHostingStartupCommand);
            scenarioCommand.Subcommands.Add(aspnetAppCommand);
            scenarioCommand.Subcommands.Add(nonAspNetAppCommand);
            return scenarioCommand;
        }

        public static Task<int> VerifyNonAspNetAppNotImpactedAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(logger =>
            {
                Assert.Equal(0, HostingStartup.HostingStartup.InvocationCount);
                return Task.FromResult(0);
            }, token);
        }

        public static Task<int> VerifyAspNetAppWithoutHostingStartupAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunWebScenarioAsync<Startup>(logger =>
            {
                Assert.Equal(0, HostingStartup.HostingStartup.InvocationCount);
                return Task.FromResult(0);
            }, token);
        }

        public static Task<int> VerifyAspNetAppAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunWebScenarioAsync<Startup>(logger =>
            {
                Assert.Equal(1, HostingStartup.HostingStartup.InvocationCount);
                return Task.FromResult(0);
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
                    endpoints.MapGet("/", () => Results.Ok());
                });
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
#if NET6_0_OR_GREATER
    internal sealed class AspNetScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.AspNet.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            context.ExitCode = await ScenarioHelpers.RunWebScenarioAsync<Startup>(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.AspNet.Commands.Continue, logger);

                return 0;
            }, context.GetCancellationToken());
        }

        private sealed class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddControllers();
                services.AddSingleton<ResponseService>();
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResponseService responses)
            {
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", responses.Ok);
                    endpoints.MapGet("/privacy", responses.Ok);
                    endpoints.MapGet("/slowresponse", responses.SlowResponseAsync);
                });
            }

            public sealed class ResponseService
            {
                public IResult Ok() => Results.Ok();

                public async Task<IResult> SlowResponseAsync()
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));

                    return Results.Ok();
                }
            }
        }
    }
#endif
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Reflection;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;
using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        [Fact]
        public async Task ActionListExecutor_MultipleExecute()
        {
            ActionListExecutor executor = new();



            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "ZeroExitCode" });

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionResult result = await action.ExecuteAsync(options, null, cancellationTokenSource.Token);

            ValidateActionResult(result, "0");
        }

        [Fact]
        public async Task ExecuteAction_NonzeroExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "NonzeroExitCode" });

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            InvalidOperationException invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => action.ExecuteAsync(options, null, cancellationTokenSource.Token));

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), invalidOperationException.Message);
        }

        [Fact]
        public async Task ExecuteAction_TokenCancellation()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "Sleep", (TokenTimeoutMs + DelayMs).ToString() }); ;

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            TaskCanceledException taskCanceledException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => action.ExecuteAsync(options, null, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ExecuteAction_TextFileOutput()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            DirectoryInfo outputDirectory = null;

            try
            {
                outputDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ExecuteAction", Guid.NewGuid().ToString()));
                string textFileOutputPath = Path.Combine(outputDirectory.FullName, "file.txt");

                const string testMessage = "TestMessage";

                options.Path = DotNetHost.HostExePath;
                options.Arguments = GenerateArgumentsString(new string[] { "TextFileOutput", textFileOutputPath, testMessage });

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleActionResult result = await action.ExecuteAsync(options, null, cancellationTokenSource.Token);

                ValidateActionResult(result, "0");

                Assert.Equal(testMessage, File.ReadAllText(textFileOutputPath));
            }
            finally
            {
                try
                {
                    outputDirectory?.Delete(recursive: true);
                }
                catch
                {
                }
            }
        }

        [Fact]
        public async Task ExecuteAction_InvalidPath()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            string uniquePathName = Guid.NewGuid().ToString();

            options.Path = uniquePathName;
            options.Arguments = GenerateArgumentsString(Array.Empty<string>());

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            FileNotFoundException fileNotFoundException = await Assert.ThrowsAsync<FileNotFoundException>(
                () => action.ExecuteAsync(options, null, cancellationTokenSource.Token));

            Assert.Equal(string.Format(Strings.ErrorMessage_FileNotFound, uniquePathName), fileNotFoundException.Message);
        }

        [Fact]
        public async Task ExecuteAction_IgnoreExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "NonzeroExitCode" });
            options.IgnoreExitCode = true;

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionResult result = await action.ExecuteAsync(options, null, cancellationTokenSource.Token);

            ValidateActionResult(result, "1");
        }

        private static string GenerateArgumentsString(string[] additionalArgs)
        {
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            List<string> args = new();

            // Entrypoint assembly
            args.Add(AssemblyHelper.GetAssemblyArtifactBinPath(currAssembly, "Microsoft.Diagnostics.Monitoring.ExecuteActionApp", TargetFrameworkMoniker.NetCoreApp31));

            // Entrypoint arguments
            args.AddRange(additionalArgs);

            return string.Join(' ', args);
        }

        private static void ValidateActionResult(CollectionRuleActionResult result, string expectedExitCode)
        {
            string actualExitCode;

            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue("ExitCode", out actualExitCode));
            Assert.Equal(expectedExitCode, actualExitCode);
        }

        private async Task Validate()
        {
            IHost host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();
                })
                .Build();

            try
            {
                validate(host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>());
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    host.Dispose();
                }
            }
        }
    }
}
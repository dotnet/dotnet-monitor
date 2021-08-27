// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Reflection;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;
using System;
using System.IO;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExecuteActionTests
    {
        private const int TokenTimeoutMs = 10000;
        private const int DelayMs = 1000;

        [Fact]
        public async Task ExecuteAction_ZeroExitCode()
        {
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

            CollectionRuleActionException invalidOperationException = await Assert.ThrowsAsync<CollectionRuleActionException>(
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

            CollectionRuleActionException fileNotFoundException = await Assert.ThrowsAsync<CollectionRuleActionException>(
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

        internal static string GenerateArgumentsString(string[] additionalArgs)
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
    }
}

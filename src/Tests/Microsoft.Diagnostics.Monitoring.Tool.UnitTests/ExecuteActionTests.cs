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

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExecuteActionTests
    {
        private const int TokenTimeoutMs = 2000; // Must be less than DelayMs in the ExecuteApp

        private const string TaskCanceledMessage = "A task was canceled";

        [Fact]
        public async Task ExecuteAction_ZeroExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "ZeroExitCode" });

            CollectionRuleActionResult result = await action.ExecuteAsync(options, null, CreateCancellationToken());

            Assert.Equal("0", result.OutputValues["ExitCode"]);
        }

        [Fact]
        public async Task ExecuteAction_NonzeroExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "NonzeroExitCode" });

            InvalidOperationException invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => action.ExecuteAsync(options, null, CreateCancellationToken()));

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "-1"), invalidOperationException.Message);
        }

        [Fact]
        public async Task ExecuteAction_TokenCancellation()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "TokenCancellation" }); ;

            TaskCanceledException taskCanceledException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => action.ExecuteAsync(options, null, CreateCancellationToken()));

            Assert.Contains(TaskCanceledMessage, taskCanceledException.Message);
        }

        [Fact]
        public async Task ExecuteAction_TextFileOutput()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            DirectoryInfo outputDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ExecuteAction", Guid.NewGuid().ToString()));
            string textFileOutputPath = outputDirectory.FullName + "\\file.txt";

            const string testMessage = "TestMessage";

            options.Path = DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(new string[] { "TextFileOutput", textFileOutputPath, testMessage });

            CollectionRuleActionResult result = await action.ExecuteAsync(options, null, CreateCancellationToken());

            Assert.Equal("0", result.OutputValues["ExitCode"]);
            Assert.Equal(testMessage, File.ReadAllText(textFileOutputPath));

            try
            {
                outputDirectory?.Delete(recursive: true);
            }
            catch
            {
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

            FileNotFoundException fileNotFoundException = await Assert.ThrowsAsync<FileNotFoundException>(
                () => action.ExecuteAsync(options, null, CreateCancellationToken()));

            Assert.Equal(string.Format(Strings.ErrorMessage_FileNotFound, uniquePathName), fileNotFoundException.Message);
        }

        private static string GenerateArgumentsString(string[] additionalArgs)
        {
            return Assembly.GetExecutingAssembly().Location.Replace(
                Assembly.GetExecutingAssembly().GetName().Name,
                "Microsoft.Diagnostics.Monitoring.ExecuteActionApp") + ' ' + string.Join(' ', additionalArgs);
        }

        private static CancellationToken CreateCancellationToken()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);
            return cancellationTokenSource.Token;
        }
    }
}

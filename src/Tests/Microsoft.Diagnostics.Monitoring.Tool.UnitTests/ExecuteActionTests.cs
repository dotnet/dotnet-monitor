﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExecuteActionTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _outputHelper;

        public ExecuteActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task ExecuteAction_ZeroExitCode()
        {
            await ValidateAction(
                options =>
                {
                    options.Path = DotNetHost.GetPath();
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(new string[] { ActionTestsConstants.ZeroExitCode });
                },
                async (action, token) =>
                {
                    await action.StartAsync(token);

                    CollectionRuleActionResult result = await action.WaitForCompletionAsync(token);

                    ValidateActionResult(result, "0");
                });
        }

        [Fact]
        public async Task ExecuteAction_NonzeroExitCode()
        {
            await ValidateAction(
                options =>
                {
                    options.Path = DotNetHost.GetPath();
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(new string[] { ActionTestsConstants.NonzeroExitCode });
                },
                async (action, token) =>
                {
                    await action.StartAsync(token);

                    CollectionRuleActionException invalidOperationException = await Assert.ThrowsAsync<CollectionRuleActionException>(
                        () => action.WaitForCompletionAsync(token));

                    Assert.Equal(string.Format(Tools.Monitor.Strings.ErrorMessage_NonzeroExitCode, "1"), invalidOperationException.Message);
                });
        }

        [Fact]
        public async Task ExecuteAction_TokenCancellation()
        {
            // This timeout is much shorter than the default test timeout.
            TimeSpan CompletionTimeout = TimeSpan.FromSeconds(3);
            // Have app sleep longer than the timeout period so that cancellation can be effective.
            string sleepMsArg = (2 * CompletionTimeout).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

            await ValidateAction(
                options =>
                {
                    options.Path = DotNetHost.GetPath();
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(new string[] { ActionTestsConstants.Sleep, sleepMsArg });
                },
                async (action, token) =>
                {
                    await action.StartAsync(token);

                    // Start a separate cancellation source for the completion timeout since it needs
                    // to be much shorter than the default test timeout.
                    using CancellationTokenSource completionCancellation = new(CompletionTimeout);

                    await Assert.ThrowsAsync<TaskCanceledException>(
                        () => action.WaitForCompletionAsync(completionCancellation.Token));
                });
        }

        [Fact]
        public async Task ExecuteAction_TextFileOutput()
        {
            using TemporaryDirectory outputDirectory = new(_outputHelper);

            string textFileOutputPath = Path.Combine(outputDirectory.FullName, "file.txt");

            const string testMessage = "TestMessage";

            await ValidateAction(
                options =>
                {
                    options.Path = DotNetHost.GetPath();
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(new string[] { ActionTestsConstants.TextFileOutput, textFileOutputPath, testMessage });
                },
                async (action, token) =>
                {
                    await action.StartAsync(token);

                    CollectionRuleActionResult result = await action.WaitForCompletionAsync(token);

                    ValidateActionResult(result, "0");
                });

            Assert.Equal(testMessage, File.ReadAllText(textFileOutputPath));
        }

        [Fact]
        public async Task ExecuteAction_InvalidPath()
        {
            string uniquePathName = Guid.NewGuid().ToString();

            await ValidateAction(
                options =>
                {
                    options.Path = uniquePathName;
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(Array.Empty<string>());
                },
                async (action, token) =>
                {
                    CollectionRuleActionException fileNotFoundException = await Assert.ThrowsAsync<CollectionRuleActionException>(
                        () => action.StartAsync(token));

                    Assert.Equal(string.Format(Tools.Monitor.Strings.ErrorMessage_FileNotFound, uniquePathName), fileNotFoundException.Message);
                });
        }

        [Fact]
        public async Task ExecuteAction_IgnoreExitCode()
        {
            await ValidateAction(
                options =>
                {
                    options.Path = DotNetHost.GetPath();
                    options.Arguments = ExecuteActionTestHelper.GenerateArgumentsString(new string[] { ActionTestsConstants.NonzeroExitCode });
                    options.IgnoreExitCode = true;
                },
                async (action, token) =>
                {
                    await action.StartAsync(token);

                    CollectionRuleActionResult result = await action.WaitForCompletionAsync(token);

                    ValidateActionResult(result, "1");
                });
        }

        private static void ValidateActionResult(CollectionRuleActionResult result, string expectedExitCode)
        {
            string actualExitCode;

            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue("ExitCode", out actualExitCode));
            Assert.Equal(expectedExitCode, actualExitCode);
        }

        private static async Task ValidateAction(Action<ExecuteOptions> optionsCallback, Func<ICollectionRuleAction, CancellationToken, Task> actionCallback)
        {
            ExecuteActionFactory factory = new();

            ExecuteOptions options = new();

            optionsCallback(options);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

            ICollectionRuleAction action = factory.Create(null, options);

            try
            {
                await actionCallback(action, cancellationTokenSource.Token);
            }
            finally
            {
                await DisposableHelper.DisposeAsync(action);
            }
        }
    }
}

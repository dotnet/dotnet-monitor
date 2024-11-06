// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class ExceptionsTests
    {
        private const string FrameTypeName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario";
        private const string FrameMethodName = "ThrowAndCatchInvalidOperationException";
        private const string FrameParameterType = "System.Boolean";
        private const string SimpleFrameParameterType = "Boolean";
        private const string UnitTestAppModule = "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll";
        private const string CoreLibModuleName = "System.Private.CoreLib.dll";
        private const string SystemInvalidOperationException = "System.InvalidOperationException";
        private const string ExceptionMessage = $"Exception of type '{SystemInvalidOperationException}' was thrown.";
        private const string FirstChanceExceptionMessage = "First chance exception at";
        private const string CustomGenericsException = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomGenericsException`2[System.Int32,System.String]";
        private const string SystemArgumentNullException = "System.ArgumentNullException";
        private const string FilteringExceptionsScenario = TestAppScenarios.Exceptions.Name + " " + TestAppScenarios.Exceptions.SubScenarios.MultipleExceptions;

        private string exceptionsResult = string.Empty;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ExceptionsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsTextTest(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Exceptions.Name + " " + TestAppScenarios.Exceptions.SubScenarios.SingleException,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsWithEnvVarJsonTest(Architecture targetArchitecture)
        {
            await ExceptionsJsonTest(targetArchitecture, true);
        }

#if NET8_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsWithoutEnvVarJsonTest(Architecture targetArchitecture)
        {
            await ExceptionsJsonTest(targetArchitecture, false);
        }
#endif

        private async Task ExceptionsJsonTest(Architecture targetArchitecture, bool enableStartupHook)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Exceptions.Name + " " + TestAppScenarios.Exceptions.SubScenarios.SingleException,
                appValidate: async (appRunner, apiClient) =>
                {
                    DateTime startTime = DateTime.UtcNow.ToLocalTime();

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.NewlineDelimitedJson);

                    DateTime currentTime = DateTime.UtcNow.ToLocalTime();

                    var exceptionsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(exceptionsResult);

                    Assert.Equal("2", exceptionsDict["id"].ToString());
                    Assert.Equal(SystemInvalidOperationException, exceptionsDict["typeName"].ToString());

                    var timestamp = DateTime.Parse(exceptionsDict["timestamp"].ToString());
                    Assert.True(startTime < timestamp);
                    Assert.True(currentTime > timestamp);
                    Assert.Equal(CoreLibModuleName, exceptionsDict["moduleName"].ToString());
                    Assert.Equal(ExceptionMessage, exceptionsDict["message"].ToString());

                    var callStackResultsRootElement = JsonSerializer.SerializeToDocument(exceptionsDict["stack"]).RootElement;

                    Assert.NotEqual("0", callStackResultsRootElement.GetProperty("threadId").ToString());

                    var topFrame = callStackResultsRootElement.GetProperty("frames").EnumerateArray().FirstOrDefault();

                    Assert.Equal(FrameMethodName, topFrame.GetProperty("methodName").ToString());
                    Assert.Equal(2, topFrame.GetProperty("parameterTypes").GetArrayLength());
                    Assert.Equal(FrameParameterType, topFrame.GetProperty("parameterTypes")[0].ToString());
                    Assert.Equal(FrameParameterType, topFrame.GetProperty("parameterTypes")[1].ToString());
                    Assert.Equal(FrameTypeName, topFrame.GetProperty("typeName").ToString());
                    Assert.Equal(UnitTestAppModule, topFrame.GetProperty("moduleName").ToString());
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = enableStartupHook;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterNoIncludeExclude(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, new ExceptionsConfiguration());

                    ValidateMultipleExceptionsText(3, new() { CustomGenericsException, SystemInvalidOperationException, SystemArgumentNullException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeBasic(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemInvalidOperationException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeDetailed(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException,
                            MethodName = FrameMethodName,
                            TypeName = FrameTypeName,
                            ModuleName = UnitTestAppModule
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemArgumentNullException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeMultiple(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = CustomGenericsException
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeAndExclude(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ModuleName = UnitTestAppModule
                        }
                    );
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory(Skip = "Flaky")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeBasic(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory(Skip = "Flaky")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeMultiple(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    // This is effectively an OR that will include anything that matches either of the options
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            MethodName = FrameMethodName
                        }
                    );
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = CustomGenericsException
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemInvalidOperationException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeDetailed(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException,
                            MethodName = FrameMethodName,
                            TypeName = FrameTypeName,
                            ModuleName = UnitTestAppModule
                        }
                    );

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText, configuration);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeBasic_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeMultiple_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemInvalidOperationException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    // This is effectively an OR that will include anything that matches either of the options
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            MethodName = FrameMethodName
                        }
                    );
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = CustomGenericsException
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeDetailed_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException,
                            MethodName = FrameMethodName,
                            TypeName = FrameTypeName,
                            ModuleName = UnitTestAppModule
                        }
                    );
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeBasic_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemInvalidOperationException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeDetailed_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateMultipleExceptionsText(2, new() { CustomGenericsException, SystemArgumentNullException });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemInvalidOperationException,
                            MethodName = FrameMethodName,
                            TypeName = FrameTypeName,
                            ModuleName = UnitTestAppModule
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterExcludeMultiple_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = CustomGenericsException
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Exceptions_FilterIncludeAndExclude_Configuration(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                FilteringExceptionsScenario,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    ValidateSingleExceptionText(
                        SystemInvalidOperationException,
                        ExceptionMessage,
                        FrameTypeName,
                        FrameMethodName,
                        new List<string>() { SimpleFrameParameterType, SimpleFrameParameterType });
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    ExceptionsConfiguration configuration = new();
                    configuration.Include.Add(
                        new ExceptionFilter()
                        {
                            ModuleName = UnitTestAppModule
                        }
                    );
                    configuration.Exclude.Add(
                        new ExceptionFilter()
                        {
                            ExceptionType = SystemArgumentNullException
                        }
                    );

                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures().EnableExceptions().SetExceptionFiltering(configuration);
                });
        }

        private void ValidateMultipleExceptionsText(int exceptionsCount, List<string> exceptionTypes)
        {
            var exceptions = exceptionsResult.Split(new[] { FirstChanceExceptionMessage }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(exceptionsCount, exceptions.Length);

            foreach (string exceptionType in exceptionTypes)
            {
                Assert.Contains(exceptionType, exceptionsResult);
            }
        }

        private void ValidateSingleExceptionText(string exceptionType, string exceptionMessage, string frameTypeName, string frameMethodName, List<string> parameterTypes)
        {
            var exceptionsLines = exceptionsResult.Split(Environment.NewLine, StringSplitOptions.None);

            Assert.True(exceptionsLines.Length >= 3);
            Assert.Contains(FirstChanceExceptionMessage, exceptionsLines[0]);
            Assert.Equal($"{exceptionType}: {exceptionMessage}", exceptionsLines[1]);
            Assert.Equal($"   at {frameTypeName}.{frameMethodName}({string.Join(',', parameterTypes)})", exceptionsLines[2]);
        }

        private async Task GetExceptions(ApiClient apiClient, AppRunner appRunner, ExceptionFormat format, ExceptionsConfiguration configuration = null)
        {
            await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.Begin);

            int processId = await appRunner.ProcessIdTask;

            await RetryUtilities.RetryAsync(
                () => CaptureExtensions(apiClient, processId, format, configuration),
                shouldRetry: (Exception ex) => ex is ArgumentException,
                maxRetryCount: 5,
                outputHelper: _outputHelper);

            await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.End);
        }

        private async Task CaptureExtensions(ApiClient apiClient, int processId, ExceptionFormat format, ExceptionsConfiguration configuration)
        {
            await Task.Delay(500);

            ResponseStreamHolder holder;
            if (configuration != null)
            {
                holder = await apiClient.CaptureExceptionsAsync(configuration, processId, format);
            }
            else
            {
                holder = await apiClient.CaptureExceptionsAsync(processId, format);
            }

            using (var reader = new StreamReader(holder.Stream))
            {
                exceptionsResult = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(exceptionsResult))
            {
                throw new ArgumentException();
            }
        }
    }
}

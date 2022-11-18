// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class LoggerScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.Logger.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                ServiceProvider services = null;
                try
                {
                    ILoggerFactory loggerFactory;
                    const int maxRetryCount = 3;
                    int attemptIteration = 0;
                    while (true)
                    {
                        attemptIteration++;
                        try
                        {
                            services = new ServiceCollection()
                                .AddLogging(builder =>
                                {
                                    builder.AddEventSourceLogger();
                                    builder.AddFilter(null, LogLevel.None); // Default
                                    builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory1, LogLevel.Debug);
                                    builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory2, LogLevel.Information);
                                    builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Warning);
                                    builder.AddFilter(TestAppScenarios.Logger.Categories.SentinelCategory, LogLevel.Critical);
                                    builder.AddFilter(TestAppScenarios.Logger.Categories.FlushCategory, LogLevel.Critical);
                                }).BuildServiceProvider();

                            loggerFactory = services.GetRequiredService<ILoggerFactory>();

                            break;
                        }
                        catch (InvalidOperationException ex) when (
                            attemptIteration < maxRetryCount &&
                            ex.Message.Equals("Somebody else set the _disposable field", StringComparison.OrdinalIgnoreCase))
                        {
                            services?.Dispose();
                            // TESTFIX - Chained configuration building appears to contain a race condition
                            // https://github.com/dotnet/runtime/issues/36042
                            /*
                            [App1] Exception: System.InvalidOperationException: Somebody else set the _disposable field
                            [App1]    at Microsoft.Extensions.Primitives.ChangeToken.ChangeTokenRegistration`1.SetDisposable(IDisposable disposable)
                            [App1]    at Microsoft.Extensions.Primitives.ChangeToken.ChangeTokenRegistration`1.RegisterChangeTokenCallback(IChangeToken token)
                            [App1]    at Microsoft.Extensions.Primitives.ChangeToken.ChangeTokenRegistration`1..ctor(Func`1 changeTokenProducer, Action`1 changeTokenConsumer, TState state)
                            [App1]    at Microsoft.Extensions.Primitives.ChangeToken.OnChange[TState](Func`1 changeTokenProducer, Action`1 changeTokenConsumer, TState state)
                            [App1]    at Microsoft.Extensions.Options.OptionsMonitor`1.<.ctor>g__RegisterSource|6_0(IOptionsChangeTokenSource`1 source)
                            [App1]    at Microsoft.Extensions.Options.OptionsMonitor`1..ctor(IOptionsFactory`1 factory, IEnumerable`1 sources, IOptionsMonitorCache`1 cache)
                            [App1]    at System.RuntimeMethodHandle.InvokeMethod(Object target, Span`1& arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
                            [App1]    at System.Reflection.RuntimeConstructorInfo.Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor(Type serviceType)
                            [App1]    at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey key, Func`2 valueFactory)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(Type serviceType)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
                            [App1]    at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
                            [App1]    at Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.LoggerScenario.<>c.<<ExecuteAsync>b__1_0>d.MoveNext() in /Users/runner/work/1/s/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/Scenarios/LoggerScenario.cs:line 30
                            [App1] --- End of stack trace from previous location ---
                            [App1]    at Microsoft.Diagnostics.Monitoring.UnitTestApp.ScenarioHelpers.RunScenarioAsync(Func`2 func, CancellationToken token, Action`1 beforeReadyCallback) in /Users/runner/work/1/s/src/Tests/Microsoft.Diagnostics.Monitoring.UnitTestApp/ScenarioHelpers.cs:line 59
                            [App1] End Standard Error
                            */
                        }
                    }

                    await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Logger.Commands.StartLogging, logger);

                    ILogger cat1Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory1);
                    LogTraceMessage(cat1Logger);
                    LogDebugMessage(cat1Logger);
                    LogInformationMessage(cat1Logger);
                    LogWarningMessage(cat1Logger);
                    LogErrorMessage(cat1Logger);
                    LogCriticalMessage(cat1Logger);

                    ILogger cat2Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory2);
                    LogTraceMessage(cat2Logger);
                    LogDebugMessage(cat2Logger);
                    LogInformationMessage(cat2Logger);
                    LogWarningMessage(cat2Logger);
                    LogErrorMessage(cat2Logger);
                    LogCriticalMessage(cat2Logger);

                    ILogger cat3Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory3);
                    LogTraceMessage(cat3Logger);
                    LogDebugMessage(cat3Logger);
                    LogInformationMessage(cat3Logger);
                    LogWarningMessage(cat3Logger);
                    LogErrorMessage(cat3Logger);
                    LogCriticalMessage(cat3Logger);

                    ILogger sentinelCategory = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.SentinelCategory);
                    // This sentinel entry helps the logs tests to understand that they will not receive
                    // any more logging data that should be checked.
                    LogCriticalMessage(sentinelCategory);

                    // See: https://github.com/dotnet/runtime/issues/76704
                    // The log entries above may get stuck in buffers in the runtime eventing infra or
                    // in the trace event library event processor due to their close proximity in being emitted.
                    // To mitigate this, repeatedly wait a short time and send another log entry, which will cause the buffer
                    // to flush the existing entries. These log entries should be ignored by the logs tests.
                    ILogger flushCategory = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.FlushCategory);
                    // The number of times the flush entry is produced came about from imperical testing. Sending one seems
                    // to occassionally flush the entries through, but not often. Two entries "should" be enough: the first entry
                    // (if it doesn't flow through all the way) will initially get stuck in the runtime eventing buffer; the second
                    // entry will flush out the first entry. This second entry may be stuck in the runtime eventing buffer and
                    // the first one may be stuck in the trace event library buffer on the consumer side, however all of the relevant
                    // data entries that precede these flush entries should no longer be buffered. Sending any more "should" not
                    // be necessary unless another layer of buffering is in place.
                    for (int i = 0; i < 2; i++)
                    {
                        await Task.Delay(CommonTestTimeouts.EventSourceBufferAvoidanceTimeout);

                        LogCriticalMessage(flushCategory);
                    }
                }
                finally
                {
                    services?.Dispose();
                }

                return 0;
            }, context.GetCancellationToken());
        }

        private static void LogTraceMessage(ILogger logger)
        {
            logger.LogTrace(new EventId(1, "EventIdTrace"), "Trace message with values {Value1} and {Value2}.", 3, true);
        }

        private static void LogDebugMessage(ILogger logger)
        {
            logger.LogDebug(new EventId(1, "EventIdDebug"), "Debug message with values {Value1} and {Value2}.", new Guid("F39A5065-732B-4CCE-89D1-52E4AF39E233"), null);
        }

        private static void LogInformationMessage(ILogger logger)
        {
            logger.LogInformation("Information message with values {Value1} and {Value2}.", "hello", "goodbye");
        }

        private static void LogWarningMessage(ILogger logger)
        {
            logger.Log(
            LogLevel.Warning,
            new EventId(5, "EventIdWarning"),
            new CustomLogState(
            "Warning message with custom state.",
            new string[] { "KeyA", "Key2", "KeyZ" },
            new object[] { 4, 'p', LogLevel.Error }),
            null,
            CustomLogState.Formatter);
        }

        private static void LogErrorMessage(ILogger logger)
        {
            logger.LogError(new EventId(1, "EventIdError"), "Error message with values {Value1} and {Value2}.", 'a', new IntPtr(42));
        }

        private static void LogCriticalMessage(ILogger logger)
        {
            try
            {
                throw new InvalidOperationException("Application is shutting down.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogCritical(new EventId(1, "EventIdCritical"), ex, "Critical message.");
            }
        }

        private readonly struct CustomLogState : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<CustomLogState, Exception, string> Formatter = (state, exception) => $"'{state._message}' with {state._keys.Length} state values.";

            private readonly string[] _keys;
            private readonly string _message;
            private readonly object[] _values;

            public CustomLogState(string message, string[] keys, object[] values)
            {
                _message = message;
                _keys = keys ?? throw new ArgumentNullException(nameof(keys));
                _values = values ?? throw new ArgumentNullException(nameof(values));

                if (_keys.Length != _values.Length)
                {
                    throw new ArgumentException($"{nameof(keys)} and {nameof(values)} must have the same length.");
                }
            }

            public KeyValuePair<string, object> this[int index] => new KeyValuePair<string, object>(_keys[index], _values[index]);

            public int Count => _keys.Length;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                return _message;
            }
        }
    }
}

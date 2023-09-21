// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class ParameterCapturingLoggerTests
    {
        private readonly MethodInfo _testMethod = typeof(ParameterCapturingLoggerTests).GetMethod(nameof(TestMethod), BindingFlags.Static | BindingFlags.NonPublic);
        private static void TestMethod() { }

        [Theory]
        [InlineData(ParameterCaptureMode.Inline, typeof(DotnetMonitor.ParameterCapture.UserCode))]
        [InlineData(ParameterCaptureMode.Background, typeof(DotnetMonitor.ParameterCapture.SystemCode))]
        internal void LoggingCategories(ParameterCaptureMode mode, Type categoryType)
        {
            // Arrange & Act
            IList<LogRecordEntry> entries = TestCore(mode);

            // Assert
            LogRecordEntry entry = Assert.Single(entries);
            Assert.Equal(categoryType.FullName, entry.Category);

            return;
        }

        [Theory]
        [InlineData(ParameterCaptureMode.Inline)]
        [InlineData(ParameterCaptureMode.Background)]
        internal void ScopeData(ParameterCaptureMode mode)
        {
            // Arrange
            using Activity loggingActivity = new("ScopeDataTest");
            Activity.Current = loggingActivity;
            loggingActivity.Start();

            Dictionary<string, object> expectedScope = new()
            {
                { ParameterCapturingLogger.Scopes.ActivityId, loggingActivity.Id },
                { ParameterCapturingLogger.Scopes.ActivityIdFormat, loggingActivity.IdFormat },
                { ParameterCapturingLogger.Scopes.CaptureSite.MethodName, _testMethod.Name },
                { ParameterCapturingLogger.Scopes.CaptureSite.TypeName, _testMethod.DeclaringType.FullName },
                { ParameterCapturingLogger.Scopes.CaptureSite.ModuleName, _testMethod.Module.Name },
            };

            // Act
            IList<LogRecordEntry> entries = TestCore(mode);

            // Assert
            LogRecordEntry entry = Assert.Single(entries);
            IReadOnlyList<KeyValuePair<string, object>> rawScope = Assert.Single(entry.Scopes);

            Dictionary<string, object> scopeData = new(rawScope);

            // Validate variant data first.

            // Timestamp
            Assert.True(scopeData.Remove(ParameterCapturingLogger.Scopes.TimeStamp, out object rawTimeStamp));
            string timeStampStr = Assert.IsType<string>(rawTimeStamp);
            Assert.True(DateTime.TryParse(timeStampStr, out _));

            // Thread id
            Assert.True(scopeData.Remove(ParameterCapturingLogger.Scopes.ThreadId, out object rawThreadId));
            int threadId = Assert.IsType<int>(rawThreadId);
            Assert.Equal(Environment.CurrentManagedThreadId, threadId);

            // Static data
            Assert.Equal(expectedScope, scopeData);

            return;
        }

        private IList<LogRecordEntry> TestCore(ParameterCaptureMode mode)
        {
            Assert.NotNull(_testMethod);

            // Arrange
            LogRecord logRecord = new();
            ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(new TestLoggerProvider(logRecord)));

            MethodTemplateString message = new(_testMethod);

            // Act
            using (ParameterCapturingLogger logger = new(factory.CreateLogger<DotnetMonitor.ParameterCapture.UserCode>(), factory.CreateLogger<DotnetMonitor.ParameterCapture.SystemCode>()))
            {
                logger.Log(mode, message, Array.Empty<string>());

                // Force the logger to drain the background queue before we dispose it.
                logger.Complete();
            }

            // Assert
            return logRecord.Events;
        }
    }
}

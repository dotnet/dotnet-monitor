// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class SystemLoggingTests
    {
        [Fact]
        public void TestLoggingCategories()
        {
            var logRecord = new LogRecord();
            var factory = LoggerFactory.Create(builder => builder.AddProvider(new TestLoggerProvider(logRecord)));

            const string inlineMessage = "Inline message";
            const string backgroundMessage = "Background message";

            using (ParameterCapturingLogger logger = new(factory.CreateLogger<DotnetMonitor.ParameterCapture.UserCode>(), factory.CreateLogger<DotnetMonitor.ParameterCapture.SystemCode>()))
            {
                logger.Log(ParameterCaptureMode.Inline, inlineMessage, Array.Empty<string>());
                logger.Log(ParameterCaptureMode.Background, backgroundMessage, Array.Empty<string>());
            }

            Assert.Equal(2, logRecord.Events.Count);

            var userCodeEntry = logRecord.Events.First(e => e.Message == inlineMessage);
            Assert.Equal(typeof(DotnetMonitor.ParameterCapture.UserCode).FullName, userCodeEntry.Category);

            var systemEntry = logRecord.Events.First(e => e.Message == backgroundMessage);
            Assert.Equal(typeof(DotnetMonitor.ParameterCapture.SystemCode).FullName, systemEntry.Category);

            return;
        }
    }
}

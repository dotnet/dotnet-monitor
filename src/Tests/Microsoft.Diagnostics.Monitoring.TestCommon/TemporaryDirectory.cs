// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly ITestOutputHelper _outputHelper;

        public TemporaryDirectory(ITestOutputHelper outputhelper)
        {
            _outputHelper = outputhelper;

            // AGENT_TEMPDIRECTORY is an AzureDevops variable which is set to a path
            // that is cleaned up after every job.
            string topLevelTempDir = Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY");
            if (string.IsNullOrEmpty(topLevelTempDir))
            {
                topLevelTempDir = Path.GetTempPath();
            }

            string tempDir = Path.Combine(topLevelTempDir, Path.GetRandomFileName());
            Assert.False(Directory.Exists(tempDir));

            _directoryInfo = Directory.CreateDirectory(tempDir);
            _outputHelper.WriteLine("Created temporary directory '{0}'", FullName);
        }

        public void Dispose()
        {
            try
            {
                _directoryInfo?.Delete(recursive: true);
                _outputHelper.WriteLine("Removed temporary directory '{0}'", FullName);
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine("Failed to remove temporary directory '{0}': {1}", FullName, ex.Message);
            }
        }

        public string FullName => _directoryInfo.FullName;
    }
}

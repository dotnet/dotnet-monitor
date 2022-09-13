// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace UnitTests
{
    internal class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly ITestOutputHelper _outputHelper;

        public TemporaryDirectory(ITestOutputHelper outputhelper)
        {
            _outputHelper = outputhelper;

            _directoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            _directoryInfo.Create();

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

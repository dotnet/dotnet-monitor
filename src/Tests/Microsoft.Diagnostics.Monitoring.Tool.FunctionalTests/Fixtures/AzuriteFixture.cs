// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// https://github.com/Azure/azure-sdk-for-net/blob/4162f6fa2445b2127468b9cfd080f01c9da88eba/sdk/storage/Azure.Storage.Common/tests/Shared/AzuriteFixture.cs

using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.DotNet.XUnitExtensions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures
{
    public class AzuriteAccount
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public int BlobsPort { get; set; }
        public int QueuesPort { get; set; }

        public string BlobEndpoint => $"http://127.0.0.1:{BlobsPort}/{Name}";
        public string QueueEndpoint => $"http://127.0.0.1:{QueuesPort}/{Name}";

        public string ConnectionString => $"DefaultEndpointsProtocol=http;AccountName={Name};AccountKey={Key};BlobEndpoint={BlobEndpoint};QueueEndpoint={QueueEndpoint};";
    }

    /// <summary>
    /// This class manages Azurite Lifecycle for a test class.
    /// - Creates accounts pool, so that each test has own account, bump up pool size if you're running out of accounts
    /// - Starts Azurite process
    /// - Tears down Azurite process after test class is run
    /// It requires Azurite V3. See installation instructions here https://github.com/Azure/Azurite.
    /// After installing Azurite define env variable AZURE_AZURITE_LOCATION that points to azurite installation
    /// </summary>
    public class AzuriteFixture : IDisposable
    {
        private const string AzuriteLocationKey = "AZURE_AZURITE_LOCATION";

        private Process _azuriteProcess;
        private readonly TemporaryDirectory _workspaceDirectory;
        private readonly CountdownEvent _startupCountdownEvent;

        private readonly StringBuilder _azuriteStartupStdout;
        private readonly StringBuilder _azuriteStartupStderr;

        private readonly string _startupError;

        public AzuriteAccount Account { get; }

        public AzuriteFixture()
        {
            Account = new AzuriteAccount()
            {
                Name = Guid.NewGuid().ToString("N"),
                Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
            };

            _startupCountdownEvent = new CountdownEvent(initialCount: 2); // Wait for Port and Queue services
            _workspaceDirectory = new TemporaryDirectory(new ConsoleOutputHelper());
            _azuriteProcess = new Process()
            {
                StartInfo = ConstructAzuriteProcessStartInfo(Account, _workspaceDirectory.FullName)
            };

            _azuriteStartupStdout = new();
            _azuriteStartupStderr = new();

            _azuriteProcess.OutputDataReceived += ParseAzuriteStartupOutput;
            _azuriteProcess.ErrorDataReceived += ParseAzuriteStartupError;

            try
            {
                _azuriteProcess.Start();
            }
            catch (Exception ex)
            {
                _startupError = ErrorMessage($"failed to start azurite with exception: {ex.Message}");
                _azuriteProcess.Dispose();
                _azuriteProcess = null;
                return;
            }

            _azuriteProcess.BeginOutputReadLine();
            _azuriteProcess.BeginErrorReadLine();

            bool didAzuriteStart = _startupCountdownEvent.Wait(TestTimeouts.AzuriteInitializationTimeout);
            if (!didAzuriteStart)
            {
                if (_azuriteProcess.HasExited)
                {
                    throw new InvalidOperationException($"azurite could not start with following output:\n{_azuriteStartupStdout}\nerror:\n{_azuriteStartupStderr}\nexit code:{_azuriteProcess.ExitCode}");
                }
                else
                {
                    _azuriteProcess.Kill();
                    _azuriteProcess.WaitForExit(TestTimeouts.AzuriteTeardownTimeout.Milliseconds);
                    throw new InvalidOperationException($"azurite could not initialize within timeout with following output:\n{_azuriteStartupStdout}\nerror:\n{_azuriteStartupStderr}");
                }
            }
        }

        private static ProcessStartInfo ConstructAzuriteProcessStartInfo(AzuriteAccount authorizedAccount, string workspaceDirectory)
        {
            string azuriteFolder = Environment.GetEnvironmentVariable(AzuriteLocationKey);
            if (azuriteFolder == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string defaultAzuriteFolder = Path.Combine(Environment.GetEnvironmentVariable("VSAPPIDDIR") ?? string.Empty, "Extensions", "Microsoft", "Azure Storage Emulator");
                if (Directory.Exists(defaultAzuriteFolder))
                {
                    azuriteFolder = defaultAzuriteFolder;
                }
            }

            string azuriteExecutablePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                azuriteExecutablePath = Path.Combine(azuriteFolder ?? string.Empty, "azurite.exe");
            }
            else
            {
                azuriteExecutablePath = Path.Combine(azuriteFolder ?? string.Empty, "azurite");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = azuriteExecutablePath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            startInfo.ArgumentList.Add("--skipApiVersionCheck");

            // Use a temporary directory to store data
            startInfo.ArgumentList.Add("--location");
            startInfo.ArgumentList.Add(workspaceDirectory);

            // Auto pick port
            startInfo.ArgumentList.Add("--blobPort");
            startInfo.ArgumentList.Add("0");

            // Auto pick port
            startInfo.ArgumentList.Add("--queuePort");
            startInfo.ArgumentList.Add("0");

            startInfo.EnvironmentVariables.Add("AZURITE_ACCOUNTS", $"{authorizedAccount.Name}:{authorizedAccount.Key}");

            return startInfo;
        }

        public void SkipTestIfNotInitialized()
        {
            if (_startupError != null)
            {
                throw new SkipTestException(_startupError);
            }
        }

        private void ParseAzuriteStartupOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            if (!_startupCountdownEvent.IsSet)
            {
                _azuriteStartupStdout.AppendLine(e.Data);

                if (e.Data.Contains("Azurite Blob service is successfully listening at"))
                {
                    Account.BlobsPort = ParseAzuritePort(e.Data);
                    _startupCountdownEvent.Signal();
                }
                else if (e.Data.Contains("Azurite Queue service is successfully listening at"))
                {
                    Account.QueuesPort = ParseAzuritePort(e.Data);
                    _startupCountdownEvent.Signal();
                }
            }
        }

        private void ParseAzuriteStartupError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            if (!_startupCountdownEvent.IsSet)
            {
                _azuriteStartupStderr.AppendLine(e.Data);
            }
        }

        private int ParseAzuritePort(string outputLine)
        {
            int indexFrom = outputLine.LastIndexOf(':') + 1;
            return int.Parse(outputLine.Substring(indexFrom));
        }

        private string ErrorMessage(string specificReason)
        {
            return $"Could not run Azurite based test: {specificReason}.\n" +
                "Make sure that:\n" +
                "- Azurite V3 is installed either via Visual Studio or NPM (see https://docs.microsoft.com/azure/storage/common/storage-use-azurite#install-azurite for instructions)\n" +
                $"- {AzuriteLocationKey} envorinment is set and pointing to location of directory that has 'azurite' executable\n";
        }

        public void Dispose()
        {
            if (_azuriteProcess != null)
            {
                if (!_azuriteProcess.HasExited)
                {
                    _azuriteProcess.Kill();
                    _azuriteProcess.WaitForExit(TestTimeouts.AzuriteTeardownTimeout.Milliseconds);
                    _azuriteProcess.Dispose();
                    _azuriteProcess = null;
                }
            }

            if (_workspaceDirectory != null)
            {
                _workspaceDirectory.Dispose();
            }
        }
    }
}

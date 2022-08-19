// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// https://github.com/Azure/azure-sdk-for-net/blob/4162f6fa2445b2127468b9cfd080f01c9da88eba/sdk/storage/Azure.Storage.Common/tests/Shared/AzuriteFixture.cs

using Microsoft.DotNet.XUnitExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Fixtures.Azurite
{
    public class AzuriteAccount
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public int BlobsPort { get; set; }
        public int QueuesPort { get; set; }

        public int TablesPort { get; set; }

        public string BlobEndpoint => $"http://127.0.0.1:{BlobsPort}/{Name}";
        public string QueueEndpoint => $"http://127.0.0.1:{QueuesPort}/{Name}";
        public string TableEndpoint => $"http://127.0.0.1:{TablesPort}/{Name}";


        public string ConnectionString => $"DefaultEndpointsProtocol=http;AccountName={Name};AccountKey={Key};BlobEndpoint={BlobEndpoint};QueueEndpoint={QueueEndpoint};TableEndpoint={TableEndpoint}";
    }

    /// <summary>
    /// This class manages Azurite Lifecycle for a test class.
    /// - Creates accounts pool, so that each test has own account, bump up pool size if you're running out of accounts
    /// - Starts Azurite process
    /// - Tears down Azurite process after test class is run
    /// It requires Azurite V3. See installation instructions here https://github.com/Azure/Azurite.
    /// After installing Azurite define env variable AZURITE_LOCATION that points to azurite installation
    /// </summary>
    public class AzuriteFixture : IDisposable
    {
        private const string AzuriteLocationKey = "AZURITE_LOCATION";

        private Process _azuriteProcess;
        private readonly TemporaryDirectory _workspaceDirectory;
        private readonly CountdownEvent _startupCountdownEvent;

        private readonly StringBuilder _azuriteStartupStdout;
        private readonly StringBuilder _azuriteStartupStderr;

        private readonly string _startupError;

        public AzuriteAccount Account { get; }

        public AzuriteFixture()
        {
            // Check if the tests are running on a pipeline build machine.
            // If so, Azurite must succesfully initialize otherwise mark the dependent tests as failed
            // to avoid hidding failures in our CI.
            bool isPipelineBuildMachine = Environment.GetEnvironmentVariable("TF_BUILD") != null;
            isPipelineBuildMachine = true;
            Account = new AzuriteAccount()
            {
                Name = Guid.NewGuid().ToString("N"),
                Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
            };

            _startupCountdownEvent = new CountdownEvent(initialCount: 3); // Wait for the Blob, Queue, and Table services to start
            _workspaceDirectory = new TemporaryDirectory(new ConsoleOutputHelper());

            try
            {
                _azuriteProcess = new Process()
                {
                    StartInfo = ConstructAzuriteProcessStartInfo(Account, _workspaceDirectory.FullName)
                };
            }
            catch (FileNotFoundException ex)
            {
                _startupError = ErrorMessage(ex.Message);
                if (isPipelineBuildMachine)
                {
                    throw new InvalidOperationException(_startupError, ex);
                }

                return;
            }

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

                if (isPipelineBuildMachine)
                {
                    throw new InvalidOperationException(_startupError, ex);
                }

                return;
            }

            _azuriteProcess.BeginOutputReadLine();
            _azuriteProcess.BeginErrorReadLine();

            bool didAzuriteStart = _startupCountdownEvent.Wait(CommonTestTimeouts.AzuriteInitializationTimeout);
            if (!didAzuriteStart)
            {
                if (_azuriteProcess.HasExited)
                {
                    throw new InvalidOperationException($"azurite could not start with following output:\n{_azuriteStartupStdout}\nerror:\n{_azuriteStartupStderr}\nexit code:{_azuriteProcess.ExitCode}");
                }
                else
                {
                    _azuriteProcess.Kill();
                    _azuriteProcess.WaitForExit(CommonTestTimeouts.AzuriteTeardownTimeout.Milliseconds);
                    throw new InvalidOperationException($"azurite could not initialize within timeout with following output:\n{_azuriteStartupStdout}\nerror:\n{_azuriteStartupStderr}");
                }
            }
        }

        private ProcessStartInfo ConstructAzuriteProcessStartInfo(AzuriteAccount authorizedAccount, string workspaceDirectory)
        {
            string azuriteFolder = Environment.GetEnvironmentVariable(AzuriteLocationKey);

            bool isVSCopy = false;
            if (azuriteFolder == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string vsAppDir = Environment.GetEnvironmentVariable("VSAPPIDDIR");
                if (vsAppDir != null)
                {
                    string vsAzuriteFolder = Path.Combine(vsAppDir, "Extensions", "Microsoft", "Azure Storage Emulator");
                    if (Directory.Exists(vsAzuriteFolder))
                    {
                        azuriteFolder = vsAzuriteFolder;
                        isVSCopy = true;
                    }
                }
            }

            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            string azuriteExecutable;
            if (isVSCopy)
            {
                azuriteExecutable = "azurite.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                azuriteExecutable = "azurite.cmd";
            }
            else
            {
                azuriteExecutable = "azurite";
            }

            startInfo.FileName = Path.Combine(azuriteFolder ?? string.Empty, azuriteExecutable);

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

            // Auto pick port
            startInfo.ArgumentList.Add("--tablePort");
            startInfo.ArgumentList.Add("0");

            startInfo.EnvironmentVariables.Add("AZURITE_ACCOUNTS", $"{authorizedAccount.Name}:{authorizedAccount.Key}");

            return startInfo;
        }

        public void SkipTestIfNotAvailable()
        {
            if (_startupError != null)
            {
                throw new SkipTestException(_startupError);
            }
        }

        private void ParseAzuriteStartupOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null || _startupCountdownEvent.IsSet)
            {
                return;
            }

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
            else if (e.Data.Contains("Azurite Table service is successfully listening at"))
            {
                Account.TablesPort = ParseAzuritePort(e.Data);
                _startupCountdownEvent.Signal();
            }
        }

        private void ParseAzuriteStartupError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null || _startupCountdownEvent.IsSet)
            {
                return;
            }

            _azuriteStartupStderr.AppendLine(e.Data);
        }

        private int ParseAzuritePort(string outputLine)
        {
            int portDelimiterIndex = outputLine.LastIndexOf(':') + 1;
            if (portDelimiterIndex == 0 || portDelimiterIndex >= outputLine.Length)
            {
                throw new InvalidOperationException($"azurite stdout did not follow the expected format, cannot parse port information. Unexpected output: {outputLine}");
            }

            return int.Parse(outputLine[portDelimiterIndex..]);
        }

        private string ErrorMessage(string specificReason)
        {
            return $"Could not run Azurite based test: {specificReason}.\n" +
                "Make sure that:\n" +
                "- Azurite V3 is installed either via Visual Studio or NPM (see https://docs.microsoft.com/azure/storage/common/storage-use-azurite#install-azurite for instructions)\n" +
                "- If installing through NPM, ensure that the directory that has 'azurite' executable is in the 'PATH' environment variable\n";
        }

        public void Dispose()
        {
            if (_azuriteProcess != null)
            {
                if (!_azuriteProcess.HasExited)
                {
                    _azuriteProcess.Kill();
                    _azuriteProcess.WaitForExit(CommonTestTimeouts.AzuriteTeardownTimeout.Milliseconds);
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

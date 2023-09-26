// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file and its classes are derived from:
// https://github.com/Azure/azure-sdk-for-net/blob/Azure.ResourceManager_1.3.1/sdk/storage/Azure.Storage.Common/tests/Shared/AzuriteFixture.cs
//

using Microsoft.DotNet.XUnitExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Fixtures
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
    /// It requires Azurite V3. See installation instructions here https://github.com/Azure/Azurite.
    /// </summary>
    public class AzuriteFixture : IDisposable
    {
        private Process _azuriteProcess;

        private readonly TemporaryDirectory _workspaceDirectory;
        private readonly CountdownEvent _startupCountdownEvent = new CountdownEvent(initialCount: 3); // Wait for the Blob, Queue, and Table services to start

        private readonly StringBuilder _azuriteStartupStdout = new();
        private readonly StringBuilder _azuriteStartupStderr = new();
        private readonly string _startupErrorMessage;

        public AzuriteAccount Account { get; }

        public AzuriteFixture()
        {
            // Check if the tests are running on a pipeline build machine.
            // If so, Azurite must successfully initialize otherwise mark the dependent tests as failed
            // to avoid hiding failures in our CI.
            //
            // Workaround: for now allow Windows environments to skip Azurite based tests due to configuration
            // issues in the Pipeline environment.
            bool mustInitialize = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_AZURITE_MUST_INITIALIZE"));

            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            Account = new AzuriteAccount()
            {
                Name = Guid.NewGuid().ToString("N"),
                Key = Convert.ToBase64String(key),
            };

            _workspaceDirectory = new TemporaryDirectory(new ConsoleOutputHelper());
            _azuriteProcess = new Process()
            {
                StartInfo = ConstructAzuriteProcessStartInfo(Account, _workspaceDirectory.FullName)
            };

            _azuriteProcess.OutputDataReceived += ParseAzuriteStartupOutput;
            _azuriteProcess.ErrorDataReceived += ParseAzuriteStartupError;

            try
            {
                _azuriteProcess.Start();
            }
            catch (Exception ex)
            {
                _startupErrorMessage = ErrorMessage($"failed to start azurite with exception: {ex.Message}");

                if (mustInitialize)
                {
                    throw new InvalidOperationException(_startupErrorMessage, ex);
                }

                _azuriteProcess = null;
                return;
            }

            _azuriteProcess.BeginOutputReadLine();
            _azuriteProcess.BeginErrorReadLine();

            bool didAzuriteStart = _startupCountdownEvent.Wait(CommonTestTimeouts.AzuriteInitializationTimeout);
            if (!didAzuriteStart)
            {
                // If we were able to launch the azurite process but initialization failed, mark the tests as failed
                // even for non-pipeline machines.
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

        private static ProcessStartInfo ConstructAzuriteProcessStartInfo(AzuriteAccount authorizedAccount, string workspaceDirectory)
        {
            bool isVSCopy = false;
            string azuriteFolder = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                RedirectStandardOutput = true,
                CreateNoWindow = true
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
            if (_startupErrorMessage != null)
            {
                throw new SkipTestException(_startupErrorMessage);
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

        private static int ParseAzuritePort(string outputLine)
        {
            int portDelimiterIndex = outputLine.LastIndexOf(':') + 1;
            if (portDelimiterIndex == 0 || portDelimiterIndex >= outputLine.Length)
            {
                throw new InvalidOperationException($"azurite stdout did not follow the expected format, cannot parse port information. Unexpected output: {outputLine}");
            }

            return int.Parse(outputLine[portDelimiterIndex..]);
        }

        private static string ErrorMessage(string specificReason)
        {
            return $"Could not run Azurite based test: {specificReason}.\n" +
                "Make sure that:\n" +
                "- Azurite V3 is installed either via Visual Studio 2022 (or later) or NPM (see https://docs.microsoft.com/azure/storage/common/storage-use-azurite#install-azurite for instructions)\n" +
                "- Ensure that the directory that has 'azurite' executable is in the 'PATH' environment variable if not launching tests through Test Explorer in Visual Studio\n";
        }

        public void Dispose()
        {
            if (_azuriteProcess?.HasExited == false)
            {
                _azuriteProcess.Kill();
                _azuriteProcess.WaitForExit(CommonTestTimeouts.AzuriteTeardownTimeout.Milliseconds);
            }

            _azuriteProcess?.Dispose();
            _workspaceDirectory?.Dispose();
        }
    }
}

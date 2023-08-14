// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool. This runner is for the "generatekey" command.
    /// </summary>
    internal sealed class MonitorGenerateKeyRunner : MonitorRunner
    {
        // Completion source containing the bearer token emitted by the generatekey command
        private readonly TaskCompletionSource<string> _bearerTokenTaskSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Regex _bearerTokenRegex =
            new Regex("^Authorization: Bearer (?<token>[a-zA-Z0-9_-]+\\.[a-zA-Z0-9_-]+\\.[a-zA-Z0-9_-]+)$", RegexOptions.Compiled);
        private readonly Regex _authorizationHeaderRegex =
            new Regex("^Bearer (?<token>[a-zA-Z0-9_-]+\\.[a-zA-Z0-9_-]+\\.[a-zA-Z0-9_-]+)$", RegexOptions.Compiled);

        // Completion source containing the format type emitted by the generatekey command
        private readonly TaskCompletionSource<string> _formatHeaderSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Regex _formatHeaderRegex =
            new Regex("^Settings in (?<format>[a-zA-Z0-9-_]+) format:$", RegexOptions.Compiled);

        // Completion source containing the Subject field emitted by the generatekey command
        private readonly TaskCompletionSource<string> _subjectSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Dictionary<OutputFormat, Regex> _subjectRegexMap =
            new Dictionary<OutputFormat, Regex>()
            {
                { OutputFormat.MachineJson, null },
                { OutputFormat.Json, null },
                { OutputFormat.Text, new Regex("Subject:\\s*(?<subject>[0-9a-zA-Z-_!@#\\$%\\^&\\*\\(\\)\\{\\}\\[\\]|\\,\\.;:/]+)\\Z", RegexOptions.Compiled) },
                { OutputFormat.Cmd, new Regex("set\\s*Authentication__MonitorApiKey__Subject=(?<subject>[0-9a-zA-Z-_!@#\\$%\\^&\\*\\(\\)\\{\\}\\[\\]|\\,\\.;:/]+)\\Z", RegexOptions.Compiled) },
                { OutputFormat.PowerShell, new Regex("\\$env\\:Authentication__MonitorApiKey__Subject\\s*=\\s*\\\"(?<subject>[0-9a-zA-Z-_!@#\\$%\\^&\\*\\(\\)\\{\\}\\[\\]|\\,\\.;:/]+)\\\"", RegexOptions.Compiled) },
                { OutputFormat.Shell, new Regex("export\\s*Authentication__MonitorApiKey__Subject=\\\"(?<subject>[0-9a-zA-Z-_!@#\\$%\\^&\\*\\(\\)\\{\\}\\[\\]|\\,\\.;:/]+)\\\"", RegexOptions.Compiled) },
            };

        // Completion source containing the PublicKey field emitted by the generatekey command
        private readonly TaskCompletionSource<string> _publicKeySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Dictionary<OutputFormat, Regex> _publicKeyRegexMap =
            new Dictionary<OutputFormat, Regex>()
            {
                { OutputFormat.MachineJson, null },
                { OutputFormat.Json, null },
                { OutputFormat.Text, new Regex("Public Key:\\s(?<publickey>[a-zA-Z0-9_-]{2,}?)\\Z", RegexOptions.Compiled) },
                { OutputFormat.Cmd, new Regex("set\\s*Authentication__MonitorApiKey__PublicKey=(?<publickey>[a-zA-Z0-9_-]{2,}?)\\Z", RegexOptions.Compiled) },
                { OutputFormat.PowerShell, new Regex("\\$env\\:Authentication__MonitorApiKey__PublicKey\\s*=\\s*\\\"(?<publickey>[a-zA-Z0-9_-]{2,}?)\\\"", RegexOptions.Compiled) },
                { OutputFormat.Shell, new Regex("export\\s*Authentication__MonitorApiKey__PublicKey=\\\"(?<publickey>[a-zA-Z0-9_-]{2,}?)\\\"", RegexOptions.Compiled) },
            };

        // Completion source containing the output in the specified format (this is everything after the _formatHeaderRegex line)
        private readonly TaskCompletionSource<string> _outputSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private StringBuilder _outputBuilder = new();

        // String builder that has the full output this will include any headers
        private StringBuilder _fullOutputBuilder = new();

        /// <summary>
        /// Gets the expected default <see cref="OutputFormat"/> when no --output parameter is specified at the command line.
        /// </summary>
        private const OutputFormat DefaultOutputFormat = OutputFormat.Json;
        /// <summary>
        /// A bool indicating if <see cref="StartAsync(CancellationToken)"/> has been called and the field <see cref="_selectedFormat"/> should not be updated.
        /// </summary>
        private bool _executionStarted;
        /// <summary>
        /// A value indicating which format is being used. This should not be updated after <see cref="StartAsync(CancellationToken)"/> is called.
        /// </summary>
        private OutputFormat? _selectedFormat;

        /// <summary>
        /// Gets the <see cref="OutputFormat"/> that was used to execute dotnet-monitor. If <see cref="Format"/> is set to <see langword="null"/> then
        /// the default <see cref="OutputFormat"/> is returned.
        /// </summary>
        /// <exception cref="InvalidOperationException">Will be thrown when <see cref="StartAsync(CancellationToken)"/> has not yet been called.</exception>
        public OutputFormat FormatUsed
        {
            get
            {
                if (!_executionStarted)
                {
                    throw new InvalidOperationException($"Can't get {nameof(FormatUsed)} until {nameof(StartAsync)} is called.");
                }

                return _selectedFormat ?? DefaultOutputFormat;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="OutputFormat"/> to be used while executing dotnet-monitor.
        /// This can not be set after <see cref="StartAsync(CancellationToken)"/> is called.
        /// </summary>
        public OutputFormat? Format
        {
            get
            {
                return _selectedFormat;
            }
            set
            {
                if (_executionStarted)
                {
                    throw new InvalidOperationException($"Can't set {nameof(Format)} after {nameof(StartAsync)} is called.");
                }

                _selectedFormat = value;
            }
        }

        public MonitorGenerateKeyRunner(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        public async Task StartAsync(CancellationToken token)
        {
            _executionStarted = true;

            List<string> argsList = new();

            const string command = "generatekey";

            if (null != Format)
            {
                argsList.Add("--output");
                argsList.Add(Format.ToString());
            }

            await base.StartAsync(command, argsList.ToArray(), token);
        }

        public override async Task WaitForExitAsync(CancellationToken token)
        {
            await base.WaitForExitAsync(token).ConfigureAwait(false);

            Assert.True(_outputSource.TrySetResult(_outputBuilder.ToString()));

            if (FormatUsed == OutputFormat.Json)
            {
                RootOptions parsedOpts = JsonSerializer.Deserialize<RootOptions>(_outputBuilder.ToString());

                Assert.True(_subjectSource.TrySetResult(parsedOpts?.Authentication?.MonitorApiKey?.Subject));
                Assert.True(_publicKeySource.TrySetResult(parsedOpts?.Authentication?.MonitorApiKey?.PublicKey));
            }
            else if (FormatUsed == OutputFormat.MachineJson)
            {
                ExpectedMachineOutputFormat parsedPayload = JsonSerializer.Deserialize<ExpectedMachineOutputFormat>(_fullOutputBuilder.ToString());

                Assert.NotNull(parsedPayload);

                Match tokenMatch = _authorizationHeaderRegex.Match(parsedPayload.AuthorizationHeader);
                if (tokenMatch.Success)
                {
                    string tokenValue = tokenMatch.Groups["token"].Value;
                    _outputHelper.WriteLine($"Found Bearer Token: {tokenValue}");
                    Assert.True(_bearerTokenTaskSource.TrySetResult(tokenValue));
                }

                Assert.True(_subjectSource.TrySetResult(parsedPayload.Authentication?.MonitorApiKey?.Subject));
                Assert.True(_publicKeySource.TrySetResult(parsedPayload.Authentication?.MonitorApiKey?.PublicKey));
            }
        }

        protected override void StandardOutputCallback(string line)
        {
            _fullOutputBuilder.AppendLine(line);
            if (_formatHeaderSource.Task.IsCompletedSuccessfully)
            {
                _outputBuilder.AppendLine(line);
            }

            Match tokenMatch = _bearerTokenRegex.Match(line);
            if (tokenMatch.Success)
            {
                string tokenValue = tokenMatch.Groups["token"].Value;
                _outputHelper.WriteLine($"Found Bearer Token: {tokenValue}");
                Assert.True(_bearerTokenTaskSource.TrySetResult(tokenValue));
            }

            Match formatMatch = _formatHeaderRegex.Match(line);
            if (formatMatch.Success)
            {
                string formatValue = formatMatch.Groups["format"].Value;
                _outputHelper.WriteLine($"Output Format: {formatValue}");
                Assert.True(_formatHeaderSource.TrySetResult(formatValue));
            }

            if (_subjectRegexMap[FormatUsed] != null)
            {
                Match subjectMatch = _subjectRegexMap[FormatUsed].Match(line);
                if (subjectMatch.Success)
                {
                    string subjectValue = subjectMatch.Groups["subject"].Value;
                    _outputHelper.WriteLine($"Subject: {subjectValue}");

                    // for Json we will parse the whole blob and set the value that way
                    if (FormatUsed != OutputFormat.Json)
                    {
                        Assert.True(_subjectSource.TrySetResult(subjectValue));
                    }
                }
            }

            if (_publicKeyRegexMap[FormatUsed] != null)
            {
                Match publicKeyMatch = _publicKeyRegexMap[FormatUsed].Match(line);
                if (publicKeyMatch.Success)
                {
                    string publicKeyValue = publicKeyMatch.Groups["publickey"].Value;
                    _outputHelper.WriteLine($"Public Key: {publicKeyValue}");

                    // for Json we will parse the whole blob and set the value that way
                    if (FormatUsed != OutputFormat.Json)
                    {
                        Assert.True(_publicKeySource.TrySetResult(publicKeyValue));
                    }
                }
            }
        }

        public Task<string> GetBearerToken(CancellationToken token)
        {
            return _bearerTokenTaskSource.Task.WithCancellation(token);
        }

        public Task<string> GetFormat(CancellationToken token)
        {
            return _formatHeaderSource.Task.WithCancellation(token);
        }

        public Task<string> GetOutput(CancellationToken token)
        {
            return _outputSource.Task.WithCancellation(token);
        }

        public Task<string> GetSubject(CancellationToken token)
        {
            return _subjectSource.Task.WithCancellation(token);
        }

        public Task<string> GetPublicKey(CancellationToken token)
        {
            return _publicKeySource.Task.WithCancellation(token);
        }

        /// <summary>
        /// Expected output format for <see cref="OutputFormat.MachineJson" />.
        /// </summary>
        /// <remarks>
        /// This is intentionally a second copy of the class used to generate the output, 
        /// Microsoft.Diagnostics.Tools.Monitor.Commands.GenerateApiKeyCommandHandler.MachineOutputForma.
        /// This is separate so that any breaking changes to the first copy will cause a test failure.
        /// We shouldn't break this format; if you find yourself here editing this, 
        /// be careful of any downstream dependencies that are depending on this remaining stable.
        /// </remarks>
        internal class ExpectedMachineOutputFormat
        {
            public AuthenticationOptions Authentication { get; set; }
            public string AuthorizationHeader { get; set; }
        }
    }
}

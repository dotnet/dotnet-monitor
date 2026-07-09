// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class OpenApiGeneratorTests
    {
        private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(30);

        private const string OpenApiBaselineName = "openapi.json";
        private const string OpenApiGenName = "Microsoft.Diagnostics.Monitoring.OpenApiGen";

        private static readonly string CurrentExecutingAssemblyPath =
            Assembly.GetExecutingAssembly().Location;

        private static readonly string OpenApiBaselinePath =
            Path.Combine(Path.GetDirectoryName(CurrentExecutingAssemblyPath), OpenApiBaselineName);

        private static readonly string OpenApiGenPath =
            AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), OpenApiGenName, TargetFrameworkMoniker.Net100);

        private readonly ITestOutputHelper _outputHelper;

        public OpenApiGeneratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test that the committed OpenAPI document for dotnet-monitor
        /// is the same that is generated from the dotnet-monitor binaries.
        /// </summary>
        [Fact]
        public async Task BaselineDifferenceTestAsync()
        {
            using FileStream stream = await GenerateDocumentAsync();
            using StreamReader reader = new(stream);

            // Renormalize line endings due to git checkout normalizing to the operating system preference.
            string baselineContent = File.ReadAllText(OpenApiBaselinePath).Replace("\r\n", "\n");
            string generatedContent = await reader.ReadToEndAsync();

            bool equal = string.Equals(baselineContent, generatedContent, StringComparison.Ordinal);
            if (!equal)
            {
                var baseline = baselineContent.Split('\n');
                var generated = generatedContent.Split('\n');
                //Help with some of the diffs, especially since VS autoformats json files
                for (int i = 0; i < Math.Min(baseline.Length, generated.Length); i++)
                {
                    if (!string.Equals(baseline[i], generated[i], StringComparison.Ordinal))
                    {
                        _outputHelper.WriteLine(@$"Mismatch content on line {i + 1}:{Environment.NewLine}{baseline[i]}{Environment.NewLine}{generated[i]}");
                        break;
                    }
                }

                _outputHelper.WriteLine(generatedContent);
            }
            Assert.True(equal, "The generated OpenAPI description is different than the documented baseline.");
        }

        /// <summary>
        /// Test that the committed OpenAPI document is valid.
        /// </summary>
        [Fact]
        public async Task BaselineIsValidTestAsync()
        {
            using FileStream stream = new(OpenApiBaselinePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            await ValidateDocumentAsync(stream);
        }

        /// <summary>
        /// Test that the generated OpenAPI document is valid.
        /// </summary>
        [Fact]
        public async Task GeneratedIsValidTestAsync()
        {
            using FileStream stream = await GenerateDocumentAsync();

            await ValidateDocumentAsync(stream);
        }

        private async Task<FileStream> GenerateDocumentAsync()
        {
            string path = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".json");

            DotNetRunner runner = new();
            runner.EntrypointAssemblyPath = OpenApiGenPath;
            runner.Arguments = path;

            await using LoggingRunnerAdapter adapter = new(_outputHelper, runner);

            using CancellationTokenSource cancellation = new(GenerationTimeout);

            await adapter.StartAsync(cancellation.Token);

            int exitCode = await adapter.WaitForExitAsync(cancellation.Token);

            Assert.Equal(0, exitCode);

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
        }

        private static async Task ValidateDocumentAsync(Stream stream)
        {
            ReadResult result = await OpenApiDocument.LoadAsync(stream, OpenApiConstants.Json);
            Assert.Empty(result.Diagnostic.Errors);

            IEnumerable<OpenApiError> errors = result.Document.Validate(ValidationRuleSet.GetDefaultRuleSet());
            Assert.Empty(errors);
        }
    }
}

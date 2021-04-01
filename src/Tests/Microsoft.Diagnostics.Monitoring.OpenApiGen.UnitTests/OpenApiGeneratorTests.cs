// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
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
            CurrentExecutingAssemblyPath.Replace(Assembly.GetExecutingAssembly().GetName().Name, OpenApiGenName);

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
        public async Task BaselineDifferenceTest()
        {
            using FileStream stream = await GenerateDocumentAsync();
            using StreamReader reader = new(stream);

            // Renormalize line endings due to git checkout normalizing to the operating system preference.
            string baselineContent = File.ReadAllText(OpenApiBaselinePath).Replace("\r\n", "\n");
            string generatedContent = reader.ReadToEnd();

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
            }
            Assert.True(equal, "The generated OpenAPI description is different than the documented baseline.");
        }

        /// <summary>
        /// Test that the committed OpenAPI document is valid.
        /// </summary>
        [Fact]
        public void BaselineIsValidTest()
        {
            using FileStream stream = new(OpenApiBaselinePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            ValidateDocument(stream);
        }

        /// <summary>
        /// Test that the generated OpenAPI document is valid.
        /// </summary>
        [Fact]
        public async Task GeneratedIsValidTest()
        {
            using FileStream stream = await GenerateDocumentAsync();

            ValidateDocument(stream);
        }

        private async Task<FileStream> GenerateDocumentAsync()
        {
            string path = Path.GetTempFileName();

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

        private static void ValidateDocument(FileStream stream)
        {
            OpenApiStreamReader reader = new();
            OpenApiDocument document = reader.Read(stream, out OpenApiDiagnostic diagnostic);
            Assert.Empty(diagnostic.Errors);

            IEnumerable<OpenApiError> errors = document.Validate(ValidationRuleSet.GetDefaultRuleSet());
            Assert.Empty(errors);
        }
    }
}

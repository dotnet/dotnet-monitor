// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Reflection;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests
{
    public class SchemaGenerationTests
    {
        private readonly ITestOutputHelper _outputHelper;

        private const string SchemaBaseline = "schema.json";
        private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(30);

        private static readonly string CurrentExecutingAssemblyPath =
            Assembly.GetExecutingAssembly().Location;

        private static readonly string SchemaBaselinePath =
            Path.Combine(Path.GetDirectoryName(CurrentExecutingAssemblyPath), SchemaBaseline);

        private const string SchemaGeneratorName = "Microsoft.Diagnostics.Monitoring.ConfigurationSchema";

        private static readonly string SchemaGeneratorPath =
            CurrentExecutingAssemblyPath.Replace(Assembly.GetExecutingAssembly().GetName().Name, SchemaGeneratorName);

        public SchemaGenerationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestGenerateSchema()
        {
            using Stream schema = await GenerateSchema();
            using var reader = new StreamReader(schema);
            string completeSchema = await reader.ReadToEndAsync();
            _outputHelper.WriteLine(completeSchema);
        }

        [Fact]
        public async Task TestSchemaBaseline()
        {
            Stream generatedSchema = await GenerateSchema();
            using StreamReader baseline = new StreamReader(SchemaBaselinePath);
            using StreamReader generated = new StreamReader(generatedSchema);
            bool equal = await CompareLines(baseline, generated);
            Assert.True(equal, "Generated schema differs from baseline.");
        }

        private async Task<Stream> GenerateSchema()
        {
            string tempSchema = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".json");

            DotNetRunner runner = new();
            runner.EntrypointAssemblyPath = SchemaGeneratorPath;
            runner.Arguments = tempSchema;

            await using LoggingRunnerAdapter adapter = new(_outputHelper, runner);

            using CancellationTokenSource cancellation = new(GenerationTimeout);

            await adapter.StartAsync(cancellation.Token);

            int exitCode = await adapter.WaitForExitAsync(cancellation.Token);

            Assert.Equal(0, exitCode);

            return new FileStream(tempSchema, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
        }

        private async Task<bool> CompareLines(TextReader first, TextReader second)
        {
            IList<string> firstLines = await ReadAllLines(first);
            IList<string> secondLines = await ReadAllLines(second);

            for (int i = 0; i < Math.Min(firstLines.Count, secondLines.Count); i++)
            {
                if (!string.Equals(firstLines[i], secondLines[i], StringComparison.Ordinal))
                {
                    _outputHelper.WriteLine($"Differs from baseline on line {i + 1}:");
                    _outputHelper.WriteLine(firstLines[i]);
                    _outputHelper.WriteLine(secondLines[i]);
                    return false;
                }
            }

            if (firstLines.Count != secondLines.Count)
            {
                _outputHelper.WriteLine($"Count differs from baseline: {firstLines.Count} {secondLines.Count}");
                return false;
            }

            return true;
        }

        private async Task<IList<string>> ReadAllLines(TextReader reader)
        {
            var lines = new List<string>();
            string line = null;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}

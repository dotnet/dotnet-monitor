// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
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
            AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                SchemaGeneratorName,
                TargetFrameworkMoniker.Net80);

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

        private async Task<bool> CompareLines(TextReader baseline, TextReader generated)
        {
            IList<string> baselineLines = await ReadAllLines(baseline);
            IList<string> generatedLines = await ReadAllLines(generated);

            for (int i = 0; i < Math.Min(baselineLines.Count, generatedLines.Count); i++)
            {
                if (!string.Equals(baselineLines[i], generatedLines[i], StringComparison.Ordinal))
                {
                    _outputHelper.WriteLine($"Differs from baseline on line {i + 1}:");
                    PrintSection(_outputHelper, nameof(baseline), baselineLines, i);
                    PrintSection(_outputHelper, nameof(generated), generatedLines, i);
                    return false;
                }
            }

            if (baselineLines.Count != generatedLines.Count)
            {
                _outputHelper.WriteLine($"Count differs from baseline: {baselineLines.Count} {generatedLines.Count}");
                return false;
            }

            return true;
        }

        private static void PrintSection(ITestOutputHelper outputHelper, string header, IList<string> lines, int lineHighlighted, int contextQty = 7)
        {
            outputHelper.WriteLine($"-----{header}-----");
            int startLine = Math.Max(0, lineHighlighted - contextQty);
            int endLine = Math.Min(lines.Count, lineHighlighted + contextQty);
            int formatQty = (endLine + 1).ToString("D").Length; // Get the length of the biggest number (add 1 for the 1-based index)
            for (int i = startLine; i <= endLine; i++)
            {
                outputHelper.WriteLine("{0}:{1}{2}", (i + 1).ToString("D" + formatQty.ToString(CultureInfo.InvariantCulture)), (i == lineHighlighted) ? " >" : "  ", lines[i]);
            }
        }

        private static async Task<IList<string>> ReadAllLines(TextReader reader)
        {
            var lines = new List<string>();
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using NJsonSchema;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Reflection;
using NJsonSchema.Generation;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests
{
    public class SchemaGenerationTests
    {
        private readonly ITestOutputHelper _outputHelper;

        private const string SchemaBaseline = "schema.json";

        private static readonly string CurrentExecutingAssemblyPath =
            Assembly.GetExecutingAssembly().Location;

        private static readonly string SchemaBaselinePath =
            Path.Combine(Path.GetDirectoryName(CurrentExecutingAssemblyPath), SchemaBaseline);

        public SchemaGenerationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void TestGenerateSchema()
        {
            string completeSchema = GenerateSchema();
            _outputHelper.WriteLine(completeSchema);
        }

        [Fact]
        public async Task TestSchemaBaseline()
        {
            string generatedSchema = GenerateSchema();
            using StreamReader baseline = new StreamReader(SchemaBaselinePath);
            using StringReader generated = new StringReader(generatedSchema);
            bool equal = await CompareLines(baseline, generated);
            Assert.True(equal, "Generated schema differs from baseline.");
        }

        private string GenerateSchema()
        {
            var settings = new JsonSchemaGeneratorSettings();

            JsonSchema schema = JsonSchema.FromType<RootOptions>(settings);
            schema.Id = @"http://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            //HACK Even though Properties is defined as JsonDataExtension, it still emits a property
            //called 'Properties' into the schema, instead of just specifying additionalProperties.
            //There is likely a more elegant way to fix this.
            schema.Definitions[nameof(EgressProvider)].Properties.Remove(nameof(EgressProvider.Properties));

            string schemaPayload = schema.ToJson();
            return schemaPayload;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ConfigurationTokenParserTests
    {
        [Theory]
        [InlineData("../outside.dmp", "outside.dmp")]
        [InlineData(@"..\outside.dmp", "outside.dmp")]
        [InlineData("/tmp/outside.dmp", "outside.dmp")]
        [InlineData(@"\outside.dmp", "outside.dmp")]
        [InlineData(@"C:\tmp\outside.dmp", "outside.dmp")]
        [InlineData(@"\\server\share\outside.dmp", "outside.dmp")]
        [InlineData("C:outside.dmp", "outside.dmp")]
        [InlineData("artifact.dmp:stream", "artifact.dmp")]
        [InlineData("C:artifact.dmp:stream", "artifact.dmp")]
        [InlineData("..", "")]
        [InlineData("C:..", "")]
        [InlineData(".. ", "")]
        [InlineData("C:.. ", "")]
        public void CommandLineReferenceInArtifactNameUsesFileName(string commandLine, string expectedArtifactName)
        {
            CollectDumpOptions settings = new()
            {
                Egress = ConfigurationTokenParser.CommandLineReference,
                ArtifactName = ConfigurationTokenParser.CommandLineReference
            };
            ConfigurationTokenParser parser = new(NullLogger.Instance);

            CollectDumpOptions newSettings = Assert.IsType<CollectDumpOptions>(
                parser.SubstituteOptionValues(settings, new TokenContext { CommandLine = commandLine }));

            Assert.Equal(commandLine, newSettings.Egress);
            Assert.Equal(expectedArtifactName, newSettings.ArtifactName);
            Assert.Equal(ConfigurationTokenParser.CommandLineReference, settings.Egress);
            Assert.Equal(ConfigurationTokenParser.CommandLineReference, settings.ArtifactName);
        }
    }
}

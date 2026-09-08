// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ConfigurationTokenParserTests
    {
        [Theory]
        [InlineData("../escape", "escape")]
        [InlineData(@"..\escape", "escape")]
        [InlineData("/tmp/escape", "escape")]
        [InlineData(@"C:\temp\escape", "escape")]
        [InlineData(@"\\server\share\escape", "escape")]
        [InlineData("C:escape", "escape")]
        [InlineData("volume:escape", "volume")]
        [InlineData("artifact.dmp:stream", "artifact.dmp")]
        [InlineData("C:artifact.dmp:stream", "artifact.dmp")]
        [InlineData("..", "")]
        [InlineData("C:..", "")]
        [InlineData(".. ", "")]
        [InlineData("C:.. ", "")]
        [InlineData("...", "")]
        [InlineData("   ", "")]
        public void SubstituteOptionValues_CommandLineInArtifactName_UsesFileName(
            string commandLine,
            string expectedCommandLine)
        {
            const string ArtifactNamePrefix = "prefix-";
            const string ArtifactNameSuffix = "-suffix";
            string originalArtifactName = ArtifactNamePrefix +
                ConfigurationTokenParser.CommandLineReference +
                ArtifactNameSuffix;
            CollectDumpOptions originalSettings = new()
            {
                ArtifactName = originalArtifactName
            };
            ConfigurationTokenParser parser = new(Mock.Of<ILogger>());

            CollectDumpOptions newSettings = (CollectDumpOptions)parser.SubstituteOptionValues(
                originalSettings,
                new TokenContext { CommandLine = commandLine });

            Assert.Equal(ArtifactNamePrefix + expectedCommandLine + ArtifactNameSuffix, newSettings.ArtifactName);
            Assert.Equal(originalArtifactName, originalSettings.ArtifactName);
        }

        [Fact]
        public void SubstituteOptionValues_CommandLineInOtherSettings_PreservesValue()
        {
            const string CommandLine = @"..\path/to/application --argument";
            CollectDumpOptions originalSettings = new()
            {
                Egress = ConfigurationTokenParser.CommandLineReference
            };
            ConfigurationTokenParser parser = new(Mock.Of<ILogger>());

            CollectDumpOptions newSettings = (CollectDumpOptions)parser.SubstituteOptionValues(
                originalSettings,
                new TokenContext { CommandLine = CommandLine });

            Assert.Equal(CommandLine, newSettings.Egress);
        }
    }
}

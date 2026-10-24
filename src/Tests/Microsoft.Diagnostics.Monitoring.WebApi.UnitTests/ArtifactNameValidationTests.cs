// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests
{
    public class ArtifactNameValidationTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("dump.dmp")]
        [InlineData("myapp_prod_20261007.dmp")]
        [InlineData("core_myapp")]
        [InlineData("name with spaces.dmp")]
        [InlineData(".hidden")]
        [InlineData("a..b.dmp")]
        public void ValidateArtifactName_Valid(string artifactName)
        {
            Utilities.ValidateArtifactName(artifactName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("../outside.dmp")]
        [InlineData(@"..\..\outside.dmp")]
        [InlineData("/tmp/outside.dmp")]
        [InlineData(@"\outside.dmp")]
        [InlineData(@"C:\temp\outside.dmp")]
        [InlineData("C:outside.dmp")]
        [InlineData(@"\\server\share\outside.dmp")]
        [InlineData("sub/dir.dmp")]
        [InlineData("artifact.dmp:stream")]
        [InlineData("trailingdot.")]
        [InlineData("trailingspace ")]
        [InlineData("wild*card.dmp")]
        [InlineData("quest?ion.dmp")]
        [InlineData("pi|pe.dmp")]
        [InlineData("<angle>.dmp")]
        [InlineData("quo\"te.dmp")]
        [InlineData("null\0char.dmp")]
        [InlineData("new\nline.dmp")]
        public void ValidateArtifactName_Invalid(string artifactName)
        {
            Assert.Throws<ValidationException>(() => Utilities.ValidateArtifactName(artifactName));
        }

        [Fact]
        public void ValidateArtifactName_TooLong()
        {
            Utilities.ValidateArtifactName(new string('a', 255));
            Assert.Throws<ValidationException>(() => Utilities.ValidateArtifactName(new string('a', 256)));
        }
    }
}

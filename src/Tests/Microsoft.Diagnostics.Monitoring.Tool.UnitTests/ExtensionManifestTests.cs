// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Testing;
using System.IO;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class ExtensionManifestFixture : WebApplicationFactory<Program>
    {
    }

    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExtensionManifestTests : IClassFixture<ExtensionManifestFixture>
    {
        private const string ExpectedName = "CustomEgress";
        private const string ExpectedExecutableName = "CustomExecutable";
        private const string ExpectedAssemblyName = "CustomAssembly";

        private readonly ITestOutputHelper _outputHelper;
        private readonly ExtensionManifestFixture _fixture;

        public ExtensionManifestTests(ITestOutputHelper outputHelper, ExtensionManifestFixture fixture)
        {
            _outputHelper = outputHelper;
            _fixture = fixture;
        }

        [Fact]
        public void ExtensionManifest_NullPath_ThrowOnParse()
        {
            ExtensionException ex = Assert.Throws<ExtensionException>(() => ExtensionManifest.FromPath(null));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_EmptyPath_ThrowOnParse()
        {
            ExtensionException ex = Assert.Throws<ExtensionException>(() => ExtensionManifest.FromPath(string.Empty));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_NonExistentPath_ThrowOnParse()
        {
            ExtensionException ex = Assert.Throws<ExtensionException>(() => ExtensionManifest.FromPath(Path.GetRandomFileName()));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_EmptyFile_ThrowOnParse()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            CreateManifestStream(dir, out string manifestPath).Dispose();

            ExtensionException ex = Assert.Throws<ExtensionException>(() => ExtensionManifest.FromPath(manifestPath));
            Assert.IsType<JsonException>(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_InvalidJsonFile_ThrowOnParse()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using StreamWriter writer = new(stream);
                writer.WriteLine("InvalidContent");
            }

            ExtensionException ex = Assert.Throws<ExtensionException>(() => ExtensionManifest.FromPath(manifestPath));
            Assert.IsType<JsonException>(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_EmptyObject_ThrowOnValidate()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using Utf8JsonWriter writer = new(stream);
                writer.WriteStartObject();
                writer.WriteEndObject();
                writer.Flush();
            }

            ExtensionManifest manifest = ExtensionManifest.FromPath(manifestPath);
            Assert.Null(manifest.Name);
            Assert.Null(manifest.AssemblyFileName);
            Assert.Null(manifest.ExecutableFileName);

            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            ExtensionException ex = Assert.Throws<ExtensionException>(() => manifest.Validate(_fixture.Services, validationOptions));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_NameOnly_ThrowOnValidate()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using Utf8JsonWriter writer = new(stream);
                writer.WriteStartObject();
                writer.WriteString(nameof(ExtensionManifest.Name), ExpectedName);
                writer.WriteEndObject();
                writer.Flush();
            }

            ExtensionManifest manifest = ExtensionManifest.FromPath(manifestPath);
            Assert.Equal(ExpectedName, manifest.Name);
            Assert.Null(manifest.AssemblyFileName);
            Assert.Null(manifest.ExecutableFileName);

            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            ExtensionException ex = Assert.Throws<ExtensionException>(() => manifest.Validate(_fixture.Services, validationOptions));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_ExecutableAndAssembly_ThrowOnValidate()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using Utf8JsonWriter writer = new(stream);
                writer.WriteStartObject();
                writer.WriteString(nameof(ExtensionManifest.Name), ExpectedName);
                writer.WriteString(nameof(ExtensionManifest.AssemblyFileName), ExpectedAssemblyName);
                writer.WriteString(nameof(ExtensionManifest.ExecutableFileName), ExpectedExecutableName);
                writer.WriteEndObject();
                writer.Flush();
            }

            ExtensionManifest manifest = ExtensionManifest.FromPath(manifestPath);
            Assert.Equal(ExpectedName, manifest.Name);
            Assert.Equal(ExpectedAssemblyName, manifest.AssemblyFileName);
            Assert.Equal(ExpectedExecutableName, manifest.ExecutableFileName);

            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            ExtensionException ex = Assert.Throws<ExtensionException>(() => manifest.Validate(_fixture.Services, validationOptions));
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ExtensionManifest_NameAndAssembly_Valid()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using Utf8JsonWriter writer = new(stream);
                writer.WriteStartObject();
                writer.WriteString(nameof(ExtensionManifest.Name), ExpectedName);
                writer.WriteString(nameof(ExtensionManifest.AssemblyFileName), ExpectedAssemblyName);
                writer.WriteEndObject();
                writer.Flush();
            }

            ExtensionManifest manifest = ExtensionManifest.FromPath(manifestPath);
            Assert.Equal(ExpectedName, manifest.Name);
            Assert.Equal(ExpectedAssemblyName, manifest.AssemblyFileName);
            Assert.Null(manifest.ExecutableFileName);

    
            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            manifest.Validate(_fixture.Services, validationOptions);
        }

        [Fact]
        public void ExtensionManifest_NameAndExecutable_Valid()
        {
            using TemporaryDirectory dir = new(_outputHelper);

            string manifestPath;
            using (Stream stream = CreateManifestStream(dir, out manifestPath))
            {
                using Utf8JsonWriter writer = new(stream);
                writer.WriteStartObject();
                writer.WriteString(nameof(ExtensionManifest.Name), ExpectedName);
                writer.WriteString(nameof(ExtensionManifest.ExecutableFileName), ExpectedExecutableName);
                writer.WriteEndObject();
                writer.Flush();
            }

            ExtensionManifest manifest = ExtensionManifest.FromPath(manifestPath);
            Assert.Equal(ExpectedName, manifest.Name);
            Assert.Null(manifest.AssemblyFileName);
            Assert.Equal(ExpectedExecutableName, manifest.ExecutableFileName);

            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            manifest.Validate(_fixture.Services, validationOptions);
        }

        private static Stream CreateManifestStream(TemporaryDirectory dir, out string path)
        {
            path = Path.Combine(dir.FullName, Path.GetRandomFileName());

            return new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
    }
}

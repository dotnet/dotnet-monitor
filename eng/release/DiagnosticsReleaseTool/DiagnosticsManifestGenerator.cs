using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiagnosticsReleaseTool.Util;
using Microsoft.Extensions.Logging;
using ReleaseTool.Core;

namespace DiagnosticsReleaseTool.Impl
{
    internal class DiagnosticsManifestGenerator : IManifestGenerator
    {
        private readonly ReleaseMetadata _productReleaseMetadata;
        private readonly JsonDocument _assetManifestManifestDom;
        private readonly ILogger _logger;

        public DiagnosticsManifestGenerator(ReleaseMetadata productReleaseMetadata, FileInfo toolManifest, ILogger logger)
        {
            _productReleaseMetadata = productReleaseMetadata;
            string manifestContent = File.ReadAllText(toolManifest.FullName);
            _assetManifestManifestDom = JsonDocument.Parse(manifestContent);
            _logger = logger;
        }

        public void Dispose()
        {
            _assetManifestManifestDom.Dispose();
        }

        public Stream GenerateManifest(IEnumerable<FileReleaseData> filesProcessed)
        {
            var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = true }))
            {
                writer.WriteStartObject();

                WriteMetadata(writer);

                WriteNugetShippingPackages(writer, filesProcessed);

                writer.WriteEndObject();
            }
            stream.Position = 0;
            return stream;
        }

        private void WriteNugetShippingPackages(Utf8JsonWriter writer, IEnumerable<FileReleaseData> filesProcessed)
        {
            writer.WritePropertyName(FileMetadata.GetDefaultCatgoryForClass(FileClass.Nuget));
            writer.WriteStartArray();

            IEnumerable<FileReleaseData> nugetFiles = filesProcessed.Where(file => file.FileMetadata.Class == FileClass.Nuget);

            foreach (FileReleaseData fileToRelease in nugetFiles)
            {
                writer.WriteStartObject();
                writer.WriteString("PublishRelativePath", fileToRelease.FileMap.RelativeOutputPath);
                writer.WriteString("PublishedPath", fileToRelease.PublishUri);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private void WriteMetadata(Utf8JsonWriter writer)
        {
            // There's no way to obtain the json DOM for an object...
            byte[] metadataJsonObj = JsonSerializer.SerializeToUtf8Bytes<ReleaseMetadata>(_productReleaseMetadata);
            JsonDocument metadataDoc = JsonDocument.Parse(metadataJsonObj);
            foreach(var element in metadataDoc.RootElement.EnumerateObject())
            {
                element.WriteTo(writer);
            }
        }
    }
}
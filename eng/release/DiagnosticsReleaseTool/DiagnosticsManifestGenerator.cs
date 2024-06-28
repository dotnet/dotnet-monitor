// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using ReleaseTool.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

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

            var jro = new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using (var writer = new Utf8JsonWriter(stream, jro))
            {
                writer.WriteStartObject();

                WriteMetadata(writer);

                foreach (FileClass fc in Enum.GetValues<FileClass>())
                {
                    WriteFiles(writer, filesProcessed, fc);
                }

                writer.WriteEndObject();
            }
            stream.Position = 0;
            return stream;
        }

        private void WriteFiles(Utf8JsonWriter writer, IEnumerable<FileReleaseData> filesProcessed, FileClass fileClass)
        {
            writer.WritePropertyName(FileMetadata.GetDefaultCategoryForClass(fileClass));
            writer.WriteStartArray();

            IEnumerable<FileReleaseData> nugetFiles = filesProcessed.Where(file => file.FileMetadata.Class == fileClass);

            foreach (FileReleaseData fileToRelease in nugetFiles)
            {
                writer.WriteStartObject();
                writer.WriteString("PublishRelativePath", fileToRelease.FileMap.RelativeOutputPath);
                writer.WriteString("Sha512", fileToRelease.FileMetadata.Sha512);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }


        private void WriteMetadata(Utf8JsonWriter writer)
        {
            // There's no way to obtain the json DOM for an object...
            byte[] metadataJsonObj = JsonSerializer.SerializeToUtf8Bytes<ReleaseMetadata>(_productReleaseMetadata);
            JsonDocument metadataDoc = JsonDocument.Parse(metadataJsonObj);
            foreach (var element in metadataDoc.RootElement.EnumerateObject())
            {
                element.WriteTo(writer);
            }
        }
    }
}

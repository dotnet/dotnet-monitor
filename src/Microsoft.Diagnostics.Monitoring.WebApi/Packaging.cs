// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// WARNING! This is a temporary Zip based implementation for diagsession files, but likely not the way
    /// these will be created in the long term.
    /// </summary>
    internal static class Packaging
    {
        private static readonly XNamespace Ns = "urn:diagnosticshub-package-metadata-2-1";

        public static async Task<string> CreateDiagSession(string dumpFilePath, string dac, string dbi, CancellationToken token)
        {
            string packagePath = Path.ChangeExtension(dumpFilePath, ".diagsession");

            //Note it is critical to have Create/Write here. A Read/Write mode will cause
            //the entire dump to be placed into MemoryStream
            using Package package = Package.Open(packagePath, FileMode.Create, FileAccess.Write);

            Guid tool = new Guid("013914d7-d262-4da2-8fa4-ba8b4e328a1e");

            var root = new XElement(Ns + "Package",
                new XElement(Ns + "Tools",
                    new XElement(Ns + "Tool", new XAttribute("Id", FormattableString.Invariant($"{tool:b}")),
                        new XElement(Ns + "AcquireLink", new XAttribute("Type", "none")))));

            var content = new XElement(Ns + "Content");
            root.Add(content);

            XElement dumpResource = await CreateResource(package, dumpFilePath, token);
            content.Add(dumpResource);
            if (dac != null)
            {
                XElement dacResource = await CreateResource(package, dac, token);
                content.Add(dacResource);
            }
            if (dbi != null)
            {
                XElement dbiResource = await CreateResource(package, dbi, token);
                content.Add(dbiResource);
            }

            //After all the content nodes are added, add the metadata.xml file
            Uri metadataUri = PackUriHelper.CreatePartUri(new Uri("/metadata.xml", UriKind.Relative));
            PackagePart metadataPart = package.CreatePart(metadataUri, System.Net.Mime.MediaTypeNames.Text.Xml);
            using (var streamWriter = new StreamWriter(metadataPart.GetStream(), Encoding.UTF8))
            {
                await streamWriter.WriteAsync(root.ToString());
            }

            return packagePath;
        }

        private static async Task<XElement> CreateResource(Package package, string path, CancellationToken token)
        {
            Guid id = Guid.NewGuid();
            string name = Path.GetFileName(path);

            string prefix = FormattableString.Invariant($"{id:d}");
            long timeAdded = DateTime.UtcNow.ToFileTimeUtc();
            string type = @"DiagnosticsHub.Resource.File";

            var resource = new XElement(Ns + "Resource",
                new XAttribute("CompressionOption", "none"),
                new XAttribute("Id", FormattableString.Invariant($"{id:b}")),
                new XAttribute("IsDirectoryOnDisk", "false"),
                new XAttribute("Name", name),
                new XAttribute("ResourcePackageUriPrefix", prefix),
                new XAttribute("TimeAddedUTC", timeAdded),
                new XAttribute("Type", type));

            Uri uri = PackUriHelper.CreatePartUri(new Uri($"/{id:d}/{name}", UriKind.Relative));
            PackagePart part = package.CreatePart(uri, ContentTypes.ApplicationOctetStream, CompressionOption.NotCompressed);
            using FileStream fileStream = File.OpenRead(path);
            using Stream partStream = part.GetStream(FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(partStream, bufferSize: 0x10000, token);

            return resource;
        }
    }
}

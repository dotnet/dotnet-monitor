// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Writes out configuration as JSON.
    /// </summary>
    internal sealed class ConfigurationJsonWriter : IDisposable
    {
        private readonly Utf8JsonWriter _writer;
        public ConfigurationJsonWriter(Stream outputStream)
        {
            JsonWriterOptions options = new() { Indented = true };
            _writer = new Utf8JsonWriter(outputStream, options);
        }

        public void Write(IConfiguration configuration, bool full)
        {
            //Note that we avoid IConfigurationRoot.GetDebugView because it shows everything
            //CONSIDER Should we show this in json, since it cannot represent complete configuration?
            //CONSIDER Should we convert number based names to arrays?
            //CONSIDER There's probably room for making this code more generalized, to separate the output from the traversal
            //but it seemed like over-engineering.

            _writer.WriteStartObject();

            ProcessChildSection(configuration, WebHostDefaults.ServerUrlsKey);
            IConfigurationSection kestrel = ProcessChildSection(configuration, "Kestrel", includeChildSections: false);
            if (kestrel != null)
            {
                _writer.WriteStartObject();
                IConfigurationSection certificates = ProcessChildSection(kestrel, "Certificates", includeChildSections: false);
                if (certificates != null)
                {
                    _writer.WriteStartObject();
                    IConfigurationSection defaultCert = ProcessChildSection(certificates, "Default", includeChildSections: false);
                    if (defaultCert != null)
                    {
                        _writer.WriteStartObject();
                        ProcessChildSection(defaultCert, "Path", includeChildSections: false);
                        ProcessChildSection(defaultCert, "Password", includeChildSections: false, redact: !full);
                        _writer.WriteEndObject();
                    }
                    _writer.WriteEndObject();
                }
                _writer.WriteEndObject();
            }

            //No sensitive information
            ProcessChildSection(configuration, ConfigurationKeys.CorsConfiguration, includeChildSections: true);
            ProcessChildSection(configuration, ConfigurationKeys.DiagnosticPort, includeChildSections: true);
            ProcessChildSection(configuration, ConfigurationKeys.Metrics, includeChildSections: true);
            ProcessChildSection(configuration, ConfigurationKeys.Storage, includeChildSections: true);
            ProcessChildSection(configuration, ConfigurationKeys.DefaultProcess, includeChildSections: true);
            ProcessChildSection(configuration, ConfigurationKeys.Logging, includeChildSections: true);

            if (full)
            {
                ProcessChildSection(configuration, ConfigurationKeys.ApiAuthentication, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.Egress, includeChildSections: true);
            }
            else
            {
                //Do not emit ApiKeyHash
                IConfigurationSection authSection = ProcessChildSection(configuration, ConfigurationKeys.ApiAuthentication, includeChildSections: false);
                if (authSection != null)
                {
                    _writer.WriteStartObject();
                    ProcessChildSection(authSection, nameof(ApiAuthenticationOptions.ApiKeyHash), includeChildSections: false, redact: true);
                    ProcessChildSection(authSection, nameof(ApiAuthenticationOptions.ApiKeyHashType), includeChildSections: false, redact: false);
                    _writer.WriteEndObject();
                }

                IConfigurationSection egress = ProcessChildSection(configuration, ConfigurationKeys.Egress, includeChildSections: false);
                if (egress != null)
                {
                    _writer.WriteStartObject();
                    ProcessEgressSection(egress);
                    _writer.WriteEndObject();
                }
            }
            _writer.WriteEndObject();
        }

        private void ProcessEgressSection(IConfiguration egress)
        {
            IList<string> processedSectionPaths = new List<string>();

            // Redact all the properties since they could include secrets such as storage keys
            IConfigurationSection propertiesSection = ProcessChildSection(egress, nameof(EgressOptions.Properties), includeChildSections: true, redact: true);
            if (null != propertiesSection)
            {
                processedSectionPaths.Add(propertiesSection.Path);
            }

            IConfigurationSection azureBlobProviderSection = ProcessChildSection(egress, nameof(EgressOptions.AzureBlobStorage), includeChildSections: false);
            if (azureBlobProviderSection != null)
            {
                processedSectionPaths.Add(azureBlobProviderSection.Path);

                _writer.WriteStartObject();
                foreach (IConfigurationSection optionsSection in azureBlobProviderSection.GetChildren())
                {
                    _writer.WritePropertyName(optionsSection.Key);
                    _writer.WriteStartObject();
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountUri), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.BlobPrefix), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.ContainerName), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.CopyBufferSize), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature), includeChildSections: false, redact: true);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKey), includeChildSections: false, redact: true);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignatureName), includeChildSections: false, redact: false);
                    ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKeyName), includeChildSections: false, redact: false);
                    _writer.WriteEndObject();
                }
                _writer.WriteEndObject();
            }

            IConfigurationSection fileSystemProviderSection = ProcessChildSection(egress, nameof(EgressOptions.FileSystem), includeChildSections: false);
            if (fileSystemProviderSection != null)
            {
                processedSectionPaths.Add(fileSystemProviderSection.Path);

                _writer.WriteStartObject();
                foreach (IConfigurationSection optionsSection in fileSystemProviderSection.GetChildren())
                {
                    _writer.WritePropertyName(optionsSection.Key);
                    _writer.WriteStartObject();
                    ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.DirectoryPath), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath), includeChildSections: false);
                    ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.CopyBufferSize), includeChildSections: false);
                    _writer.WriteEndObject();
                }
                _writer.WriteEndObject();
            }

            //Emit other egress entries, with redaction
            foreach (IConfigurationSection childSection in egress.GetChildren())
            {
                if (!processedSectionPaths.Contains(childSection.Path))
                {
                    ProcessChildSection(egress, childSection.Key, includeChildSections: true, redact: true);
                }
            }
        }

        private IConfigurationSection ProcessChildSection(IConfiguration parentSection, string key, bool includeChildSections = true, bool redact = false)
        {
            IConfigurationSection section = parentSection.GetSection(key);
            if (!section.Exists())
            {
                _writer.WritePropertyName(key);
                _writer.WriteStringValue(Strings.Placeholder_NotPresent);
                return null;
            }

            ProcessSection(section, includeChildSections, redact);

            return section;
        }

        private void ProcessSection(IConfigurationSection section, bool includeChildSections = true, bool redact = false)
        {
            _writer.WritePropertyName(section.Key);

            IEnumerable<IConfigurationSection> children = section.GetChildren();

            //If we do not traverse the child sections, the caller is responsible for creating the value
            if (includeChildSections && children.Any())
            {
                _writer.WriteStartObject();
                foreach (IConfigurationSection child in children)
                {
                    ProcessSection(child, includeChildSections, redact);
                }
                _writer.WriteEndObject();
            }
            else
            {
                if (!children.Any())
                {
                    if (redact)
                    {
                        _writer.WriteStringValue(Strings.Placeholder_Redacted);
                    }
                    else
                    {
                        _writer.WriteStringValue(section.Value);
                    }
                }
            }
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
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

        public void Write(IConfiguration configuration, bool full, bool skipNotPresent)
        {
            //Note that we avoid IConfigurationRoot.GetDebugView because it shows everything
            //CONSIDER Should we show this in json, since it cannot represent complete configuration?
            //CONSIDER Should we convert number based names to arrays?
            //CONSIDER There's probably room for making this code more generalized, to separate the output from the traversal
            //but it seemed like over-engineering.

            using (new JsonObjectContext(_writer))
            {
                ProcessChildSection(configuration, WebHostDefaults.ServerUrlsKey, skipNotPresent);
                IConfigurationSection kestrel = ProcessChildSection(configuration, "Kestrel", skipNotPresent, includeChildSections: false);
                if (kestrel != null)
                {
                    using (new JsonObjectContext(_writer))
                    {
                        IConfigurationSection certificates = ProcessChildSection(kestrel, "Certificates", skipNotPresent, includeChildSections: false);
                        if (certificates != null)
                        {
                            using (new JsonObjectContext(_writer))
                            {
                                IConfigurationSection defaultCert = ProcessChildSection(certificates, "Default", skipNotPresent, includeChildSections: false);
                                if (defaultCert != null)
                                {
                                    using (new JsonObjectContext(_writer))
                                    {
                                        ProcessChildSection(defaultCert, "Path", skipNotPresent, includeChildSections: false);
                                        ProcessChildSection(defaultCert, "Password", skipNotPresent, includeChildSections: false, redact: !full);
                                    }
                                }
                            }
                        }
                    }
                }

                //No sensitive information
                ProcessChildSection(configuration, ConfigurationKeys.GlobalCounter, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.CollectionRules, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.CorsConfiguration, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.DiagnosticPort, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.Metrics, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.Storage, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.DefaultProcess, skipNotPresent, includeChildSections: true);
                ProcessChildSection(configuration, ConfigurationKeys.Logging, skipNotPresent, includeChildSections: true);

                if (full)
                {
                    ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: true);
                    ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: true);
                }
                else
                {
                    IConfigurationSection auth = ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: false);
                    if (null != auth)
                    {
                        using (new JsonObjectContext(_writer))
                        {
                            IConfigurationSection monitorApiKey = ProcessChildSection(auth, ConfigurationKeys.MonitorApiKey, skipNotPresent, includeChildSections: false);
                            if (null != monitorApiKey)
                            {
                                using (new JsonObjectContext(_writer))
                                {
                                    ProcessChildSection(monitorApiKey, nameof(MonitorApiKeyOptions.Subject), skipNotPresent, includeChildSections: false, redact: false);
                                    // The PublicKey should only ever contain the public key, however we expect that accidents may occur and we should
                                    // redact this field in the event the JWK contains the private key information.
                                    ProcessChildSection(monitorApiKey, nameof(MonitorApiKeyOptions.PublicKey), skipNotPresent, includeChildSections: false, redact: true);
                                }
                            }
                        }
                    }

                    IConfigurationSection egress = ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: false);
                    if (egress != null)
                    {
                        using (new JsonObjectContext(_writer))
                        {
                            ProcessEgressSection(egress, skipNotPresent);
                        }
                    }
                }
            }
        }

        private void ProcessEgressSection(IConfiguration egress, bool skipNotPresent)
        {
            IList<string> processedSectionPaths = new List<string>();

            // Redact all the properties since they could include secrets such as storage keys
            IConfigurationSection propertiesSection = ProcessChildSection(egress, nameof(EgressOptions.Properties), skipNotPresent, includeChildSections: true, redact: true);
            if (null != propertiesSection)
            {
                processedSectionPaths.Add(propertiesSection.Path);
            }

            IConfigurationSection azureBlobProviderSection = ProcessChildSection(egress, nameof(EgressOptions.AzureBlobStorage), skipNotPresent, includeChildSections: false);
            if (azureBlobProviderSection != null)
            {
                processedSectionPaths.Add(azureBlobProviderSection.Path);

                using (new JsonObjectContext(_writer))
                {
                    foreach (IConfigurationSection optionsSection in azureBlobProviderSection.GetChildren())
                    {
                        _writer.WritePropertyName(optionsSection.Key);
                        using (new JsonObjectContext(_writer))
                        {
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountUri), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.BlobPrefix), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.ContainerName), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.CopyBufferSize), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature), skipNotPresent, includeChildSections: false, redact: true);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKey), skipNotPresent, includeChildSections: false, redact: true);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignatureName), skipNotPresent, includeChildSections: false, redact: false);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKeyName), skipNotPresent, includeChildSections: false, redact: false);
                        }
                    }
                }
            }

            IConfigurationSection fileSystemProviderSection = ProcessChildSection(egress, nameof(EgressOptions.FileSystem), skipNotPresent, includeChildSections: false);
            if (fileSystemProviderSection != null)
            {
                processedSectionPaths.Add(fileSystemProviderSection.Path);

                using (new JsonObjectContext(_writer))
                {
                    foreach (IConfigurationSection optionsSection in fileSystemProviderSection.GetChildren())
                    {
                        _writer.WritePropertyName(optionsSection.Key);
                        using (new JsonObjectContext(_writer))
                        {
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.DirectoryPath), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath), skipNotPresent, includeChildSections: false);
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.CopyBufferSize), skipNotPresent, includeChildSections: false);
                        }
                    }
                }
            }

            //Emit other egress entries, with redaction
            foreach (IConfigurationSection childSection in egress.GetChildren())
            {
                if (!processedSectionPaths.Contains(childSection.Path))
                {
                    ProcessChildSection(egress, childSection.Key, skipNotPresent, includeChildSections: true, redact: true);
                }
            }
        }

        private IConfigurationSection ProcessChildSection(IConfiguration parentSection, string key, bool skipNotPresent, bool includeChildSections = true, bool redact = false)
        {
            IConfigurationSection section = parentSection.GetSection(key);
            if (!section.Exists())
            {
                if (!skipNotPresent)
                {
                    _writer.WritePropertyName(key);
                    _writer.WriteStringValue(Strings.Placeholder_NotPresent);
                }
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
                using (new JsonObjectContext(_writer))
                {
                    foreach (IConfigurationSection child in children)
                    {
                        ProcessSection(child, includeChildSections, redact);
                    }
                }
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

        private class JsonObjectContext : IDisposable
        {
            private readonly Utf8JsonWriter Writer;

            public JsonObjectContext(Utf8JsonWriter writer)
            {
                Writer = writer;
                Writer.WriteStartObject();
            }

            public void Dispose()
            {
                Writer.WriteEndObject();
            }
        }
    }

}
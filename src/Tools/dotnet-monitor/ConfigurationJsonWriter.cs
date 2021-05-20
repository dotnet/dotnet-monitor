﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureStorage;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                    //Redact all the properties since they could include secrets such as storage keys
                    ProcessChildSection(egress, nameof(EgressOptions.Properties), includeChildSections: true, redact: true);
                    WriteProviders(egress);
                    _writer.WriteEndObject();
                }
            }
            _writer.WriteEndObject();
        }

        private void WriteProviders(IConfiguration egress)
        {
            IConfigurationSection providers = ProcessChildSection(egress, nameof(EgressOptions.Providers), includeChildSections: false);
            if (providers != null)
            {
                Func<IConfigurationSection, string, bool> matchesProvider = (configSection, providerName) =>
                                    configSection.Exists() &&
                                    string.Equals(configSection.Key, nameof(EgressConfigureOptions.CommonEgressProviderOptions.Type), StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(configSection.Value, providerName, StringComparison.OrdinalIgnoreCase);

                _writer.WriteStartObject();
                foreach (IConfigurationSection provider in providers.GetChildren())
                {
                    string egressName = provider.Key;
                    IEnumerable<IConfigurationSection> egressProperties = provider.GetChildren();

                    _writer.WritePropertyName(egressName);
                    _writer.WriteStartObject();
                    //matches azure blob storage provider type.
                    //Emit well known properties.
                    if (egressProperties.Any(child => matchesProvider(child, EgressProviderTypes.AzureBlobStorage)))
                    {
                        ProcessChildSection(provider, nameof(EgressConfigureOptions.CommonEgressProviderOptions.Type), includeChildSections: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.AccountUri), includeChildSections: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.BlobPrefix), includeChildSections: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.ContainerName), includeChildSections: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.CopyBufferSize), includeChildSections: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature), includeChildSections: false, redact: true);
                        ProcessChildSection(provider, nameof(AzureBlobEgressProviderOptions.AccountKey), includeChildSections: false, redact: true);
                        ProcessChildSection(provider, nameof(AzureBlobEgressFactory.ConfigurationOptions.SharedAccessSignatureName), includeChildSections: false, redact: false);
                        ProcessChildSection(provider, nameof(AzureBlobEgressFactory.ConfigurationOptions.AccountKeyName), includeChildSections: false, redact: false);
                    }
                    //Matches filesystem provider type.
                    else if (egressProperties.Any(child => matchesProvider(child, EgressProviderTypes.FileSystem)))
                    {
                        ProcessChildSection(provider, nameof(EgressConfigureOptions.CommonEgressProviderOptions.Type), includeChildSections: false);
                        ProcessChildSection(provider, nameof(FileSystemEgressProviderOptions.DirectoryPath), includeChildSections: false);
                        ProcessChildSection(provider, nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath), includeChildSections: false);
                        ProcessChildSection(provider, nameof(FileSystemEgressProviderOptions.CopyBufferSize), includeChildSections: false);
                    }
                    else
                    {
                        //Emit other egress entries, with redaction
                        foreach (IConfigurationSection providerProperty in egressProperties)
                        {
                            if (string.Equals(providerProperty.Key, nameof(EgressConfigureOptions.CommonEgressProviderOptions.Type), StringComparison.OrdinalIgnoreCase))
                            {
                                ProcessSection(providerProperty, includeChildSections: false, redact: false);
                            }
                            else
                            {
                                ProcessSection(providerProperty, includeChildSections: true, redact: true);
                            }
                        }
                    }
                    _writer.WriteEndObject();
                }
                _writer.WriteEndObject();
            }
        }

        private IConfigurationSection ProcessChildSection(IConfiguration parentSection, string key, bool includeChildSections = true, bool redact = false)
        {
            IConfigurationSection section = parentSection.GetSection(key);
            if (!section.Exists())
            {
                _writer.WritePropertyName(key);
                _writer.WriteStringValue(":NOT PRESENT:");
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
                        _writer.WriteStringValue(":REDACTED:");
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
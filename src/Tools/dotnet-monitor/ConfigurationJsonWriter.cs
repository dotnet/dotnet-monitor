// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private IConfiguration? _configuration;
        public ConfigurationJsonWriter(Stream outputStream)
        {
            JsonWriterOptions options = new() { Indented = true };
            _writer = new Utf8JsonWriter(outputStream, options);
        }

        public void Write(IConfiguration configuration, bool full, bool skipNotPresent, bool showSources = false)
        {
            _configuration = configuration;
            //Note that we avoid IConfigurationRoot.GetDebugView because it shows everything
            //CONSIDER Should we show this in json, since it cannot represent complete configuration?
            //CONSIDER Should we convert number based names to arrays?
            //CONSIDER There's probably room for making this code more generalized, to separate the output from the traversal
            //but it seemed like over-engineering.

            using (new JsonObjectContext(_writer))
            {
                ProcessChildSection(configuration, WebHostDefaults.ServerUrlsKey, skipNotPresent, showSources: showSources);
                IConfigurationSection? kestrel = ProcessChildSection(configuration, "Kestrel", skipNotPresent, includeChildSections: false, showSources: showSources);
                if (kestrel != null)
                {
                    using (new JsonObjectContext(_writer))
                    {
                        IConfigurationSection? certificates = ProcessChildSection(kestrel, "Certificates", skipNotPresent, includeChildSections: false, showSources: showSources);
                        if (certificates != null)
                        {
                            using (new JsonObjectContext(_writer))
                            {
                                IConfigurationSection? defaultCert = ProcessChildSection(certificates, "Default", skipNotPresent, includeChildSections: false, showSources: showSources);
                                if (defaultCert != null)
                                {
                                    using (new JsonObjectContext(_writer))
                                    {
                                        ProcessChildSection(defaultCert, "Path", skipNotPresent, includeChildSections: false, showSources: showSources);
                                        ProcessChildSection(defaultCert, "Password", skipNotPresent, includeChildSections: false, redact: !full, showSources: showSources);
                                    }
                                }
                            }
                        }
                    }
                }

                //No sensitive information
                ProcessChildSection(configuration, ConfigurationKeys.Templates, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.CollectionRuleDefaults, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.GlobalCounter, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.CollectionRules, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.CorsConfiguration, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.DiagnosticPort, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.InProcessFeatures, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Metrics, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Storage, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.DefaultProcess, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Logging, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.DotnetMonitorDebug, skipNotPresent, includeChildSections: true, showSources: showSources);

                if (full)
                {
                    ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: true, showSources: showSources);
                    ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: true, showSources: showSources);
                }
                else
                {
                    IConfigurationSection? auth = ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: false, showSources: showSources);
                    if (null != auth)
                    {
                        using (new JsonObjectContext(_writer))
                        {
                            IConfigurationSection? monitorApiKey = ProcessChildSection(auth, ConfigurationKeys.MonitorApiKey, skipNotPresent, includeChildSections: false, showSources: showSources);
                            if (null != monitorApiKey)
                            {
                                using (new JsonObjectContext(_writer))
                                {
                                    ProcessChildSection(monitorApiKey, nameof(MonitorApiKeyOptions.Subject), skipNotPresent, includeChildSections: false, redact: false, showSources: showSources);
                                    // The PublicKey should only ever contain the public key, however we expect that accidents may occur and we should
                                    // redact this field in the event the JWK contains the private key information.
                                    ProcessChildSection(monitorApiKey, nameof(MonitorApiKeyOptions.PublicKey), skipNotPresent, includeChildSections: false, redact: true, showSources: showSources);
                                }
                            }

                            // No sensitive information
                            IConfigurationSection? azureAd = ProcessChildSection(auth, ConfigurationKeys.AzureAd, skipNotPresent, includeChildSections: true, showSources: showSources);
                        }
                    }

                    IConfigurationSection? egress = ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: false, showSources: showSources);
                    if (egress != null)
                    {
                        using (new JsonObjectContext(_writer))
                        {
                            ProcessEgressSection(egress, skipNotPresent, showSources: showSources);
                        }
                    }
                }
            }
        }

        private void ProcessEgressSection(IConfiguration egress, bool skipNotPresent, bool showSources = false)
        {
            IList<string> processedSectionPaths = new List<string>();

            // Redact all the properties since they could include secrets such as storage keys
            IConfigurationSection? propertiesSection = ProcessChildSection(egress, ConfigurationKeys.Egress_Properties, skipNotPresent, includeChildSections: true, redact: true, showSources: showSources);
            if (null != propertiesSection)
            {
                processedSectionPaths.Add(propertiesSection.Path);
            }

            IConfigurationSection? fileSystemProviderSection = ProcessChildSection(egress, EgressProviderTypes.FileSystem, skipNotPresent, includeChildSections: false, showSources: showSources);
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
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.DirectoryPath), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(FileSystemEgressProviderOptions.CopyBufferSize), skipNotPresent, includeChildSections: false, showSources: showSources);
                        }
                    }
                }
            }

            //Emit other egress entries, with redaction
            foreach (IConfigurationSection childSection in egress.GetChildren())
            {
                if (!processedSectionPaths.Contains(childSection.Path))
                {
                    ProcessChildSection(egress, childSection.Key, skipNotPresent, includeChildSections: true, redact: true, showSources: showSources);
                }
            }
        }

        private IConfigurationSection? ProcessChildSection(IConfiguration parentSection, string key, bool skipNotPresent, bool includeChildSections = true, bool redact = false, bool showSources = false)
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

            ProcessSection(section, includeChildSections, redact, showSources: showSources);

            return section;
        }

        private void ProcessSection(IConfigurationSection section, bool includeChildSections = true, bool redact = false, bool showSources = false)
        {
            _writer.WritePropertyName(section.Key);

            IEnumerable<IConfigurationSection> children = section.GetChildren();

            bool canWriteChildren = CanWriteChildren(section, children);

            //If we do not traverse the child sections, the caller is responsible for creating the value
            if (includeChildSections && canWriteChildren)
            {
                bool isSequentialIndices = CheckForSequentialIndices(children);

                bool parentIsCR = section.Key.Equals(ConfigurationKeys.CollectionRules);

                if (isSequentialIndices && !parentIsCR)
                {
                    _writer.WriteStartArray();

                    foreach (IConfigurationSection child in children)
                    {
                        if (child.GetChildren().Any())
                        {
                            ProcessChildren(child, includeChildSections, redact, showSources: showSources);
                        }
                        else
                        {
                            WriteValue(child.Value, redact);

                            string comment = GetConfigurationProvider(child, showSources);

                            if (comment.Length > 0)
                            {
                                // TODO: Comments are currently written after Key/Value pairs due to a limitation in System.Text.Json
                                // that prevents comments from being directly after commas
                                _writer.WriteCommentValue(comment);
                            }
                        }
                    }

                    _writer.WriteEndArray();
                }
                else
                {
                    ProcessChildren(section, includeChildSections, redact, showSources: showSources);
                }
            }
            else if (!canWriteChildren)
            {
                WriteValue(section.Value, redact);

                string comment = GetConfigurationProvider(section, showSources);

                if (comment.Length > 0)
                {
                    // TODO: Comments are currently written after Key/Value pairs due to a limitation in System.Text.Json
                    // that prevents comments from being directly after commas
                    _writer.WriteCommentValue(comment);
                }
            }
        }
#nullable disable
        private string GetConfigurationProvider(IConfigurationSection section, bool showSources)
        {
            if (showSources && _configuration.TryGetProviderAndValue(section.Path, out IConfigurationProvider provider, out _))
            {
                return provider.ToString();
            }
            return string.Empty;
        }
#nullable restore

        private static bool CanWriteChildren(IConfigurationSection section, IEnumerable<IConfigurationSection> children)
        {
            if (section.Path.Equals(nameof(RootOptions.DiagnosticPort)))
            {
                return string.IsNullOrEmpty(section.Value);
            }

            return children.Any();
        }

        private void WriteValue(string? value, bool redact)
        {
            string? valueToWrite = redact ? Strings.Placeholder_Redacted : value;

            _writer.WriteStringValue(valueToWrite);
        }

        private void ProcessChildren(IConfigurationSection section, bool includeChildSections, bool redact, bool showSources)
        {
            using (new JsonObjectContext(_writer))
            {
                foreach (IConfigurationSection child in section.GetChildren())
                {
                    ProcessSection(child, includeChildSections, redact, showSources: showSources);
                }
            }
        }

        private static bool CheckForSequentialIndices(IEnumerable<IConfigurationSection> children)
        {
            int indexValue = 0;

            foreach (IConfigurationSection child in children)
            {
                if (!child.Key.Equals(indexValue.ToString(CultureInfo.InvariantCulture)))
                {
                    return false;
                }

                indexValue++;
            }

            return true;
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

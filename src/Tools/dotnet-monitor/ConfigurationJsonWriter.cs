// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private IConfiguration _configuration;
        private IServiceProvider _serviceProvider;

        public ConfigurationJsonWriter(Stream outputStream)
        {
            JsonWriterOptions options = new() { Indented = true };
            _writer = new Utf8JsonWriter(outputStream, options);
        }

        public void Write(IConfiguration configuration, bool full, bool skipNotPresent, bool showSources = false, IServiceProvider serviceProvider = null)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            //Note that we avoid IConfigurationRoot.GetDebugView because it shows everything
            //CONSIDER Should we show this in json, since it cannot represent complete configuration?
            //CONSIDER Should we convert number based names to arrays?
            //CONSIDER There's probably room for making this code more generalized, to separate the output from the traversal
            //but it seemed like over-engineering.

            using (new JsonObjectContext(_writer))
            {
                ProcessChildSection(configuration, WebHostDefaults.ServerUrlsKey, skipNotPresent, showSources: showSources);
                IConfigurationSection kestrel = ProcessChildSection(configuration, "Kestrel", skipNotPresent, includeChildSections: false, showSources: showSources);
                if (kestrel != null)
                {
                    using (new JsonObjectContext(_writer))
                    {
                        IConfigurationSection certificates = ProcessChildSection(kestrel, "Certificates", skipNotPresent, includeChildSections: false, showSources: showSources);
                        if (certificates != null)
                        {
                            using (new JsonObjectContext(_writer))
                            {
                                IConfigurationSection defaultCert = ProcessChildSection(certificates, "Default", skipNotPresent, includeChildSections: false, showSources: showSources);
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
                ProcessChildSection(configuration, ConfigurationKeys.CollectionRuleDefaults, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.GlobalCounter, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.CollectionRules, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.CorsConfiguration, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.DiagnosticPort, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Metrics, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Storage, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.DefaultProcess, skipNotPresent, includeChildSections: true, showSources: showSources);
                ProcessChildSection(configuration, ConfigurationKeys.Logging, skipNotPresent, includeChildSections: true, showSources: showSources);

                if (full)
                {
                    ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: true, showSources: showSources);
                    ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: true, showSources: showSources);
                }
                else
                {
                    IConfigurationSection auth = ProcessChildSection(configuration, ConfigurationKeys.Authentication, skipNotPresent, includeChildSections: false, showSources: showSources);
                    if (null != auth)
                    {
                        using (new JsonObjectContext(_writer))
                        {
                            IConfigurationSection monitorApiKey = ProcessChildSection(auth, ConfigurationKeys.MonitorApiKey, skipNotPresent, includeChildSections: false, showSources: showSources);
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
                        }
                    }

                    IConfigurationSection egress = ProcessChildSection(configuration, ConfigurationKeys.Egress, skipNotPresent, includeChildSections: false, showSources: showSources);
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
            IConfigurationSection propertiesSection = ProcessChildSection(egress, nameof(EgressOptions.Properties), skipNotPresent, includeChildSections: true, redact: true, showSources: showSources);
            if (null != propertiesSection)
            {
                processedSectionPaths.Add(propertiesSection.Path);
            }

            IConfigurationSection azureBlobProviderSection = ProcessChildSection(egress, nameof(EgressOptions.AzureBlobStorage), skipNotPresent, includeChildSections: false, showSources: showSources);
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
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountUri), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.BlobPrefix), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.ContainerName), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.CopyBufferSize), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.QueueName), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.QueueAccountUri), skipNotPresent, includeChildSections: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature), skipNotPresent, includeChildSections: false, redact: true, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKey), skipNotPresent, includeChildSections: false, redact: true, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.SharedAccessSignatureName), skipNotPresent, includeChildSections: false, redact: false, showSources: showSources);
                            ProcessChildSection(optionsSection, nameof(AzureBlobEgressProviderOptions.AccountKeyName), skipNotPresent, includeChildSections: false, redact: false, showSources: showSources);
                        }
                    }
                }
            }

            IConfigurationSection fileSystemProviderSection = ProcessChildSection(egress, nameof(EgressOptions.FileSystem), skipNotPresent, includeChildSections: false, showSources: showSources);
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

        private IConfigurationSection ProcessChildSection(IConfiguration parentSection, string key, bool skipNotPresent, bool includeChildSections = true, bool redact = false, bool showSources = false)
        {
            bool loadCRDefaults = key.Equals(ConfigurationKeys.CollectionRules);

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

            ProcessSection(section, includeChildSections, redact, showSources: showSources, loadCRDefaults: loadCRDefaults);

            return section;
        }

        private void ProcessSection(IConfigurationSection section, bool includeChildSections = true, bool redact = false, bool showSources = false, bool loadCRDefaults = false, List<(string, string)> toMock = null, IndependentConfigFlags configFlag = IndependentConfigFlags.None)
        {
            if ((null == toMock || !toMock.Any()) && loadCRDefaults)
            {
                toMock = CollectionRuleDefaultsSetup(section, ref configFlag);
            }

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
                            if (loadCRDefaults && section.Key.Equals("Actions"))
                            {
                                toMock = GetMockedChildren(GetActionOptionsType(child));

                                var updatedFlag = !child.GetSection("Settings").Exists() ? IndependentConfigFlags.MockSettings : IndependentConfigFlags.MockValuesNext;

                                configFlag = toMock.Any() ? updatedFlag : IndependentConfigFlags.None;
                            }

                            ProcessChildren(child, includeChildSections, redact, showSources: showSources, loadCRDefaults: loadCRDefaults, configFlag: configFlag, toMock: toMock);
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
                    ProcessChildren(section, includeChildSections, redact, showSources: showSources, loadCRDefaults: loadCRDefaults, toMock, configFlag: configFlag);
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

        private List<(string, string)> CollectionRuleDefaultsSetup(IConfigurationSection section, ref IndependentConfigFlags configFlag)
        {
            List<(string, string)> toMock = new();

            Type typeToUse = null;
            IndependentConfigFlags updatedFlag = IndependentConfigFlags.None;

            if (configFlag == IndependentConfigFlags.IsCollectionRule && !section.GetSection("Limits").Exists())
            {
                typeToUse = typeof(CollectionRuleLimitsOptions);
                updatedFlag = IndependentConfigFlags.MockLimits;
            }
            else if (section.Key.Equals(nameof(CollectionRuleOptions.Trigger)))
            {
                typeToUse = GetTriggerOptionsType(section);
                updatedFlag = !section.GetSection("Settings").Exists() ? IndependentConfigFlags.MockSettings : IndependentConfigFlags.MockValuesNext;
            }
            else if (section.Key.Equals(nameof(CollectionRuleOptions.Limits)))
            {
                typeToUse = typeof(CollectionRuleLimitsOptions);
                updatedFlag = IndependentConfigFlags.None;
            }
            else if (section.Key.Equals(ConfigurationKeys.CollectionRules))
            {
                configFlag = IndependentConfigFlags.IsCollectionRule;
                return null;
            }

            toMock = typeToUse != null ? GetMockedChildren(typeToUse) : toMock;

            configFlag = toMock.Any() ? updatedFlag : IndependentConfigFlags.None;

            return toMock;
        }

        private string GetConfigurationProvider(IConfigurationSection section, bool showSources)
        {
            if (showSources)
            {
                var configurationProviders = ((IConfigurationRoot)_configuration).Providers.Reverse();

                string comment = string.Empty;

                foreach (var provider in configurationProviders)
                {
                    provider.TryGet(section.Path, out string value);

                    if (!string.IsNullOrEmpty(value))
                    {
                        comment = provider.ToString();
                        break;
                    }
                }

                return comment;
            }

            return string.Empty;
        }

        private bool CanWriteChildren(IConfigurationSection section, IEnumerable<IConfigurationSection> children)
        {
            if (section.Path.Equals(nameof(RootOptions.DiagnosticPort)))
            {
                return string.IsNullOrEmpty(section.Value);
            }

            return children.Any();
        }

        private void WriteValue(string value, bool redact)
        {
            string valueToWrite = redact ? Strings.Placeholder_Redacted : value;

            _writer.WriteStringValue(valueToWrite);
        }

        private void ProcessChildren(IConfigurationSection section, bool includeChildSections, bool redact, bool showSources, bool loadCRDefaults, List<(string, string)> toMock = null, IndependentConfigFlags configFlag = IndependentConfigFlags.None)
        {
            using (new JsonObjectContext(_writer))
            {
                IndependentConfigFlags flagToUse = configFlag;

                bool shouldMock = null != toMock;
                if (flagToUse == IndependentConfigFlags.MockValuesNext)
                {
                    shouldMock = false;
                    flagToUse = IndependentConfigFlags.None;
                }

                if (configFlag != IndependentConfigFlags.IsCollectionRule)
                {
                    configFlag = IndependentConfigFlags.None;
                }

                foreach (IConfigurationSection child in section.GetChildren())
                {
                    ProcessSection(child, includeChildSections, redact, showSources: showSources, loadCRDefaults: loadCRDefaults, toMock: shouldMock ? null : toMock, configFlag: configFlag);
                }

                var childKeys = section.GetChildren().Select(x => x.Key).ToList();

                if (shouldMock)
                {
                    if (flagToUse == IndependentConfigFlags.MockSettings)
                    {
                        MockSection(toMock, childKeys, "Settings", showSources);
                    }
                    else if (flagToUse == IndependentConfigFlags.MockLimits)
                    {
                        MockSection(toMock, childKeys, "Limits", showSources);
                    }
                    else
                    {
                        MockChildren(toMock, childKeys, showSources);
                    }
                }
            }
        }

        private void MockSection(List<(string, string)> toMock, List<string> childKeys, string propertyName, bool showSources)
        {
            _writer.WritePropertyName(propertyName);

            using (new JsonObjectContext(_writer))
            {
                MockChildren(toMock, childKeys, showSources);
            }
        }

        private void MockChildren(List<(string, string)> mockedChildren, List<string> childKeys, bool showSources)
        {
            foreach (var mockedChild in mockedChildren)
            {
                if (!childKeys.Contains(mockedChild.Item1))
                {
                    _writer.WritePropertyName(mockedChild.Item1);
                    _writer.WriteStringValue(mockedChild.Item2);

                    if (showSources)
                    {
                        // TODO: Comments are currently written after Key/Value pairs due to a limitation in System.Text.Json
                        // that prevents comments from being directly after commas
                        _writer.WriteCommentValue("Collection Rule Defaults");
                    }
                }
            }
        }

        private List<(string, string)> GetMockedChildren(Type optionsType)
        {
            var childrenToMock = new List<(string, string)>();

            var settingsPropsNames = optionsType.GetProperties().Select(x => x.Name);

            var crdProps = typeof(CollectionRuleDefaultsOptions).GetProperties();

            foreach (var crdProp in crdProps)
            {
                if (settingsPropsNames.Contains(crdProp.Name))
                {
                    string valToUse = _configuration.GetSection($"{ConfigurationKeys.CollectionRuleDefaults}:{crdProp.Name}").Value;

                    if (!string.IsNullOrEmpty(valToUse))
                    {
                        childrenToMock.Add((crdProp.Name, valToUse));
                    }
                }
            }

            return childrenToMock;
        }

        private Type GetActionOptionsType(IConfigurationSection section)
        {
            string actionTypeName = section.GetSection("Type").Value;

            var actionOperations = _serviceProvider.GetService<ICollectionRuleActionOperations>();

            actionOperations.TryCreateOptions(actionTypeName, out object actionSettings);

            return actionSettings.GetType();
        }

        private Type GetTriggerOptionsType(IConfigurationSection section)
        {
            string triggerTypeName = section.GetSection("Type").Value;

            var triggerOperations = _serviceProvider.GetService<ICollectionRuleTriggerOperations>();

            triggerOperations.TryCreateOptions(triggerTypeName, out object triggerSettings);

            return triggerSettings.GetType();
        }


        private bool CheckForSequentialIndices(IEnumerable<IConfigurationSection> children)
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

        // None of these fields can occur simultaneously; to minimize optional parameters, we pass one parameter that can hold one of these values
        public enum IndependentConfigFlags
        {
            None,
            MockSettings,
            MockLimits,
            MockValuesNext,
            IsCollectionRule
        }
    }
}

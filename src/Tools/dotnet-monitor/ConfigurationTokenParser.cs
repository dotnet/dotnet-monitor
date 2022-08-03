﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TokenContext
    {
        public bool CloneOnSubstitution { get; set; } = true;

        public Guid RuntimeId { get; set; } = Guid.Empty;

        public int ProcessId { get; set; } = -1;

        public IDictionary<string, string> EnvironmentBlock { get; set; } = new Dictionary<string, string>();
    }

    internal sealed class ConfigurationTokenParser
    {
        private readonly ILogger _logger;

        public const string SubstitutionPrefix = "$(";
        public const string SubstitutionSuffix = ")";
        public const string Separator = ".";

        private const string ProcessInfoReference = "Process";
        private const string RuntimeId = "RuntimeId";
        public static readonly string RuntimeIdReference = FormattableString.Invariant($"{SubstitutionPrefix}{ProcessInfoReference}{Separator}{RuntimeId}{SubstitutionSuffix}");

        public ConfigurationTokenParser(ILogger logger)
        {
            _logger = logger;
        }

        public object SubstituteOptionValues(object originalSettings, TokenContext context)
        {
            object settings = originalSettings;

            foreach (PropertyInfo propertyInfo in GetPropertiesFromSettings(settings))
            {
                string originalPropertyValue = (string)propertyInfo.GetValue(settings);
                if (string.IsNullOrEmpty(originalPropertyValue))
                {
                    continue;
                }

                string replacement = originalPropertyValue.Replace(RuntimeIdReference, context.RuntimeId.ToString("D"), StringComparison.Ordinal);

                if (!ReferenceEquals(replacement, originalPropertyValue))
                {
                    if (context.CloneOnSubstitution && !TryCloneSettings(originalSettings, ref settings))
                    {
                        return settings;
                    }
                    propertyInfo.SetValue(settings, replacement);
                }
            }

            return settings;
        }

        public bool TryCloneSettings(object originalSettings, ref object settings)
        {
            if (originalSettings == null)
            {
                return false;
            }

            if (ReferenceEquals(originalSettings, settings))
            {
                if (originalSettings is BaseRecordOptions baseRecord)
                {
                    //Creates a copy using record's Clone method.
                    settings = baseRecord with { };
                    return true;
                }
                else
                {
                    _logger.ActionSettingsTokenizationNotSupported(settings.GetType().FullName);
                    settings = originalSettings;
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<PropertyInfo> GetPropertiesFromSettings(object settings, Predicate<PropertyInfo> predicate = null) =>
            settings?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && (predicate?.Invoke(p) ?? true)) ??
            Enumerable.Empty<PropertyInfo>();
    }
}

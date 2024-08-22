// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TokenContext
    {
        public bool CloneOnSubstitution { get; set; } = true;

        public Guid RuntimeId { get; set; } = Guid.Empty;

        public int ProcessId { get; set; }

        public string ProcessName { get; set; } = string.Empty;

        public string CommandLine { get; set; } = string.Empty;

        public string MonitorHostName { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }

        public IDictionary<string, string> EnvironmentBlock { get; set; } = new Dictionary<string, string>();
    }

    internal sealed class ConfigurationTokenParser
    {
        private readonly ILogger _logger;

        public const string SubstitutionPrefix = "$(";
        public const string SubstitutionSuffix = ")";
        public const string Separator = ".";

        private const string MonitorInfoReference = "Monitor";
        private const string ProcessInfoReference = "Process";

        private const string RuntimeId = "RuntimeId";
        private const string ProcessId = "ProcessId";
        private const string ProcessName = "Name";
        private const string CommandLine = "CommandLine";
        private const string HostName = "HostName";
        private const string UnixTime = "UnixTime";

        public static readonly string RuntimeIdReference = CreateTokenReference(ProcessInfoReference, RuntimeId);
        public static readonly string ProcessIdReference = CreateTokenReference(ProcessInfoReference, ProcessId);
        public static readonly string ProcessNameReference = CreateTokenReference(ProcessInfoReference, ProcessName);
        public static readonly string CommandLineReference = CreateTokenReference(ProcessInfoReference, CommandLine);
        public static readonly string HostNameReference = CreateTokenReference(MonitorInfoReference, HostName);
        public static readonly string UnixTimeReference = CreateTokenReference(MonitorInfoReference, UnixTime);

        public ConfigurationTokenParser(ILogger logger)
        {
            _logger = logger;
        }

        public object? SubstituteOptionValues(object? originalSettings, TokenContext context)
        {
            object? settings = originalSettings;

            foreach (PropertyInfo propertyInfo in GetPropertiesFromSettings(settings))
            {
                string? originalPropertyValue = (string?)propertyInfo.GetValue(settings);
                if (string.IsNullOrEmpty(originalPropertyValue))
                {
                    continue;
                }

                string replacement = originalPropertyValue.Replace(RuntimeIdReference, context.RuntimeId.ToString("D"), StringComparison.Ordinal);
                replacement = replacement.Replace(ProcessIdReference, context.ProcessId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
                replacement = replacement.Replace(ProcessNameReference, context.ProcessName, StringComparison.Ordinal);
                replacement = replacement.Replace(CommandLineReference, context.CommandLine, StringComparison.Ordinal);
                replacement = replacement.Replace(HostNameReference, context.MonitorHostName, StringComparison.Ordinal);
                replacement = replacement.Replace(UnixTimeReference, context.Timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

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

        public bool TryCloneSettings(object? originalSettings, ref object? settings)
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
#nullable disable
                    _logger.ActionSettingsTokenizationNotSupported(settings.GetType().FullName);
#nullable restore
                    settings = originalSettings;
                    return false;
                }
            }
            return true;
        }

        public static IEnumerable<PropertyInfo> GetPropertiesFromSettings(object? settings, Predicate<PropertyInfo>? predicate = null) =>
            settings?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && (predicate?.Invoke(p) ?? true)) ??
            Enumerable.Empty<PropertyInfo>();

        private static string CreateTokenReference(string category, string token) =>
            FormattableString.Invariant($"{SubstitutionPrefix}{category}{Separator}{token}{SubstitutionSuffix}");
    }
}

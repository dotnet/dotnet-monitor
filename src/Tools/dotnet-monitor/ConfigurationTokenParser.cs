// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public object? SubstituteOptionValues(CollectionRuleActionOptions actionOptions, TokenContext context)
        {
            var originalSettings = actionOptions.Settings;
            object? settings = originalSettings;

            foreach (PropertyInfo propertyInfo in GetPropertiesFromSettings(actionOptions))
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

        public static IEnumerable<PropertyInfo> GetPropertiesFromSettings(CollectionRuleActionOptions actionOptions, Predicate<PropertyInfo>? predicate = null)
        {
            object? settings = actionOptions.Settings;
            return actionOptions.Type switch {
                KnownCollectionRuleActions.CollectDump => GetPropertiesFromSettings(typeof(CollectDumpOptions), predicate),
                KnownCollectionRuleActions.CollectExceptions => GetPropertiesFromSettings(typeof(CollectExceptionsOptions), predicate),
                KnownCollectionRuleActions.CollectGCDump => GetPropertiesFromSettings(typeof(CollectGCDumpOptions), predicate),
                KnownCollectionRuleActions.CollectLogs => GetPropertiesFromSettings(typeof(CollectLogsOptions), predicate),
                KnownCollectionRuleActions.CollectStacks => GetPropertiesFromSettings(typeof(CollectStacksOptions), predicate),
                KnownCollectionRuleActions.CollectTrace => GetPropertiesFromSettings(typeof(CollectTraceOptions), predicate),
                KnownCollectionRuleActions.CollectLiveMetrics => GetPropertiesFromSettings(typeof(CollectLiveMetricsOptions), predicate),
                KnownCollectionRuleActions.Execute => GetPropertiesFromSettings(typeof(ExecuteOptions), predicate),
                KnownCollectionRuleActions.LoadProfiler => GetPropertiesFromSettings(typeof(LoadProfilerOptions), predicate),
                KnownCollectionRuleActions.SetEnvironmentVariable => GetPropertiesFromSettings(typeof(SetEnvironmentVariableOptions), predicate),
                KnownCollectionRuleActions.GetEnvironmentVariable => GetPropertiesFromSettings(typeof(GetEnvironmentVariableOptions), predicate),
                _ => throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unknown action type: {0}",
                    actionOptions.Type))
            };

            static IEnumerable<PropertyInfo> GetPropertiesFromSettings([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Predicate<PropertyInfo>? predicate = null)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(string) && (predicate?.Invoke(p) ?? true))
                    .ToArray();
            }
        }

        private static string CreateTokenReference(string category, string token) =>
            FormattableString.Invariant($"{SubstitutionPrefix}{category}{Separator}{token}{SubstitutionSuffix}");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.TestCommon;
#endif
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class CommonOptionsMapper
    {
        private const string KeySegmentSeparator = "__";

        /// <summary>
        /// Generates a map of options that can be passed directly to configuration via an in-memory collection.
        /// </summary>
        /// <remarks>
        /// Each key is the configuration path; each value is the configuration path value.
        /// </remarks>
        public IDictionary<string, string> ToConfigurationValues(RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapRootOptions(options, string.Empty, ConfigurationPath.KeyDelimiter, variables);
            return variables;
        }

        /// <summary>
        /// Generates an environment variable map of the options.
        /// </summary>
        /// <remarks>
        /// Each key is the variable name; each value is the variable value.
        /// </remarks>
        public IDictionary<string, string> ToEnvironmentConfiguration(RootOptions options, bool useDotnetMonitorPrefix = false)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapRootOptions(options, useDotnetMonitorPrefix ? ToolIdentifiers.StandardPrefix : string.Empty, KeySegmentSeparator, variables);
            return variables;
        }

        /// <summary>
        /// Generates a key-per-file map of the options.
        /// </summary>
        /// <remarks>
        /// Each key is the file name; each value is the file content.
        /// </remarks>
        public IDictionary<string, string> ToKeyPerFileConfiguration(RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapRootOptions(options, string.Empty, KeySegmentSeparator, variables);
            return variables;
        }

        // private static void MapDictionary(IDictionary dictionary, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     foreach (var key in dictionary.Keys)
        //     {
        //         object? value = dictionary[key];

        //         if (null != value)
        //         {
        //             string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
        //             MapValue(
        //                 value,
        //                 FormattableString.Invariant($"{prefix}{keyString}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        // private static void MapList(IList list, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     for (int index = 0; index < list.Count; index++)
        //     {
        //         object? value = list[index];
        //         if (null != value)
        //         {
        //             MapValue(
        //                 value,
        //                 FormattableString.Invariant($"{prefix}{index}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        private void MapRootOptions(RootOptions obj, string prefix, string separator, IDictionary<string, string> map)
        {
            // TODO: in Tests, it has an additional property. Weird.
            MapAuthenticationOptions(obj.Authentication, FormattableString.Invariant($"{prefix}{nameof(obj.Authentication)}"), separator, map);
            MapDictionary_String_CollectionRuleOptions(obj.CollectionRules, FormattableString.Invariant($"{prefix}{nameof(obj.CollectionRules)}"), separator, map);
            // GlobalCounterOptions
            MapGlobalCounterOptions(obj.GlobalCounter, FormattableString.Invariant($"{prefix}{nameof(obj.GlobalCounter)}"), separator, map);
            // InProcessFeaturesOptions
            // CorsConfigurationOptions
            MapDiagnosticPortOptions(obj.DiagnosticPort, FormattableString.Invariant($"{prefix}{nameof(obj.DiagnosticPort)}"), separator, map);
            MapEgressOptions(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), separator, map);
            // MetricsOptions
            MapStorageOptions(obj.Storage, FormattableString.Invariant($"{prefix}{nameof(obj.Storage)}"), separator, map);
            // ProcessFilterOptions
            MapCollectionRuleDefaultsOptions(obj.CollectionRuleDefaults, FormattableString.Invariant($"{prefix}{nameof(obj.CollectionRuleDefaults)}"), separator, map);
            // Templates
            // DotnetMonitorDebugOptions
            // FOR TESTS: Logging?
        }

        // private static void MapObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object obj, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        //     {
        //         if (!property.GetIndexParameters().Any())
        //         {
        //             MapValue(
        //                 property.GetValue(obj),
        //                 FormattableString.Invariant($"{prefix}{property.Name}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        private void MapDictionary_String_CollectionRuleOptions(IDictionary<string, CollectionRuleOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                var prefix = FormattableString.Invariant($"{valueName}{separator}"); // passed to mapdictionary.
                foreach ((string key, CollectionRuleOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapCollectionRuleOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private void MapCollectionRuleOptions(CollectionRuleOptions obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapFilters(obj.Filters, FormattableString.Invariant($"{prefix}{nameof(obj.Filters)}"), separator, map);
            MapCollectionRuleTriggerOptions(obj.Trigger, FormattableString.Invariant($"{prefix}{nameof(obj.Trigger)}"), separator, map);
            MapActions(obj.Actions, FormattableString.Invariant($"{prefix}{nameof(obj.Actions)}"), separator, map);
            MapLimits(obj.Limits, FormattableString.Invariant($"{prefix}{nameof(obj.Limits)}"), separator, map);
        }

        private void MapActions(List<CollectionRuleActionOptions> obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            for (int index = 0; index < obj.Count; index++)
            {
                CollectionRuleActionOptions value = obj[index];
                MapCollectionRuleActionOptions(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
            }
        }

        private void MapCollectionRuleActionOptions(CollectionRuleActionOptions obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapString(obj.Name, FormattableString.Invariant($"{prefix}{nameof(obj.Name)}"), map);
            MapString(obj.Type, FormattableString.Invariant($"{prefix}{nameof(obj.Type)}"), map);
            MapCollectionRuleActionOptions_Settings(obj.Type, obj.Settings, FormattableString.Invariant($"{prefix}{nameof(obj.Settings)}"), separator, map);
            MapBool(obj.WaitForCompletion, FormattableString.Invariant($"{prefix}{nameof(obj.WaitForCompletion)}"), map);
        }

        private Dictionary<string, Action<object, string, string, IDictionary<string, string>>>? _actionSettingsMap;

        public void AddActionSettings<TSettings>(string type, Action<TSettings, string, string, IDictionary<string, string>> mapAction)
        {
            (_actionSettingsMap ??= new()).Add(type, (obj, valueName, separator, map) =>
            {
                mapAction((TSettings)obj, valueName, separator, map);
            });
        }

        private void MapCollectionRuleActionOptions_Settings(string type, object? settings, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != settings)
            {
                // TODO: inline the well-known ones to avoid a dictionary lookup.
                switch (type)
                {
                    case KnownCollectionRuleActions.CollectDump:
                        MapCollectDumpOptions(settings as CollectDumpOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.CollectExceptions:
                        MapCollectExceptionsOptions(settings as CollectExceptionsOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.CollectGCDump:
                        MapCollectGCDumpOptions(settings as CollectGCDumpOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.CollectLiveMetrics:
                        MapCollectLiveMetricsOptions(settings as CollectLiveMetricsOptions, valueName, separator, map);
                        break;
                    // case KnownCollectionRuleActions.CollectLogs:
                    //     MapCollectLogsOptions(settings as CollectLogsOptions, valueName, separator, map);
                    //     break;
                    case KnownCollectionRuleActions.CollectStacks:
                        MapCollectStacksOptions(settings as CollectStacksOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.CollectTrace:
                        MapCollectTraceOptions(settings as CollectTraceOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.Execute:
                        MapExecuteOptions(settings as ExecuteOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.LoadProfiler:
                        MapLoadProfilerOptions(settings as LoadProfilerOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.SetEnvironmentVariable:
                        MapSetEnvironmentVariableOptions(settings as SetEnvironmentVariableOptions, valueName, separator, map);
                        break;
                    case KnownCollectionRuleActions.GetEnvironmentVariable:
                        MapGetEnvironmentVariableOptions(settings as GetEnvironmentVariableOptions, valueName, separator, map);
                        break;
                    default:
                        if (_actionSettingsMap?.TryGetValue(type, out Action<object, string, string, IDictionary<string, string>>? mapAction) == true)
                        {
                            mapAction(settings, valueName, separator, map);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unknown action type: {type}");
                        }
                        break;
                }
            }
        }

        private static void MapLimits(CollectionRuleLimitsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapInt(obj.ActionCount, FormattableString.Invariant($"{prefix}{nameof(obj.ActionCount)}"), map);
                MapTimeSpan(obj.ActionCountSlidingWindowDuration, FormattableString.Invariant($"{prefix}{nameof(obj.ActionCountSlidingWindowDuration)}"), map);
                MapTimeSpan(obj.RuleDuration, FormattableString.Invariant($"{prefix}{nameof(obj.RuleDuration)}"), map);
            }
        }

        private static void MapCollectDumpOptions(CollectDumpOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapDumpType(obj.Type, FormattableString.Invariant($"{prefix}{nameof(obj.Type)}"), map);
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapCollectExceptionsOptions(CollectExceptionsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapExceptionFormat(obj.Format, FormattableString.Invariant($"{prefix}{nameof(obj.Format)}"), map);
                MapExceptionsConfiguration(obj.Filters, FormattableString.Invariant($"{prefix}{nameof(obj.Filters)}"), separator, map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapExceptionFormat(ExceptionFormat? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapExceptionsConfiguration(ExceptionsConfiguration? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapList_ExceptionFilter(obj.Include, FormattableString.Invariant($"{prefix}{nameof(obj.Include)}"), separator, map);
                MapList_ExceptionFilter(obj.Exclude, FormattableString.Invariant($"{prefix}{nameof(obj.Exclude)}"), separator, map);
            }
        }

        private static void MapList_ExceptionFilter(List<ExceptionFilter>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Count; index++)
                {
                    ExceptionFilter value = obj[index];
                    MapExceptionFilter(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapExceptionFilter(ExceptionFilter obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapString(obj.ExceptionType, FormattableString.Invariant($"{prefix}{nameof(obj.ExceptionType)}"), map);
            MapString(obj.ModuleName, FormattableString.Invariant($"{prefix}{nameof(obj.ModuleName)}"), map);
            MapString(obj.TypeName, FormattableString.Invariant($"{prefix}{nameof(obj.TypeName)}"), map);
            MapString(obj.MethodName, FormattableString.Invariant($"{prefix}{nameof(obj.MethodName)}"), map);
        }

        private static void MapCollectGCDumpOptions(CollectGCDumpOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapCollectLiveMetricsOptions(CollectLiveMetricsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapBool(obj.IncludeDefaultProviders, FormattableString.Invariant($"{prefix}{nameof(obj.IncludeDefaultProviders)}"), map);
                MapArray_EventMetricsProvider(obj.Providers, FormattableString.Invariant($"{prefix}{nameof(obj.Providers)}"), separator, map);
                MapArray_EventMetricsMeter(obj.Meters, FormattableString.Invariant($"{prefix}{nameof(obj.Meters)}"), separator, map);
                MapTimeSpan(obj.Duration, FormattableString.Invariant($"{prefix}{nameof(obj.Duration)}"), map);
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapArray_EventMetricsProvider(EventMetricsProvider[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Length; index++)
                {
                    EventMetricsProvider value = obj[index];
                    MapEventMetricsProvider(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventMetricsProvider(EventMetricsProvider obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapString(obj.ProviderName, FormattableString.Invariant($"{prefix}{nameof(obj.ProviderName)}"), map);
            MapArray_String(obj.CounterNames, FormattableString.Invariant($"{prefix}{nameof(obj.CounterNames)}"), separator, map);
        }

        private static void MapArray_String(string[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Length; index++)
                {
                    string value = obj[index];
                    MapString(value, FormattableString.Invariant($"{prefix}{index}"), map);
                }
            }
        }

        private static void MapArray_EventMetricsMeter(EventMetricsMeter[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Length; index++)
                {
                    EventMetricsMeter value = obj[index];
                    MapEventMetricsMeter(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventMetricsMeter(EventMetricsMeter obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapString(obj.MeterName, FormattableString.Invariant($"{prefix}{nameof(obj.MeterName)}"), map);
            MapArray_String(obj.InstrumentNames, FormattableString.Invariant($"{prefix}{nameof(obj.InstrumentNames)}"), separator, map);
        }

        private static void MapCollectStacksOptions(CollectStacksOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapCallStackFormat(obj.Format, FormattableString.Invariant($"{prefix}{nameof(obj.Format)}"), map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapCallStackFormat(CallStackFormat? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapCollectTraceOptions(CollectTraceOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapTraceProfile(obj.Profile, FormattableString.Invariant($"{prefix}{nameof(obj.Profile)}"), map);
                MapEventProviders(obj.Providers, FormattableString.Invariant($"{prefix}{nameof(obj.Providers)}"), separator, map);
                MapBool(obj.RequestRundown, FormattableString.Invariant($"{prefix}{nameof(obj.RequestRundown)}"), map);
                MapInt(obj.BufferSizeMegabytes, FormattableString.Invariant($"{prefix}{nameof(obj.BufferSizeMegabytes)}"), map);
                MapTimeSpan(obj.Duration, FormattableString.Invariant($"{prefix}{nameof(obj.Duration)}"), map);
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
                MapTraceEventFilter(obj.StoppingEvent, FormattableString.Invariant($"{prefix}{nameof(obj.StoppingEvent)}"), separator, map);
                MapString(obj.ArtifactName, FormattableString.Invariant($"{prefix}{nameof(obj.ArtifactName)}"), map);
            }
        }

        private static void MapExecuteOptions(ExecuteOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Path, FormattableString.Invariant($"{prefix}{nameof(obj.Path)}"), map);
                MapString(obj.Arguments, FormattableString.Invariant($"{prefix}{nameof(obj.Arguments)}"), map);
                MapBool(obj.IgnoreExitCode, FormattableString.Invariant($"{prefix}{nameof(obj.IgnoreExitCode)}"), map);
            }
        }

        private static void MapLoadProfilerOptions(LoadProfilerOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Path, FormattableString.Invariant($"{prefix}{nameof(obj.Path)}"), map);
                MapGuid(obj.Clsid, FormattableString.Invariant($"{prefix}{nameof(obj.Clsid)}"), map);
            }
        }

        private static void MapGuid(Guid? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapSetEnvironmentVariableOptions(SetEnvironmentVariableOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Name, FormattableString.Invariant($"{prefix}{nameof(obj.Name)}"), map);
                MapString(obj.Value, FormattableString.Invariant($"{prefix}{nameof(obj.Value)}"), map);
            }
        }

        private static void MapGetEnvironmentVariableOptions(GetEnvironmentVariableOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Name, FormattableString.Invariant($"{prefix}{nameof(obj.Name)}"), map);
            }
        }

        private static void MapTraceProfile(TraceProfile? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapEventProviders(List<EventPipeProvider>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Count; index++)
                {
                    EventPipeProvider value = obj[index];
                    MapEventPipeProvider(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventPipeProvider(EventPipeProvider obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapString(obj.Name, FormattableString.Invariant($"{prefix}{nameof(obj.Name)}"), map);
            MapString(obj.Keywords, FormattableString.Invariant($"{prefix}{nameof(obj.Keywords)}"), map);
            MapEventLevel(obj.EventLevel, FormattableString.Invariant($"{prefix}{nameof(obj.EventLevel)}"), map);
            MapDictionary_String_String(obj.Arguments, FormattableString.Invariant($"{prefix}{nameof(obj.Arguments)}"), separator, map);
        }

        private static void MapEventLevel(EventLevel? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapTimeSpan(TimeSpan? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapTraceEventFilter(TraceEventFilter? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.ProviderName, FormattableString.Invariant($"{prefix}{nameof(obj.ProviderName)}"), map);
                MapString(obj.EventName, FormattableString.Invariant($"{prefix}{nameof(obj.EventName)}"), map);
                MapDictionary_String_String(obj.PayloadFilter, FormattableString.Invariant($"{prefix}{nameof(obj.PayloadFilter)}"), separator, map);
            }
        }

        private static void MapDictionary_String_String(IDictionary<string, string>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                foreach ((string key, string value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapString(value, FormattableString.Invariant($"{prefix}{keyString}"), map);
                }
            }
        }

        private static void MapDumpType(DumpType? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapFilters(List<ProcessFilterDescriptor>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                for (int index = 0; index < obj.Count; index++)
                {
                    ProcessFilterDescriptor value = obj[index];
                    MapProcessFilterDescriptor(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapProcessFilterDescriptor(ProcessFilterDescriptor obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = FormattableString.Invariant($"{valueName}{separator}");
            MapProcessFilterKey(obj.Key, FormattableString.Invariant($"{prefix}{nameof(obj.Key)}"), map);
            MapString(obj.Value, FormattableString.Invariant($"{prefix}{nameof(obj.Value)}"), map);
            MapProcessFilterType(obj.MatchType, FormattableString.Invariant($"{prefix}{nameof(obj.MatchType)}"), map);
            MapString(obj.ProcessName, FormattableString.Invariant($"{prefix}{nameof(obj.ProcessName)}"), map);
            MapString(obj.ProcessId, FormattableString.Invariant($"{prefix}{nameof(obj.ProcessId)}"), map);
            MapString(obj.CommandLine, FormattableString.Invariant($"{prefix}{nameof(obj.CommandLine)}"), map);
            MapString(obj.ManagedEntryPointAssemblyName, FormattableString.Invariant($"{prefix}{nameof(obj.ManagedEntryPointAssemblyName)}"), map);
        }

        private static void MapProcessFilterKey(ProcessFilterKey? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapProcessFilterType(ProcessFilterType? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapCollectionRuleTriggerOptions(CollectionRuleTriggerOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Type, FormattableString.Invariant($"{prefix}{nameof(obj.Type)}"), map);
                MapCollectionRuleTriggerOptions_Settings(obj.Type, obj.Settings, FormattableString.Invariant($"{prefix}{nameof(obj.Settings)}"), separator, map);
            }
        }

        private static void MapCollectionRuleTriggerOptions_Settings(string type, object? settings, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != settings)
            {
                switch (type)
                {
                    // case KnownCollectionRuleTriggers.AspNetRequestCount:
                    //     MapAspNetRequestCountOptions(settings as AspNetRequestCountOptions, valueName, separator, map);
                    //     break;
                    case KnownCollectionRuleTriggers.AspNetRequestDuration:
                        MapAspNetRequestDurationOptions(settings as AspNetRequestDurationOptions, valueName, separator, map);
                        break;
                    // case KnownCollectionRuleTriggers.AspNetResponseStatus:
                    //     MapAspNetResponseStatusOptions(settings as AspNetResponseStatusOptions, valueName, separator, map);
                    //     break;
                    // case KnownCollectionRuleTriggers.EventCounter:
                    //     MapEventCounterOptions(settings as EventCounterOptions, valueName, separator, map);
                    //     break;
                    // case KnownCollectionRuleTriggers.CPUUsage:
                    //     MapCPUUsageOptions(settings as CPUUsageOptions, valueName, separator, map);
                    //     break;
                    // case KnownCollectionRuleTriggers.GCHeapSize:
                    //     MapGCHeapSizeOptions(settings as GCHeapSizeOptions, valueName, separator, map);
                    //     break;
                    // case KnownCollectionRuleTriggers.ThreadpoolQueueLength:
                    //     MapThreadpoolQueueLengthOptions(settings as ThreadpoolQueueLengthOptions, valueName, separator, map);
                    //     break;
                    // case KnownCollectionRuleTriggers.EventMeter:
                    //     MapEventMeterOptions(settings as EventMeterOptions, valueName, separator, map);
                    //     break;
                    default:
                        throw new NotSupportedException($"Unknown trigger type: {type}");
                }
            }
        }

        private static void MapAspNetRequestDurationOptions(AspNetRequestDurationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapInt(obj.RequestCount, FormattableString.Invariant($"{prefix}{nameof(obj.RequestCount)}"), map);
                MapTimeSpan(obj.RequestDuration, FormattableString.Invariant($"{prefix}{nameof(obj.RequestDuration)}"), map);
                MapTimeSpan(obj.SlidingWindowDuration, FormattableString.Invariant($"{prefix}{nameof(obj.SlidingWindowDuration)}"), map);
                MapArray_String(obj.IncludePaths, FormattableString.Invariant($"{prefix}{nameof(obj.IncludePaths)}"), separator, map);
                MapArray_String(obj.ExcludePaths, FormattableString.Invariant($"{prefix}{nameof(obj.ExcludePaths)}"), separator, map);
            }
        }

        private static void MapAuthenticationOptions(AuthenticationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapMonitorApiKeyOptions(obj.MonitorApiKey, prefix, separator, map);
                MapAzureAdOptions(obj.AzureAd, prefix, separator, map);
            }
        }

        private static void MapMonitorApiKeyOptions(MonitorApiKeyOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Subject, FormattableString.Invariant($"{prefix}{nameof(obj.Subject)}"), map);
                MapString(obj.PublicKey, FormattableString.Invariant($"{prefix}{nameof(obj.PublicKey)}"), map);
                MapString(obj.Issuer, FormattableString.Invariant($"{prefix}{nameof(obj.Issuer)}"), map);
            }
        }

        private static void MapAzureAdOptions(AzureAdOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapUri(obj.Instance, FormattableString.Invariant($"{prefix}{nameof(obj.Instance)}"), separator, map);
                MapString(obj.TenantId, FormattableString.Invariant($"{prefix}{nameof(obj.TenantId)}"), map);
                MapString(obj.ClientId, FormattableString.Invariant($"{prefix}{nameof(obj.ClientId)}"), map);
                MapUri(obj.AppIdUri, FormattableString.Invariant($"{prefix}{nameof(obj.AppIdUri)}"), separator, map);
                MapString(obj.RequiredRole, FormattableString.Invariant($"{prefix}{nameof(obj.RequiredRole)}"), map);
            }
        }

        private static void MapUri(Uri? value, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != value)
            {
                // TODO!
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(value.AbsolutePath, FormattableString.Invariant($"{prefix}{nameof(value.AbsolutePath)}"), map);
                MapString(value.AbsoluteUri, FormattableString.Invariant($"{prefix}{nameof(value.AbsoluteUri)}"), map);
                MapString(value.Authority, FormattableString.Invariant($"{prefix}{nameof(value.Authority)}"), map);
                MapString(value.DnsSafeHost, FormattableString.Invariant($"{prefix}{nameof(value.DnsSafeHost)}"), map);
                MapString(value.Fragment, FormattableString.Invariant($"{prefix}{nameof(value.Fragment)}"), map);
                MapString(value.Host, FormattableString.Invariant($"{prefix}{nameof(value.Host)}"), map);
                // MapHostNameType()
                MapString(value.IdnHost, FormattableString.Invariant($"{prefix}{nameof(value.IdnHost)}"), map);
                // MapBool(value.IsAbsoluteUri, FormattableString.Invariant($"{prefix}{nameof(value.IsAbsoluteUri)}"), map);
                // MapBool(value.IsDefaultPort, FormattableString.Invariant($"{prefix}{nameof(value.IsDefaultPort)}"), map);
                // MapBool(value.IsFile, FormattableString.Invariant($"{prefix}{nameof(value.IsFile)}"), map);
                // MapBool(value.IsLoopback, FormattableString.Invariant($"{prefix}{nameof(value.IsLoopback)}"), map);
                // MapBool(value.IsUnc, FormattableString.Invariant($"{prefix}{nameof(value.IsUnc)}"), map);
                MapString(value.LocalPath, FormattableString.Invariant($"{prefix}{nameof(value.LocalPath)}"), map);
                MapString(value.OriginalString, FormattableString.Invariant($"{prefix}{nameof(value.OriginalString)}"), map);
                MapString(value.PathAndQuery, FormattableString.Invariant($"{prefix}{nameof(value.PathAndQuery)}"), map);
                // MapInt(value.Port, FormattableString.Invariant($"{prefix}{nameof(value.Port)}"), map);
                MapString(value.Query, FormattableString.Invariant($"{prefix}{nameof(value.Query)}"), map);
                MapString(value.Scheme, FormattableString.Invariant($"{prefix}{nameof(value.Scheme)}"), map);
                // MapStringArray(value.Segments, FormattableString.Invariant($"{prefix}{nameof(value.Segments)}"), separator, map);
                // MapBool(value.UserEscaped, FormattableString.Invariant($"{prefix}{nameof(value.UserEscaped)}"), map);
                MapString(value.UserInfo, FormattableString.Invariant($"{prefix}{nameof(value.UserInfo)}"), map);
            }
        }

        private static void MapGlobalCounterOptions(GlobalCounterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapFloat(obj.IntervalSeconds, FormattableString.Invariant($"{prefix}{nameof(obj.IntervalSeconds)}"), map);
                MapInt(obj.MaxHistograms, FormattableString.Invariant($"{prefix}{nameof(obj.MaxHistograms)}"), map);
                MapInt(obj.MaxTimeSeries, FormattableString.Invariant($"{prefix}{nameof(obj.MaxTimeSeries)}"), map);
                // MapDictionary(obj.Providers, prefix, separator, map);
            }
        }

        private static void MapDiagnosticPortOptions(DiagnosticPortOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapDiagnosticPortConnectionMode(obj.ConnectionMode, FormattableString.Invariant($"{prefix}{nameof(obj.ConnectionMode)}"), map);
                MapString(obj.EndpointName, FormattableString.Invariant($"{prefix}{nameof(obj.EndpointName)}"), map);
                MapInt(obj.MaxConnections, FormattableString.Invariant($"{prefix}{nameof(obj.MaxConnections)}"), map);
                MapBool(obj.DeleteEndpointOnStartup, FormattableString.Invariant($"{prefix}{nameof(obj.DeleteEndpointOnStartup)}"), map);
            }
        }

        private static void MapDiagnosticPortConnectionMode(DiagnosticPortConnectionMode? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapEgressOptions(EgressOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapDictionary_String_FileSystemEgressProviderOptions(obj.FileSystem, FormattableString.Invariant($"{prefix}{nameof(obj.FileSystem)}"), separator, map);
                MapDictionary_String_String(obj.Properties, FormattableString.Invariant($"{prefix}{nameof(obj.Properties)}"), separator, map);
            }
        }

        private static void MapDictionary_String_FileSystemEgressProviderOptions(IDictionary<string, FileSystemEgressProviderOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                foreach ((string key, FileSystemEgressProviderOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapFileSystemEgressProviderOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private static void MapFileSystemEgressProviderOptions(FileSystemEgressProviderOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.DirectoryPath, FormattableString.Invariant($"{prefix}{nameof(obj.DirectoryPath)}"), map);
                MapString(obj.IntermediateDirectoryPath, FormattableString.Invariant($"{prefix}{nameof(obj.IntermediateDirectoryPath)}"), map);
                MapInt(obj.CopyBufferSize, FormattableString.Invariant($"{prefix}{nameof(obj.CopyBufferSize)}"), map);
            }
        }

        private static void MapStorageOptions(StorageOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.DefaultSharedPath, FormattableString.Invariant($"{prefix}{nameof(obj.DefaultSharedPath)}"), map);
                MapString(obj.DumpTempFolder, FormattableString.Invariant($"{prefix}{nameof(obj.DumpTempFolder)}"), map);
                MapString(obj.SharedLibraryPath, FormattableString.Invariant($"{prefix}{nameof(obj.SharedLibraryPath)}"), map);
            }
        }

        private static void MapCollectionRuleDefaultsOptions(CollectionRuleDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapCollectionRuleTriggerDefaultsOptions(obj.Triggers, FormattableString.Invariant($"{prefix}{nameof(obj.Triggers)}"), separator, map);
                MapCollectionRuleActionDefaultsOptions(obj.Actions, FormattableString.Invariant($"{prefix}{nameof(obj.Actions)}"), separator, map);
                MapCollectionRuleLimitsDefaultsOptions(obj.Limits, FormattableString.Invariant($"{prefix}{nameof(obj.Limits)}"), separator, map);
            }
        }

        private static void MapCollectionRuleTriggerDefaultsOptions(CollectionRuleTriggerDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapInt(obj.RequestCount, FormattableString.Invariant($"{prefix}{nameof(obj.RequestCount)}"), map);
                MapInt(obj.ResponseCount, FormattableString.Invariant($"{prefix}{nameof(obj.ResponseCount)}"), map);
                MapTimeSpan(obj.SlidingWindowDuration, FormattableString.Invariant($"{prefix}{nameof(obj.SlidingWindowDuration)}"), map);
            }
        }

        private static void MapCollectionRuleActionDefaultsOptions(CollectionRuleActionDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Egress, FormattableString.Invariant($"{prefix}{nameof(obj.Egress)}"), map);
            }
        }

        private static void MapCollectionRuleLimitsDefaultsOptions(CollectionRuleLimitsDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapInt(obj.ActionCount, FormattableString.Invariant($"{prefix}{nameof(obj.ActionCount)}"), map);
                MapTimeSpan(obj.ActionCountSlidingWindowDuration, FormattableString.Invariant($"{prefix}{nameof(obj.ActionCountSlidingWindowDuration)}"), map);
                MapTimeSpan(obj.RuleDuration, FormattableString.Invariant($"{prefix}{nameof(obj.RuleDuration)}"), map);
            }
        }

        private static void MapString(string? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapFloat(float? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapInt(int? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapBool(bool? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        // private static void MapValue(object? value, string valueName, string separator, IDictionary<string, string> map)
        // {
        //     if (null != value)
        //     {
        //         Type valueType = value.GetType();
        //         if (valueType.IsPrimitive ||
        //             valueType.IsEnum ||
        //             typeof(Guid) == valueType ||
        //             typeof(string) == valueType ||
        //             typeof(TimeSpan) == valueType)
        //         {
        //             map.Add(
        //                 valueName,
        //                 ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
        //         }
        //         else
        //         {
        //             string prefix = FormattableString.Invariant($"{valueName}{separator}");
        //             if (value is IDictionary dictionary)
        //             {
        //                 MapDictionary(dictionary, prefix, separator, map);
        //             }
        //             else if (value is IList list)
        //             {
        //                 MapList(list, prefix, separator, map);
        //             }
        //             else
        //             {
        //                 MapObject(value, prefix, separator, map);
        //             }
        //         }
        //     }
        // }

        // private static string ToHexString(byte[] data)
        // {
        //     StringBuilder builder = new(2 * data.Length);
        //     foreach (byte b in data)
        //     {
        //         builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        //     }
        //     return builder.ToString();
        // }
    }
}

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
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private Dictionary<string, Action<object, string, string, IDictionary<string, string>>>? _actionSettingsMap;

        public void AddActionSettings<TSettings>(string type, Action<TSettings, string, string, IDictionary<string, string>> mapAction)
        {
            (_actionSettingsMap ??= new()).Add(type, (obj, valueName, separator, map) =>
            {
                mapAction((TSettings)obj, valueName, separator, map);
            });
        }

        private static void MapString(string? value, string valueName, IDictionary<string, string> map)
        {
            if (value != null)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapUri(Uri? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapValue<T>(T? value, string valueName, IDictionary<string, string> map) where T : struct
        {
            if (value != null)
            {
                map.Add(valueName, ConvertUtils.ToString(value.Value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapValue<T>(T value, string valueName, IDictionary<string, string> map) where T : struct
        {
            map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
        }

        private static string BuildPropertyPath(string prefix, string name, string separator)
        {
            return FormattableString.Invariant($"{prefix}{separator}{name}");
        }

        private static string BuildPrefix(string valueName, string separator)
        {
            return FormattableString.Invariant($"{valueName}{separator}");
        }

        private void MapRootOptions(RootOptions obj, string prefix, string separator, IDictionary<string, string> map)
        {
            MapAuthenticationOptions(obj.Authentication, BuildPropertyPath(prefix, nameof(obj.Authentication), ""), separator, map);
            MapDictionary_String_CollectionRuleOptions(obj.CollectionRules, BuildPropertyPath(prefix, nameof(obj.CollectionRules), ""), separator, map);
            MapGlobalCounterOptions(obj.GlobalCounter, BuildPropertyPath(prefix, nameof(obj.GlobalCounter), ""), separator, map);
            MapInProcessFeaturesOptions(obj.InProcessFeatures, BuildPropertyPath(prefix, nameof(obj.InProcessFeatures), ""), separator, map);
            MapCorsConfigurationOptions(obj.CorsConfiguration, BuildPropertyPath(prefix, nameof(obj.CorsConfiguration), ""), separator, map);
            MapDiagnosticPortOptions(obj.DiagnosticPort, BuildPropertyPath(prefix, nameof(obj.DiagnosticPort), ""), separator, map);
            MapEgressOptions(obj.Egress, BuildPropertyPath(prefix, nameof(obj.Egress), ""), separator, map);
            MapMetricsOptions(obj.Metrics, BuildPropertyPath(prefix, nameof(obj.Metrics), ""), separator, map);
            MapStorageOptions(obj.Storage, BuildPropertyPath(prefix, nameof(obj.Storage), ""), separator, map);
            MapProcessFilterOptions(obj.DefaultProcess, BuildPropertyPath(prefix, nameof(obj.DefaultProcess), ""), separator, map);
            MapCollectionRuleDefaultsOptions(obj.CollectionRuleDefaults, BuildPropertyPath(prefix, nameof(obj.CollectionRuleDefaults), ""), separator, map);
            MapTemplateOptions(obj.Templates, BuildPropertyPath(prefix, nameof(obj.Templates), ""), separator, map);
            MapDotnetMonitorDebugOptions(obj.DotnetMonitorDebug, BuildPropertyPath(prefix, nameof(obj.DotnetMonitorDebug), ""), separator, map);
        }

        private static void MapAuthenticationOptions(AuthenticationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapMonitorApiKeyOptions(obj.MonitorApiKey, BuildPropertyPath(valueName, nameof(obj.MonitorApiKey), separator), separator, map);
                MapAzureAdOptions(obj.AzureAd, BuildPropertyPath(valueName, nameof(obj.AzureAd), separator), separator, map);
            }
        }

        private static void MapMonitorApiKeyOptions(MonitorApiKeyOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Subject, BuildPropertyPath(valueName, nameof(obj.Subject), separator), map);
                MapString(obj.PublicKey, BuildPropertyPath(valueName, nameof(obj.PublicKey), separator), map);
                MapString(obj.Issuer, BuildPropertyPath(valueName, nameof(obj.Issuer), separator), map);
            }
        }

        private static void MapAzureAdOptions(AzureAdOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapUri(obj.Instance, BuildPropertyPath(valueName, nameof(obj.Instance), separator), map);
                MapString(obj.TenantId, BuildPropertyPath(valueName, nameof(obj.TenantId), separator), map);
                MapString(obj.ClientId, BuildPropertyPath(valueName, nameof(obj.ClientId), separator), map);
                MapUri(obj.AppIdUri, BuildPropertyPath(valueName, nameof(obj.AppIdUri), separator), map);
                MapString(obj.RequiredRole, BuildPropertyPath(valueName, nameof(obj.RequiredRole), separator), map);
            }
        }

        private void MapDictionary_String_CollectionRuleOptions(IDictionary<string, CollectionRuleOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                foreach ((string key, CollectionRuleOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapCollectionRuleOptions(value, BuildPropertyPath(valueName, keyString, separator), separator, map);
                }
            }
        }

        private void MapCollectionRuleOptions(CollectionRuleOptions obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapList_ProcessFilterDescriptor(obj.Filters, BuildPropertyPath(valueName, nameof(obj.Filters), separator), separator, map);
            MapCollectionRuleTriggerOptions(obj.Trigger, BuildPropertyPath(valueName, nameof(obj.Trigger), separator), separator, map);
            MapList_CollectionRuleActionOptions(obj.Actions, BuildPropertyPath(valueName, nameof(obj.Actions), separator), separator, map);
            MapCollectionRuleLimitsOptions(obj.Limits, BuildPropertyPath(valueName, nameof(obj.Limits), separator), separator, map);
        }

        private static void MapList_ProcessFilterDescriptor(List<ProcessFilterDescriptor>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    ProcessFilterDescriptor value = obj[index];
                    MapProcessFilterDescriptor(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapProcessFilterDescriptor(ProcessFilterDescriptor obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapValue(obj.Key, BuildPropertyPath(valueName, nameof(obj.Key), separator), map);
            MapString(obj.Value, BuildPropertyPath(valueName, nameof(obj.Value), separator), map);
            MapValue(obj.MatchType, BuildPropertyPath(valueName, nameof(obj.MatchType), separator), map);
            MapString(obj.ProcessName, BuildPropertyPath(valueName, nameof(obj.ProcessName), separator), map);
            MapString(obj.ProcessId, BuildPropertyPath(valueName, nameof(obj.ProcessId), separator), map);
            MapString(obj.CommandLine, BuildPropertyPath(valueName, nameof(obj.CommandLine), separator), map);
            MapString(obj.ManagedEntryPointAssemblyName, BuildPropertyPath(valueName, nameof(obj.ManagedEntryPointAssemblyName), separator), map);
        }

        private static void MapCollectionRuleTriggerOptions(CollectionRuleTriggerOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Type, BuildPropertyPath(valueName, nameof(obj.Type), separator), map);
                MapCollectionRuleTriggerOptions_Settings(obj.Type, obj.Settings, BuildPropertyPath(valueName, nameof(obj.Settings), separator), separator, map);
            }
        }

        private static void MapCollectionRuleTriggerOptions_Settings(string type, object? settings, string valueName, string separator, IDictionary<string, string> map)
        {
            if (settings == null)
                return;

            switch (type)
            {
                case KnownCollectionRuleTriggers.AspNetRequestCount when settings is AspNetRequestCountOptions requestCountOptions:
                    MapAspNetRequestCountOptions(requestCountOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.AspNetRequestDuration when settings is AspNetRequestDurationOptions requestDurationOptions:
                    MapAspNetRequestDurationOptions(requestDurationOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.AspNetResponseStatus when settings is AspNetResponseStatusOptions responseStatusOptions:
                    MapAspNetResponseStatusOptions(responseStatusOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.EventCounter when settings is EventCounterOptions eventCounterOptions:
                    MapEventCounterOptions(eventCounterOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.CPUUsage when settings is CPUUsageOptions cpuUsageOptions:
                    MapCPUUsageOptions(cpuUsageOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.GCHeapSize when settings is GCHeapSizeOptions heapSizeOptions:
                    MapGCHeapSizeOptions(heapSizeOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.ThreadpoolQueueLength when settings is ThreadpoolQueueLengthOptions queueLengthOptions:
                    MapThreadpoolQueueLengthOptions(queueLengthOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleTriggers.EventMeter when settings is EventMeterOptions eventMeterOptions:
                    MapEventMeterOptions(eventMeterOptions, valueName, separator, map);
                    break;
                default:
                    throw new NotSupportedException($"Unknown trigger type: {type}");
            }
        }

        private static void MapAspNetRequestCountOptions(AspNetRequestCountOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.RequestCount, BuildPropertyPath(valueName, nameof(obj.RequestCount), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
                MapArray_String(obj.IncludePaths, BuildPropertyPath(valueName, nameof(obj.IncludePaths), separator), separator, map);
                MapArray_String(obj.ExcludePaths, BuildPropertyPath(valueName, nameof(obj.ExcludePaths), separator), separator, map);
            }
        }

        private static void MapArray_String(string[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Length; index++)
                {
                    string value = obj[index];
                    MapString(value, FormattableString.Invariant($"{prefix}{index}"), map);
                }
            }
        }

        private static void MapAspNetRequestDurationOptions(AspNetRequestDurationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.RequestCount, BuildPropertyPath(valueName, nameof(obj.RequestCount), separator), map);
                MapValue(obj.RequestDuration, BuildPropertyPath(valueName, nameof(obj.RequestDuration), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
                MapArray_String(obj.IncludePaths, BuildPropertyPath(valueName, nameof(obj.IncludePaths), separator), separator, map);
                MapArray_String(obj.ExcludePaths, BuildPropertyPath(valueName, nameof(obj.ExcludePaths), separator), separator, map);
            }
        }

        private static void MapAspNetResponseStatusOptions(AspNetResponseStatusOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapArray_String(obj.StatusCodes, BuildPropertyPath(valueName, nameof(obj.StatusCodes), separator), separator, map);
                MapValue(obj.ResponseCount, BuildPropertyPath(valueName, nameof(obj.ResponseCount), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
                MapArray_String(obj.IncludePaths, BuildPropertyPath(valueName, nameof(obj.IncludePaths), separator), separator, map);
                MapArray_String(obj.ExcludePaths, BuildPropertyPath(valueName, nameof(obj.ExcludePaths), separator), separator, map);
            }
        }

        private static void MapEventCounterOptions(EventCounterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.ProviderName, BuildPropertyPath(valueName, nameof(obj.ProviderName), separator), map);
                MapString(obj.CounterName, BuildPropertyPath(valueName, nameof(obj.CounterName), separator), map);
                MapValue(obj.GreaterThan, BuildPropertyPath(valueName, nameof(obj.GreaterThan), separator), map);
                MapValue(obj.LessThan, BuildPropertyPath(valueName, nameof(obj.LessThan), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
            }
        }

        private static void MapCPUUsageOptions(CPUUsageOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.GreaterThan, BuildPropertyPath(valueName, nameof(obj.GreaterThan), separator), map);
                MapValue(obj.LessThan, BuildPropertyPath(valueName, nameof(obj.LessThan), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
            }
        }

        private static void MapGCHeapSizeOptions(GCHeapSizeOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.GreaterThan, BuildPropertyPath(valueName, nameof(obj.GreaterThan), separator), map);
                MapValue(obj.LessThan, BuildPropertyPath(valueName, nameof(obj.LessThan), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
            }
        }

        private static void MapThreadpoolQueueLengthOptions(ThreadpoolQueueLengthOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.GreaterThan, BuildPropertyPath(valueName, nameof(obj.GreaterThan), separator), map);
                MapValue(obj.LessThan, BuildPropertyPath(valueName, nameof(obj.LessThan), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
            }
        }

        private static void MapEventMeterOptions(EventMeterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.MeterName, BuildPropertyPath(valueName, nameof(obj.MeterName), separator), map);
                MapString(obj.InstrumentName, BuildPropertyPath(valueName, nameof(obj.InstrumentName), separator), map);
                MapValue(obj.GreaterThan, BuildPropertyPath(valueName, nameof(obj.GreaterThan), separator), map);
                MapValue(obj.LessThan, BuildPropertyPath(valueName, nameof(obj.LessThan), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
                MapValue(obj.HistogramPercentile, BuildPropertyPath(valueName, nameof(obj.HistogramPercentile), separator), map);
            }
        }

        private static void MapCollectionRuleLimitsOptions(CollectionRuleLimitsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.ActionCount, BuildPropertyPath(valueName, nameof(obj.ActionCount), separator), map);
                MapValue(obj.ActionCountSlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.ActionCountSlidingWindowDuration), separator), map);
                MapValue(obj.RuleDuration, BuildPropertyPath(valueName, nameof(obj.RuleDuration), separator), map);
            }
        }

        private void MapList_CollectionRuleActionOptions(List<CollectionRuleActionOptions> obj, string valueName, string separator, IDictionary<string, string> map)
        {
            string prefix = BuildPrefix(valueName, separator);
            for (int index = 0; index < obj.Count; index++)
            {
                CollectionRuleActionOptions value = obj[index];
                MapCollectionRuleActionOptions(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
            }
        }

        private void MapCollectionRuleActionOptions(CollectionRuleActionOptions obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.Name, BuildPropertyPath(valueName, nameof(obj.Name), separator), map);
            MapString(obj.Type, BuildPropertyPath(valueName, nameof(obj.Type), separator), map);
            MapCollectionRuleActionOptions_Settings(obj.Type, obj.Settings, BuildPropertyPath(valueName, nameof(obj.Settings), separator), separator, map);
            MapValue(obj.WaitForCompletion, BuildPropertyPath(valueName, nameof(obj.WaitForCompletion), separator), map);
        }

        private void MapCollectionRuleActionOptions_Settings(string type, object? settings, string valueName, string separator, IDictionary<string, string> map)
        {
            if (settings == null)
                return;

            switch (type)
            {
                case KnownCollectionRuleActions.CollectDump when settings is CollectDumpOptions dumpOptions:
                    MapCollectDumpOptions(dumpOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectExceptions when settings is CollectExceptionsOptions exceptionsOptions:
                    MapCollectExceptionsOptions(exceptionsOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectGCDump when settings is CollectGCDumpOptions gcDumpOptions:
                    MapCollectGCDumpOptions(gcDumpOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectLiveMetrics when settings is CollectLiveMetricsOptions metricsOptions:
                    MapCollectLiveMetricsOptions(metricsOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectLogs when settings is CollectLogsOptions logsOptions:
                    MapCollectLogsOptions(logsOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectStacks when settings is CollectStacksOptions stacksOptions:
                    MapCollectStacksOptions(stacksOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.CollectTrace when settings is CollectTraceOptions traceOptions:
                    MapCollectTraceOptions(traceOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.Execute when settings is ExecuteOptions executeOptions:
                    MapExecuteOptions(executeOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.LoadProfiler when settings is LoadProfilerOptions profilerOptions:
                    MapLoadProfilerOptions(profilerOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.SetEnvironmentVariable when settings is SetEnvironmentVariableOptions setEnvOptions:
                    MapSetEnvironmentVariableOptions(setEnvOptions, valueName, separator, map);
                    break;
                case KnownCollectionRuleActions.GetEnvironmentVariable when settings is GetEnvironmentVariableOptions getEnvOptions:
                    MapGetEnvironmentVariableOptions(getEnvOptions, valueName, separator, map);
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

        private static void MapCollectDumpOptions(CollectDumpOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Type, BuildPropertyPath(valueName, nameof(obj.Type), separator), map);
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapCollectExceptionsOptions(CollectExceptionsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapValue(obj.Format, BuildPropertyPath(valueName, nameof(obj.Format), separator), map);
                MapExceptionsConfiguration(obj.Filters, BuildPropertyPath(valueName, nameof(obj.Filters), separator), separator, map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapExceptionsConfiguration(ExceptionsConfiguration? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapList_ExceptionFilter(obj.Include, BuildPropertyPath(valueName, nameof(obj.Include), separator), separator, map);
                MapList_ExceptionFilter(obj.Exclude, BuildPropertyPath(valueName, nameof(obj.Exclude), separator), separator, map);
            }
        }

        private static void MapList_ExceptionFilter(List<ExceptionFilter>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    ExceptionFilter value = obj[index];
                    MapExceptionFilter(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapExceptionFilter(ExceptionFilter obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.ExceptionType, BuildPropertyPath(valueName, nameof(obj.ExceptionType), separator), map);
            MapString(obj.ModuleName, BuildPropertyPath(valueName, nameof(obj.ModuleName), separator), map);
            MapString(obj.TypeName, BuildPropertyPath(valueName, nameof(obj.TypeName), separator), map);
            MapString(obj.MethodName, BuildPropertyPath(valueName, nameof(obj.MethodName), separator), map);
        }

        private static void MapCollectGCDumpOptions(CollectGCDumpOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapCollectLiveMetricsOptions(CollectLiveMetricsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.IncludeDefaultProviders, BuildPropertyPath(valueName, nameof(obj.IncludeDefaultProviders), separator), map);
                MapArray_EventMetricsProvider(obj.Providers, BuildPropertyPath(valueName, nameof(obj.Providers), separator), separator, map);
                MapArray_EventMetricsMeter(obj.Meters, BuildPropertyPath(valueName, nameof(obj.Meters), separator), separator, map);
                MapValue(obj.Duration, BuildPropertyPath(valueName, nameof(obj.Duration), separator), map);
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapArray_EventMetricsProvider(EventMetricsProvider[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Length; index++)
                {
                    EventMetricsProvider value = obj[index];
                    MapEventMetricsProvider(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventMetricsProvider(EventMetricsProvider obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.ProviderName, BuildPropertyPath(valueName, nameof(obj.ProviderName), separator), map);
            MapArray_String(obj.CounterNames, BuildPropertyPath(valueName, nameof(obj.CounterNames), separator), separator, map);
        }

        private static void MapArray_EventMetricsMeter(EventMetricsMeter[]? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Length; index++)
                {
                    EventMetricsMeter value = obj[index];
                    MapEventMetricsMeter(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventMetricsMeter(EventMetricsMeter obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.MeterName, BuildPropertyPath(valueName, nameof(obj.MeterName), separator), map);
            MapArray_String(obj.InstrumentNames, BuildPropertyPath(valueName, nameof(obj.InstrumentNames), separator), separator, map);
        }


        private static void MapCollectLogsOptions(CollectLogsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.DefaultLevel, BuildPropertyPath(valueName, nameof(obj.DefaultLevel), separator), map);
                MapDictionary_String_LogLevel(obj.FilterSpecs, BuildPropertyPath(valueName, nameof(obj.FilterSpecs), separator), separator, map);
                MapValue(obj.UseAppFilters, BuildPropertyPath(valueName, nameof(obj.UseAppFilters), separator), map);
                MapValue(obj.Duration, BuildPropertyPath(valueName, nameof(obj.Duration), separator), map);
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapValue(obj.Format, BuildPropertyPath(valueName, nameof(obj.Format), separator), map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapDictionary_String_LogLevel(IDictionary<string, LogLevel?>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, LogLevel? value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapValue(value, FormattableString.Invariant($"{prefix}{keyString}"), map);
                }
            }
        }

        private static void MapCollectStacksOptions(CollectStacksOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapValue(obj.Format, BuildPropertyPath(valueName, nameof(obj.Format), separator), map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapCollectTraceOptions(CollectTraceOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Profile, BuildPropertyPath(valueName, nameof(obj.Profile), separator), map);
                MapList_EventPipeProvider(obj.Providers, BuildPropertyPath(valueName, nameof(obj.Providers), separator), separator, map);
                MapValue(obj.RequestRundown, BuildPropertyPath(valueName, nameof(obj.RequestRundown), separator), map);
                MapValue(obj.BufferSizeMegabytes, BuildPropertyPath(valueName, nameof(obj.BufferSizeMegabytes), separator), map);
                MapValue(obj.Duration, BuildPropertyPath(valueName, nameof(obj.Duration), separator), map);
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
                MapTraceEventFilter(obj.StoppingEvent, BuildPropertyPath(valueName, nameof(obj.StoppingEvent), separator), separator, map);
                MapString(obj.ArtifactName, BuildPropertyPath(valueName, nameof(obj.ArtifactName), separator), map);
            }
        }

        private static void MapList_EventPipeProvider(List<EventPipeProvider>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    EventPipeProvider value = obj[index];
                    MapEventPipeProvider(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapEventPipeProvider(EventPipeProvider obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.Name, BuildPropertyPath(valueName, nameof(obj.Name), separator), map);
            MapString(obj.Keywords, BuildPropertyPath(valueName, nameof(obj.Keywords), separator), map);
            MapValue(obj.EventLevel, BuildPropertyPath(valueName, nameof(obj.EventLevel), separator), map);
            MapDictionary_String_String(obj.Arguments, BuildPropertyPath(valueName, nameof(obj.Arguments), separator), separator, map);
        }

        private static void MapDictionary_String_String(IDictionary<string, string>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, string value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapString(value, FormattableString.Invariant($"{prefix}{keyString}"), map);
                }
            }
        }

        private static void MapTraceEventFilter(TraceEventFilter? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.ProviderName, BuildPropertyPath(valueName, nameof(obj.ProviderName), separator), map);
                MapString(obj.EventName, BuildPropertyPath(valueName, nameof(obj.EventName), separator), map);
                MapDictionary_String_String(obj.PayloadFilter, BuildPropertyPath(valueName, nameof(obj.PayloadFilter), separator), separator, map);
            }
        }

        private static void MapExecuteOptions(ExecuteOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Path, BuildPropertyPath(valueName, nameof(obj.Path), separator), map);
                MapString(obj.Arguments, BuildPropertyPath(valueName, nameof(obj.Arguments), separator), map);
                MapValue(obj.IgnoreExitCode, BuildPropertyPath(valueName, nameof(obj.IgnoreExitCode), separator), map);
            }
        }

        private static void MapLoadProfilerOptions(LoadProfilerOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Path, BuildPropertyPath(valueName, nameof(obj.Path), separator), map);
                MapValue(obj.Clsid, BuildPropertyPath(valueName, nameof(obj.Clsid), separator), map);
            }
        }

        private static void MapSetEnvironmentVariableOptions(SetEnvironmentVariableOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Name, BuildPropertyPath(valueName, nameof(obj.Name), separator), map);
                MapString(obj.Value, BuildPropertyPath(valueName, nameof(obj.Value), separator), map);
            }
        }

        private static void MapGetEnvironmentVariableOptions(GetEnvironmentVariableOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Name, BuildPropertyPath(valueName, nameof(obj.Name), separator), map);
            }
        }

        private static void MapGlobalCounterOptions(GlobalCounterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.IntervalSeconds, BuildPropertyPath(valueName, nameof(obj.IntervalSeconds), separator), map);
                MapValue(obj.MaxHistograms, BuildPropertyPath(valueName, nameof(obj.MaxHistograms), separator), map);
                MapValue(obj.MaxTimeSeries, BuildPropertyPath(valueName, nameof(obj.MaxTimeSeries), separator), map);
                MapDictionary_String_GlobalProviderOptions(obj.Providers, BuildPropertyPath(valueName, nameof(obj.Providers), separator), separator, map);
            }
        }

        private static void MapDictionary_String_GlobalProviderOptions(IDictionary<string, GlobalProviderOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, GlobalProviderOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapGlobalProviderOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private static void MapGlobalProviderOptions(GlobalProviderOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.IntervalSeconds, BuildPropertyPath(valueName, nameof(obj.IntervalSeconds), separator), map);
            }
        }

        private static void MapInProcessFeaturesOptions(InProcessFeaturesOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Enabled, BuildPropertyPath(valueName, nameof(obj.Enabled), separator), map);
                MapCallStacksOptions(obj.CallStacks, BuildPropertyPath(valueName, nameof(obj.CallStacks), separator), separator, map);
                MapExceptionsOptions(obj.Exceptions, BuildPropertyPath(valueName, nameof(obj.Exceptions), separator), separator, map);
                MapParameterCapturingOptions(obj.ParameterCapturing, BuildPropertyPath(valueName, nameof(obj.ParameterCapturing), separator), separator, map);
            }
        }

        private static void MapCallStacksOptions(CallStacksOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Enabled, BuildPropertyPath(valueName, nameof(obj.Enabled), separator), map);
            }
        }

        private static void MapExceptionsOptions(ExceptionsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Enabled, BuildPropertyPath(valueName, nameof(obj.Enabled), separator), map);
                MapValue(obj.TopLevelLimit, BuildPropertyPath(valueName, nameof(obj.TopLevelLimit), separator), map);
                MapExceptionsConfiguration(obj.CollectionFilters, BuildPropertyPath(valueName, nameof(obj.CollectionFilters), separator), separator, map);
                MapValue(obj.CollectOnStartup, BuildPropertyPath(valueName, nameof(obj.CollectOnStartup), separator), map);
            }
        }

        private static void MapCorsConfigurationOptions(CorsConfigurationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.AllowedOrigins, BuildPropertyPath(valueName, nameof(obj.AllowedOrigins), separator), map);
            }
        }

        private static void MapParameterCapturingOptions(ParameterCapturingOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Enabled, BuildPropertyPath(valueName, nameof(obj.Enabled), separator), map);
            }
        }

        private static void MapDiagnosticPortOptions(DiagnosticPortOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.ConnectionMode, BuildPropertyPath(valueName, nameof(obj.ConnectionMode), separator), map);
                MapString(obj.EndpointName, BuildPropertyPath(valueName, nameof(obj.EndpointName), separator), map);
                MapValue(obj.MaxConnections, BuildPropertyPath(valueName, nameof(obj.MaxConnections), separator), map);
                MapValue(obj.DeleteEndpointOnStartup, BuildPropertyPath(valueName, nameof(obj.DeleteEndpointOnStartup), separator), map);
            }
        }

        private static void MapEgressOptions(EgressOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapDictionary_String_FileSystemEgressProviderOptions(obj.FileSystem, BuildPropertyPath(valueName, nameof(obj.FileSystem), separator), separator, map);
                MapDictionary_String_String(obj.Properties, BuildPropertyPath(valueName, nameof(obj.Properties), separator), separator, map);
            }
        }


        private static void MapDictionary_String_FileSystemEgressProviderOptions(IDictionary<string, FileSystemEgressProviderOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
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
                MapString(obj.DirectoryPath, BuildPropertyPath(valueName, nameof(obj.DirectoryPath), separator), map);
                MapString(obj.IntermediateDirectoryPath, BuildPropertyPath(valueName, nameof(obj.IntermediateDirectoryPath), separator), map);
                MapValue(obj.CopyBufferSize, BuildPropertyPath(valueName, nameof(obj.CopyBufferSize), separator), map);
            }
        }

        private static void MapMetricsOptions(MetricsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.Enabled, BuildPropertyPath(valueName, nameof(obj.Enabled), separator), map);
                MapString(obj.Endpoints, BuildPropertyPath(valueName, nameof(obj.Endpoints), separator), map);
                MapValue(obj.MetricCount, BuildPropertyPath(valueName, nameof(obj.MetricCount), separator), map);
                MapValue(obj.IncludeDefaultProviders, BuildPropertyPath(valueName, nameof(obj.IncludeDefaultProviders), separator), map);
                MapList_MetricProvider(obj.Providers, BuildPropertyPath(valueName, nameof(obj.Providers), separator), separator, map);
                MapList_MeterConfiguration(obj.Meters, BuildPropertyPath(valueName, nameof(obj.Meters), separator), separator, map);
            }
        }

        private static void MapList_MetricProvider(List<MetricProvider>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    MetricProvider value = obj[index];
                    MapMetricProvider(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapMetricProvider(MetricProvider obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.ProviderName, BuildPropertyPath(valueName, nameof(obj.ProviderName), separator), map);
            MapList_String(obj.CounterNames, BuildPropertyPath(valueName, nameof(obj.CounterNames), separator), separator, map);         
        }

        private static void MapList_String(List<string>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    string value = obj[index];
                    MapString(value, FormattableString.Invariant($"{prefix}{index}"), map);
                }
            }
        }

        private static void MapList_MeterConfiguration(List<MeterConfiguration>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                for (int index = 0; index < obj.Count; index++)
                {
                    MeterConfiguration value = obj[index];
                    MapMeterConfiguration(value, FormattableString.Invariant($"{prefix}{index}"), separator, map);
                }
            }
        }

        private static void MapMeterConfiguration(MeterConfiguration obj, string valueName, string separator, IDictionary<string, string> map)
        {
            MapString(obj.MeterName, BuildPropertyPath(valueName, nameof(obj.MeterName), separator), map);
            MapList_String(obj.InstrumentNames, BuildPropertyPath(valueName, nameof(obj.InstrumentNames), separator), separator, map);
        }

        private static void MapStorageOptions(StorageOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.DefaultSharedPath, BuildPropertyPath(valueName, nameof(obj.DefaultSharedPath), separator), map);
                MapString(obj.DumpTempFolder, BuildPropertyPath(valueName, nameof(obj.DumpTempFolder), separator), map);
                MapString(obj.SharedLibraryPath, BuildPropertyPath(valueName, nameof(obj.SharedLibraryPath), separator), map);
            }
        }

        private static void MapProcessFilterOptions(ProcessFilterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapList_ProcessFilterDescriptor(obj.Filters, BuildPropertyPath(valueName, nameof(obj.Filters), separator), separator, map);
            }
        }

        private static void MapCollectionRuleDefaultsOptions(CollectionRuleDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapCollectionRuleTriggerDefaultsOptions(obj.Triggers, BuildPropertyPath(valueName, nameof(obj.Triggers), separator), separator, map);
                MapCollectionRuleActionDefaultsOptions(obj.Actions, BuildPropertyPath(valueName, nameof(obj.Actions), separator), separator, map);
                MapCollectionRuleLimitsDefaultsOptions(obj.Limits, BuildPropertyPath(valueName, nameof(obj.Limits), separator), separator, map);
            }
        }

        private static void MapCollectionRuleTriggerDefaultsOptions(CollectionRuleTriggerDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.RequestCount, BuildPropertyPath(valueName, nameof(obj.RequestCount), separator), map);
                MapValue(obj.ResponseCount, BuildPropertyPath(valueName, nameof(obj.ResponseCount), separator), map);
                MapValue(obj.SlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.SlidingWindowDuration), separator), map);
            }
        }

        private static void MapCollectionRuleActionDefaultsOptions(CollectionRuleActionDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapString(obj.Egress, BuildPropertyPath(valueName, nameof(obj.Egress), separator), map);
            }
        }

        private static void MapCollectionRuleLimitsDefaultsOptions(CollectionRuleLimitsDefaultsOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.ActionCount, BuildPropertyPath(valueName, nameof(obj.ActionCount), separator), map);
                MapValue(obj.ActionCountSlidingWindowDuration, BuildPropertyPath(valueName, nameof(obj.ActionCountSlidingWindowDuration), separator), map);
                MapValue(obj.RuleDuration, BuildPropertyPath(valueName, nameof(obj.RuleDuration), separator), map);
            }
        }

        private void MapTemplateOptions(TemplateOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapDictionary_String_ProcessFilterDescriptor(obj.CollectionRuleFilters, BuildPropertyPath(valueName, nameof(obj.CollectionRuleFilters), separator), separator, map);
                MapDictionary_String_CollectionRuleTriggerOptions(obj.CollectionRuleTriggers, BuildPropertyPath(valueName, nameof(obj.CollectionRuleTriggers), separator), separator, map);
                MapDictionary_String_CollectionRuleActionOptions(obj.CollectionRuleActions, BuildPropertyPath(valueName, nameof(obj.CollectionRuleActions), separator), separator, map);
                MapDictionary_String_CollectionRuleLimitsOptions(obj.CollectionRuleLimits, BuildPropertyPath(valueName, nameof(obj.CollectionRuleLimits), separator), separator, map);
            }
        }

        private static void MapDictionary_String_ProcessFilterDescriptor(IDictionary<string, ProcessFilterDescriptor>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, ProcessFilterDescriptor value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapProcessFilterDescriptor(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private static void MapDictionary_String_CollectionRuleTriggerOptions(IDictionary<string, CollectionRuleTriggerOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, CollectionRuleTriggerOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapCollectionRuleTriggerOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private void MapDictionary_String_CollectionRuleActionOptions(IDictionary<string, CollectionRuleActionOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, CollectionRuleActionOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapCollectionRuleActionOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private static void MapDictionary_String_CollectionRuleLimitsOptions(IDictionary<string, CollectionRuleLimitsOptions>? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = BuildPrefix(valueName, separator);
                foreach ((string key, CollectionRuleLimitsOptions value) in obj)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
                    MapCollectionRuleLimitsOptions(value, FormattableString.Invariant($"{prefix}{keyString}"), separator, map);
                }
            }
        }

        private static void MapDotnetMonitorDebugOptions(DotnetMonitorDebugOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapExceptionsDebugOptions(obj.Exceptions, BuildPropertyPath(valueName, nameof(obj.Exceptions), separator), separator, map);
            }
        }

        private static void MapExceptionsDebugOptions(ExceptionsDebugOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                MapValue(obj.IncludeMonitorExceptions, BuildPropertyPath(valueName, nameof(obj.IncludeMonitorExceptions), separator), map);
            }
        }
    }
}

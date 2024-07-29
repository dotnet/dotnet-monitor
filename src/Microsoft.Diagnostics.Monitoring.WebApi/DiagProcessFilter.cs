// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// There are effectively 3 sets of models for this:
    /// The configuration model (ProcessFilterOptions), which does not contain criteria such as RuntimeId, and is always string based.
    /// The REST call model (ProcessKey), which does not contain criteria such as Command Line, and is strongly typed.
    /// This internal model, which contains all possible criteria and is string based. Only this model is used for the actual comparison.
    /// </summary>
    internal sealed class DiagProcessFilter
    {
        public IList<DiagProcessFilterEntry> Filters { get; set; } = new List<DiagProcessFilterEntry>();

        public static DiagProcessFilter FromProcessKey(ProcessKey processKey)
        {
            var filter = new DiagProcessFilter();
            List<DiagProcessFilterEntry> filterEntries = TransformKey(processKey);

            for (int index = 0; index < filterEntries.Count; ++index)
            {
                filter.Filters.Add(filterEntries[index]);
            }

            return filter;
        }

        public static DiagProcessFilter FromConfiguration(ProcessFilterOptions options)
        {
            return FromConfiguration(options.Filters);
        }

        public static DiagProcessFilter FromConfiguration(IEnumerable<ProcessFilterDescriptor>? filters)
        {
            var filter = new DiagProcessFilter();
            if (filters != null)
            {
                foreach (ProcessFilterDescriptor processFilter in filters)
                {
                    filter.Filters.Add(TransformDescriptor(processFilter));
                }
            }

            return filter;
        }

        private static List<DiagProcessFilterEntry> TransformKey(ProcessKey processKey)
        {
            List<DiagProcessFilterEntry> filterEntries = new List<DiagProcessFilterEntry>();

            if (processKey.ProcessId.HasValue)
            {
                filterEntries.Add(new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessId, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.ProcessId.Value.ToString(CultureInfo.InvariantCulture) });
            }
            if (processKey.ProcessName != null)
            {
                filterEntries.Add(new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessName, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.ProcessName });
            }
            if (processKey.RuntimeInstanceCookie.HasValue)
            {
                filterEntries.Add(new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.RuntimeId, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.RuntimeInstanceCookie.Value.ToString("D") });
            }

            if (filterEntries.Count > 0)
            {
                return filterEntries;
            }

            throw new ArgumentException($"Invalid {nameof(processKey)}");
        }

        private static DiagProcessFilterEntry TransformDescriptor(ProcessFilterDescriptor processFilterDescriptor)
        {
            if (!string.IsNullOrWhiteSpace(processFilterDescriptor.ProcessId))
            {
                return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessId, MatchType = DiagProcessFilterMatchType.Exact, Value = processFilterDescriptor.ProcessId };
            }
            else if (!string.IsNullOrWhiteSpace(processFilterDescriptor.ProcessName))
            {
                return new DiagProcessFilterEntry
                {
                    Criteria = DiagProcessFilterCriteria.ProcessName,
                    MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                    Value = processFilterDescriptor.ProcessName
                };
            }
            else if (!string.IsNullOrWhiteSpace(processFilterDescriptor.CommandLine))
            {
                return new DiagProcessFilterEntry
                {
                    Criteria = DiagProcessFilterCriteria.CommandLine,
                    MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                    Value = processFilterDescriptor.CommandLine
                };
            }

            string filterValue = processFilterDescriptor.Value!; // Guaranteed not to be null by ProcessFilterDescriptor.Validate.
            switch (processFilterDescriptor.Key)
            {
                case ProcessFilterKey.ProcessId:
                    return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessId, MatchType = DiagProcessFilterMatchType.Exact, Value = filterValue };
                case ProcessFilterKey.ProcessName:
                    return new DiagProcessFilterEntry
                    {
                        Criteria = DiagProcessFilterCriteria.ProcessName,
                        MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                        Value = filterValue
                    };
                case ProcessFilterKey.CommandLine:
                    return new DiagProcessFilterEntry
                    {
                        Criteria = DiagProcessFilterCriteria.CommandLine,
                        MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                        Value = filterValue
                    };
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_UnexpectedType,
                            nameof(ProcessFilterDescriptor),
                            processFilterDescriptor.Key),
                        nameof(processFilterDescriptor));
            }
        }
    }

    internal sealed class DiagProcessFilterEntry
    {
        public DiagProcessFilterCriteria Criteria { get; set; }

        public string Value { get; set; } = string.Empty;

        public DiagProcessFilterMatchType MatchType { get; set; }

        public bool MatchFilter(IProcessInfo processInfo)
        {
            switch (this.Criteria)
            {
                case DiagProcessFilterCriteria.ProcessId:
                    return ExactCompare(processInfo.EndpointInfo.ProcessId.ToString(CultureInfo.InvariantCulture));
                case DiagProcessFilterCriteria.RuntimeId:
                    return ExactCompare(processInfo.EndpointInfo.RuntimeInstanceCookie.ToString("D"));
                case DiagProcessFilterCriteria.CommandLine:
                    return Compare(processInfo.CommandLine);
                case DiagProcessFilterCriteria.ProcessName:
                    return Compare(processInfo.ProcessName);
                default:
                    Debug.Fail($"Unexpected {nameof(DiagProcessFilterCriteria)}: {this.Criteria}");
                    break;
            }

            return false;
        }

        private bool Compare(string? value)
        {
            if (MatchType == DiagProcessFilterMatchType.Exact)
            {
                return ExactCompare(value);
            }
            if (MatchType == DiagProcessFilterMatchType.Contains)
            {
                return ContainsCompare(value);
            }
            Debug.Fail($"Unexpected {nameof(DiagProcessFilterMatchType)}: {MatchType}");

            return false;
        }

        private bool ExactCompare(string? value)
        {
            return string.Equals(Value, value, StringComparison.OrdinalIgnoreCase);
        }

        private bool ContainsCompare(string? value)
        {
            return value?.IndexOf(Value, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }

    internal enum DiagProcessFilterCriteria
    {
        ProcessId,
        RuntimeId,
        CommandLine,
        ProcessName
    }

    internal enum DiagProcessFilterMatchType
    {
        Exact,
        Contains,
    }
}

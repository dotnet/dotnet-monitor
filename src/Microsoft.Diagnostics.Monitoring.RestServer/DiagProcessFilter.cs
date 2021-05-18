// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.RestServer
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
            filter.Filters.Add(TransformKey(processKey));
            return filter;
        }

        public static DiagProcessFilter FromConfiguration(ProcessFilterOptions options)
        {
            var filter = new DiagProcessFilter();
            foreach(ProcessFilterDescriptor processFilter in options.Filters)
            {
                filter.Filters.Add(TransformDescriptor(processFilter));
            }
            return filter;
        }

        private static DiagProcessFilterEntry TransformKey(ProcessKey processKey)
        {
            if (processKey.ProcessId.HasValue)
            {
                return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessId, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.ProcessId.Value.ToString(CultureInfo.InvariantCulture) };
            }
            if (processKey.ProcessName != null)
            {
                return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessName, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.ProcessName };
            }
            if (processKey.RuntimeInstanceCookie.HasValue)
            {
                return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.RuntimeId, MatchType = DiagProcessFilterMatchType.Exact, Value = processKey.RuntimeInstanceCookie.Value.ToString("D") };
            }

            throw new ArgumentException($"Invalid {nameof(processKey)}");
        }

        private static DiagProcessFilterEntry TransformDescriptor(ProcessFilterDescriptor processFilterDescriptor)
        {
            switch (processFilterDescriptor.Key)
            {
                case ProcessFilterKey.ProcessId:
                    return new DiagProcessFilterEntry { Criteria = DiagProcessFilterCriteria.ProcessId, MatchType = DiagProcessFilterMatchType.Exact, Value = processFilterDescriptor.Value };
                case ProcessFilterKey.ProcessName:
                    return new DiagProcessFilterEntry
                    {
                        Criteria = DiagProcessFilterCriteria.ProcessName,
                        MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                        Value = processFilterDescriptor.Value
                    };
                case ProcessFilterKey.CommandLine:
                    return new DiagProcessFilterEntry
                    {
                        Criteria = DiagProcessFilterCriteria.ProcessName,
                        MatchType = (processFilterDescriptor.MatchType == ProcessFilterType.Exact) ? DiagProcessFilterMatchType.Exact : DiagProcessFilterMatchType.Contains,
                        Value = processFilterDescriptor.Value
                    };
                default:
                    throw new ArgumentException($"Invalid {nameof(processFilterDescriptor)}");
            }
        }
    }

    internal sealed class DiagProcessFilterEntry
    {
        public DiagProcessFilterCriteria Criteria { get; set; }

        public string Value { get; set; }

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
                    Debug.Fail("Unexpected filter criteria");
                    break;

            }

            return false;
        }

        private bool Compare(string value)
        {
            if (MatchType == DiagProcessFilterMatchType.Exact)
            {
                return ExactCompare(value);
            }
            if (MatchType == DiagProcessFilterMatchType.Contains)
            {
                return LooseCompare(value);
            }
            Debug.Fail("Unexpected match type");

            return false;
        }

        private bool ExactCompare(string value)
        {
            return string.Equals(Value, value, StringComparison.OrdinalIgnoreCase);
        }

        private bool LooseCompare(string value)
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

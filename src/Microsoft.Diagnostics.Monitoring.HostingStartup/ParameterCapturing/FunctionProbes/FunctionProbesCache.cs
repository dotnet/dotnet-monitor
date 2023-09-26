// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using System.Collections.ObjectModel;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class FunctionProbesCache
    {
        public ReadOnlyDictionary<ulong, InstrumentedMethod> InstrumentedMethods { get; }
        public ObjectFormatterCache ObjectFormatterCache { get; }

        public FunctionProbesCache(ReadOnlyDictionary<ulong, InstrumentedMethod> instrumentedMethods, ObjectFormatterCache objectFormatterCache)
        {
            InstrumentedMethods = instrumentedMethods;
            ObjectFormatterCache = objectFormatterCache;
        }
    }
}

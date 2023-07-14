// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface IInProcessFeatures
    {
        /* Requirements */
        bool IsProfilerRequired { get; }

        bool IsStartupHookRequired { get; }

        bool IsHostingStartupRequired { get; }

        bool IsLibrarySharingRequired { get; }

        /* Features */
        public bool IsCallStacksEnabled { get; }

        public bool IsExceptionsEnabled { get; }

        public bool IsParameterCapturingEnabled { get; }
    }
}

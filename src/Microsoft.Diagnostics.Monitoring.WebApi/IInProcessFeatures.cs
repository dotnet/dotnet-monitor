// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface IInProcessFeatures
    {
        bool IsProfilerRequired { get; }

        bool IsMutatingProfilerRequired { get; }

        bool IsStartupHookRequired { get; }

        bool IsLibrarySharingRequired { get; }
    }
}

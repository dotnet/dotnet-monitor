﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface IInProcessFeatures
    {
        bool IsCallStacksEnabled { get; }

        bool IsExceptionsEnabled { get; }

        bool IsParameterCapturingEnabled { get; }

        bool IsProfilerRequired { get; }

        bool IsStartupHookRequired { get; }

        bool IsHostingStartupRequired { get; }

        bool IsLibrarySharingRequired { get; }
    }
}

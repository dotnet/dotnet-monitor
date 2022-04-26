﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public enum CollectionRulesMicroState
    {
        Running,
        ActionStarted,
        ActionFailed,
        ActionSucceeded,
        Throttled,
        FinishedViaConfigChange,
        FinishedViaRuleDuration,
        FinishedViaStartup,
        FinishedViaFailure,
        FinishedViaActionCount
    }
}

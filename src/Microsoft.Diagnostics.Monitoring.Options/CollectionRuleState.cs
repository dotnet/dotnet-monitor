// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
#if NET8_0_OR_GREATER
    [JsonConverter(typeof(JsonStringEnumConverter<CollectionRuleState>))]
#endif
    public enum CollectionRuleState
    {
        Running,
        ActionExecuting,
        Throttled,
        Finished
    }
}

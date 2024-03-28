// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal sealed record ParameterInfo(string Name, string Type, string TypeModuleName, string Value, bool IsIn, bool IsOut, bool IsByRef)
    {
    }
}

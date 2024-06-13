// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal sealed record ParameterSignature(string? Name, string? Type, string? TypeModuleName, ParameterAttributes Attributes, bool IsByRef)
    {
    }
}

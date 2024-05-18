﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed record ResolvedParameterInfo(string? Name, string? Type, string? TypeModuleName, ObjectFormatterResult Value, ParameterAttributes Attributes, bool IsByRef)
    {
    }
}

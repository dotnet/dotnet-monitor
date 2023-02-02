// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.Loader;

#pragma warning disable CA1050 // Declare types in namespaces
public sealed class StartupHook
#pragma warning restore CA1050 // Declare types in namespaces
{
    private const string TestHostingStartupAssemblyName = "Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup";

    public static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += AssemblyLoadContext_Resolving;
    }

    private static Assembly AssemblyLoadContext_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (TestHostingStartupAssemblyName.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
        {
            string path = Assembly.GetExecutingAssembly().Location.Replace(
                Assembly.GetExecutingAssembly().GetName().Name,
                TestHostingStartupAssemblyName);

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
        return null;
    }
}

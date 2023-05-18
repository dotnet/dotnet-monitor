// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using System;
using System.IO;

internal sealed class StartupHook
{
    private static CurrentAppDomainExceptionProcessor s_exceptionProcessor = new();
    private static AspNetHostingStartupLoader? s_hostingStartupLoader;

    public static void Initialize()
    {
        try
        {
            string? hostingStartupPath = Environment.GetEnvironmentVariable(StartupHookIdentifiers.EnvironmentVariables.HostingStartupPath);
            // TODO: Log if specified hosting startup assembly doesn't exist
            if (File.Exists(hostingStartupPath))
            {
                s_hostingStartupLoader = new AspNetHostingStartupLoader(hostingStartupPath);
            }

            s_exceptionProcessor.Start();
        }
        catch
        {
            // TODO: Log failure
        }
    }
}

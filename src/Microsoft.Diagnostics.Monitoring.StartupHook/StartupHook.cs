// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using System;

internal sealed class StartupHook
{
    private static DiagnosticsBootstrapper? s_bootstrapper;
    private static object s_lock = new object();

    public static void Initialize()
    {
        // Ensure that only one bootstrapper is created for the application domain,
        // regardless of multiple initializations or multiple threads.
        if (null == s_bootstrapper)
        {
            lock (s_lock)
            {
                if (s_bootstrapper == null)
                {
                    try
                    {
                        s_bootstrapper = new DiagnosticsBootstrapper();

                        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    }
                    catch
                    {
                        // TODO: Log failure
                    }
                }
            }
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        try
        {
            s_bootstrapper?.Dispose();
        }
        catch
        {
            // TODO: Log failure
        }
    }
}

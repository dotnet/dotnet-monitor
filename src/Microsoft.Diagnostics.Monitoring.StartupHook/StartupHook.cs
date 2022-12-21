// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;

internal sealed class StartupHook
{
    private static CurrentAppDomainExceptionProcessor s_exceptionProcessor = new();

    public static void Initialize()
    {
        try
        {
            s_exceptionProcessor.Start();
        }
        catch
        {
            // TODO: Log failure
        }
    }
}

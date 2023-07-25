// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable CA1050 // Declare types in namespaces
public sealed class StartupHook
#pragma warning restore CA1050 // Declare types in namespaces
{
    public static void Initialize()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                int parentPid = int.Parse(Environment.GetEnvironmentVariable(TestProcessCleanupIdentifiers.EnvironmentVariables.ParentPid));
                using Process parentProcess = Process.GetProcessById(parentPid);
                await parentProcess.WaitForExitAsync().ConfigureAwait(false);
                Console.WriteLine("Parent process exited, stopping.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while waiting for parent process to exit, stopping: {ex}");
            }

            Environment.Exit(1);
        });
    }
}

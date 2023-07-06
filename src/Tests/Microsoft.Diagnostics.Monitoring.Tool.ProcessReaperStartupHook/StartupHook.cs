// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable CA1050 // Declare types in namespaces
public sealed class StartupHook
#pragma warning restore CA1050 // Declare types in namespaces
{
    public static void Initialize()
    {
        string parentPid = Environment.GetEnvironmentVariable(ProcessReaperIdentifiers.EnvironmentVariables.ParentPid);
        int pid = int.Parse(parentPid);
        Console.WriteLine($"Waiting for {pid} to exit");
        using Process parentProcess = Process.GetProcessById(pid);
        Task.Run(async () =>
        {
            await parentProcess.WaitForExitAsync().ConfigureAwait(false);
            Console.WriteLine("Parent process exited, stopping.");
            Environment.Exit(1);
        });
    }
}

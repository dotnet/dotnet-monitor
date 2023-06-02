// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class LocalDev
    {
        private readonly TaskCompletionSource<object?> _doInject = new();

        public LocalDev()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            _ = _doInject.TrySetResult(null);
        }

        private static MethodInfo? ResolveMethod(string dll, string className, string methodName)
        {
            Module? userMod = null;
            Assembly? userAssembly = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var mod in assembly.Modules)
                {
                    if (mod.Name == dll)
                    {
                        userAssembly = assembly;
                        userMod = mod;
                        break;
                    }
                }
            }
            if (userMod == null || userAssembly == null)
            {
                Console.WriteLine("COULD NOT RESOLVE REMOTE MODULE");
                return null;
            }

            Type? remoteClass = userAssembly.GetType(className);
            if (remoteClass == null)
            {
                foreach (var c in userAssembly.GetTypes())
                {
                    Console.WriteLine(c.AssemblyQualifiedName);
                }
                Console.WriteLine("COULD NOT RESOLVE REMOTE CLASS");
                return null;
            }

            MethodInfo? methodInfo = remoteClass.GetMethod(methodName);
            if (methodInfo == null)
            {
                foreach (var c in remoteClass.GetMethods())
                {
                    Console.WriteLine(c.Name);
                }
                Console.WriteLine("COULD NOT RESOLVE REMOTE METHOD");
                return null;
            }

            return methodInfo;
        }

        public async Task RunDemoScenario(ParameterCapturingService capturingService, CancellationToken stoppingToken)
        {

            await _doInject.Task.WaitAsync(stoppingToken).ConfigureAwait(false);

            MethodInfo? resolvedMethod = ResolveMethod("Mvc.dll", "Benchmarks.Controllers.MyController`1", "JsonNk");
            MethodInfo? resolvedMethod2 = ResolveMethod("Mvc.dll", "Benchmarks.Controllers.MyStruct", "DoSomething");
            MethodInfo? resolvedMethod3 = ResolveMethod("Mvc.dll", "Benchmarks.Controllers.MyAwesomeClass", "DoThis");

            if (resolvedMethod == null || resolvedMethod2 == null || resolvedMethod3 == null)
            {
                return;
            }

            capturingService.StartCapturing(new List<MethodInfo> { resolvedMethod, resolvedMethod2, resolvedMethod3 });

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        }
    }
}

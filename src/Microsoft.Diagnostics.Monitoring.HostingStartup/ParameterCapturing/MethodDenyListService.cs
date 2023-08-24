// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal class MethodDenyListService : IMethodDenyListService
    {
        private static readonly string[] s_typeDenyList = {
            "Interop",
            "Internal",
            "Microsoft.Diagnostics.Monitoring",
            "System.Diagnostics.Debugger"
        };

        public void ValidateMethods(IEnumerable<MethodDescription> methods)
        {
            List<MethodDescription> _deniedMethodDescriptions = new();
            foreach (MethodDescription methodDescription in methods)
            {
                foreach (string deniedType in s_typeDenyList)
                {
                    if (TypeUtils.IsSubType(deniedType, methodDescription.TypeName))
                    {
                        _deniedMethodDescriptions.Add(methodDescription);
                        break;
                    }
                }
            }

            if (_deniedMethodDescriptions.Count > 0)
            {
                throw new DeniedMethodsExceptions(_deniedMethodDescriptions);
            }
        }
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal class MethodDescriptionValidator : IMethodDescriptionValidator
    {
        private static readonly string[] s_typeDenyList = {
            "Interop",
            "Internal",
            "Microsoft.Diagnostics.Monitoring",
            "System.Diagnostics.Debugger"
        };

        public bool IsMethodDescriptionAllowed(MethodDescription methodDescription)
        {
            foreach (string deniedType in s_typeDenyList)
            {
                if (TypeUtils.IsSubType(deniedType, methodDescription.TypeName))
                {
                    return false;
                }
            }

            return true;
        }
    }

}

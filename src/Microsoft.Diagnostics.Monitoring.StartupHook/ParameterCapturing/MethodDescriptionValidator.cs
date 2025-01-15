// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal class MethodDescriptionValidator : IMethodDescriptionValidator
    {
        // This list represents partial type names that aren't allowed.
        // Any method description with a type name that belongs to any of these
        // will be rejected (e.g. the exact same type, or a sub-type that
        // starts the same and followed by a valid type separator ('.' or '+').
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

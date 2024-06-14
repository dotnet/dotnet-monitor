// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
{
    internal interface IFunctionProbes
    {
        public void CacheMethods(IList<MethodInfo> methods);

        /// <summary>
        /// </summary>
        /// <param name="uniquifier">The uniquifier which identifies the method calling the probe.</param>
        /// <param name="args">The arguments passed into the method.</param>
        /// <returns>True if the the arguments were captured by the probe.</returns>
        public bool EnterProbe(ulong uniquifier, object[] args);
    }
}

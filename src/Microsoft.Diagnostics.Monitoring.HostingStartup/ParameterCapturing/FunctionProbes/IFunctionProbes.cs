// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal interface IFunctionProbes
    {
        public void Cache(IList<MethodInfo> methods);

        public void EnterProbe(ulong uniquifier, object[] args);
    }
}

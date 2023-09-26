// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal interface IWellKnownExtensionFactory
    {
        IEgressExtension Create();

        string Name { get; }
    }
}

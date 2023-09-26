// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class NotSupportedServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotSupportedException("Pre-process services are not supported.");
        }
    }
}

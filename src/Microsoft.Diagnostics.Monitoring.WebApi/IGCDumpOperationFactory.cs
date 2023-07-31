// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IGCDumpOperationFactory
    {
        IArtifactOperation Create(IEndpointInfo endpointInfo);
    }
}

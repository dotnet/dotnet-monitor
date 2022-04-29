// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal interface IEgressExtension : IExtension
    {
        Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Stream payload);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    /// <summary>
    /// Indicates packaging options for artifacts.
    /// The lower 2 bytes indicate the type of packaging, while the upper 2 bytes can be used to add
    /// additional payloads.
    /// </summary>
    [Flags]
    public enum PackageMode
    {
        None = 0x0,
        DiagSession =   0x00000001, //Packages the dump into a diagsession file with no compression

        //CONSIDER Other future possibilities:
        //Include application pdb file
        //Compress dump.

        //Note some of these may require that dotnet-monitor is running in the same container as the app itself.
    }
}

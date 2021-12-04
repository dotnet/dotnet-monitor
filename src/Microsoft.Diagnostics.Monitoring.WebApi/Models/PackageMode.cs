// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    [Flags]
    public enum PackageMode
    {
        None = 0x0,
        DiagSession =   0x00000001, //Packages the dump into a diagsession file with no compression
        IncludeDacDbi = DiagSession | 0x00010000, //Packages libmscordaccore.so and libmscordbi.so along with the dump.

        //CONSIDER Other future possibilities:
        //Include application pdb file
        //Compress dump.
    }
}

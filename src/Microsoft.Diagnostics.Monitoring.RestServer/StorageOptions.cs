// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer
#endif
{
    internal class StorageOptions
    {
        public string DumpTempFolder {get; set; }
    }
}

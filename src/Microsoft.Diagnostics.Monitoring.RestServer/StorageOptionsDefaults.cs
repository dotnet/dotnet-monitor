using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    internal sealed class StorageOptionsDefaults
    {
        public static readonly string DumpTempFolder = Path.GetTempPath();
    }
}

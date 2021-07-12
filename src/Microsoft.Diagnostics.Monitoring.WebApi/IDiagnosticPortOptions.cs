using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface IDiagnosticPortOptions
    {
        public string GetReadableConnectionMode();
    }
}

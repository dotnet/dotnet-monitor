using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor.Options
{
    interface IDynamicOptionsSource<TOptions>
    {
        TOptions Get(string name);
    }
}

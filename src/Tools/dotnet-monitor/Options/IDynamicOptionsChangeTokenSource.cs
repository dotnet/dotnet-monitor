using Microsoft.Extensions.Primitives;

namespace Microsoft.Diagnostics.Tools.Monitor.Options
{
    internal interface IDynamicOptionsChangeTokenSource<TOptions>
    {
        IChangeToken GetChangeToken();
    }
}

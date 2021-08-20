using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new Startup trigger.
    /// </summary>
    internal sealed class StartupTriggerFactory :
        ICollectionRuleTriggerFactory
    {
        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo)
        {
            return new StartupTrigger();
        }
    }
}

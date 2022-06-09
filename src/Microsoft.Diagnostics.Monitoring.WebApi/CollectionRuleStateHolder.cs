// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleStateHolder
    {
        public CollectionRuleState CurrentState { get; private set; } = CollectionRuleState.Running;
        public string CurrentStateReason { get; private set; } = Strings.Message_CollectionRuleStateReason_Running;

        private readonly object _lock = new object();

        public CollectionRuleStateHolder(CollectionRuleStateHolder other)
        {
            lock (_lock)
            {
                CurrentState = other.CurrentState;
                CurrentStateReason = other.CurrentStateReason;
            }
        }

        public CollectionRuleStateHolder()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void BeginActionExecution()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.ActionExecuting;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions;
            }
        }

        public void ActionExecutionSucceeded()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState == CollectionRuleState.ActionExecuting);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void ActionExecutionFailed()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState == CollectionRuleState.ActionExecuting);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void BeginThrottled()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Throttled;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Throttled;
            }
        }

        public void EndThrottled()
        {
            lock (_lock)
            {
                if (CurrentState == CollectionRuleState.Throttled)
                {
                    CurrentState = CollectionRuleState.Running;
                    CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
                }
            }
        }

        public void StartupTriggerCompleted()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup;
            }
        }

        public void RuleDurationReached()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration;
            }
        }

        public void ActionCountReached()
        {
            lock (_lock)
            {
                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount;
            }
        }

        public void RuleFailure(string errorMessage)
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = string.Format(CultureInfo.InvariantCulture, Strings.Message_CollectionRuleStateReason_Finished_Failure, errorMessage);
            }
        }
    }
}

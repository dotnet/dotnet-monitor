// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions
{
    internal sealed class CollectionRuleActionExecutionException : MonitoringException
    {
        public int ActionIndex { get; }

        public string ActionType { get; }

        public CollectionRuleActionExecutionException(Exception innerException, string actionType, int actionIndex)
            : base(innerException.Message, innerException)
        {
            ActionIndex = actionIndex;
            ActionType = actionType;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;
using Microsoft.Diagnostics.Monitoring;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions
{
    internal class CollectionRuleActionExecutionException : MonitoringException
    {
        public int ActionIndex { get; }

        public CollectionRuleActionExecutionException(Exception innerException, int actionIndex) : base(innerException.Message, innerException)
        {
            ActionIndex = actionIndex;
        }
    }
}

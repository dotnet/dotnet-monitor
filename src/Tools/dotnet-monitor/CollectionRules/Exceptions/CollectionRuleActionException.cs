// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.Diagnostics.Monitoring;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions
{
    internal sealed class CollectionRuleActionException : MonitoringException
    {
        public CollectionRuleActionException(Exception innerException) : base(innerException.Message, innerException)
        {
        }
    }
}

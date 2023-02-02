// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public enum TestAppLogEventIds
    {
        ScenarioState = 1,
        ReceivedCommand = 2,
        EnvironmentVariable = 3,
        BoundUrl = 4,
    }
    public static class TestAppLogEventIdExtensions
    {
        public static EventId EventId(this TestAppLogEventIds enumVal)
        {
            string name = Enum.GetName(typeof(TestAppLogEventIds), enumVal);
            int id = enumVal.Id();
            return new EventId(id, name);
        }
        public static int Id(this TestAppLogEventIds enumVal)
        {
            int id = (int)enumVal;
            return id;
        }
    }
}

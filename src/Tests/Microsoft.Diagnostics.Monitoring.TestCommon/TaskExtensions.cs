// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TaskExtensions
    {
        public static async Task SafeAwait(this Task task, ITestOutputHelper outputHelper)
        {
            if (task != null)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    outputHelper.WriteLine("Warning: Awaiting task threw exception: " + ex.ToString());
                }
            }
        }

        public static async Task<T> SafeAwait<T>(this Task<T> task, ITestOutputHelper outputHelper, T fallbackValue = default(T))
        {
            if (task != null)
            {
                try
                {
                    return await task;
                }
                catch (Exception ex)
                {
                    outputHelper.WriteLine("Warning: Awaiting task threw exception: " + ex.ToString());
                }
            }
            return fallbackValue;
        }
    }
}

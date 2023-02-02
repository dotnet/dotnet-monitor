// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TaskTestExtensions
    {
        public static async Task SafeAwait(this Task task, ITestOutputHelper outputHelper = null)
        {
            if (task != null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    outputHelper?.WriteLine("Warning: Exception thrown while awaiting task: {0}", ex);
                }
            }
        }

        public static async Task<T> SafeAwait<T>(this Task<T> task, ITestOutputHelper outputHelper = null, T fallbackValue = default(T))
        {
            if (task != null)
            {
                try
                {
                    return await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    outputHelper?.WriteLine("Warning: Exception thrown while awaiting task: {0}", ex);
                }
            }
            return fallbackValue;
        }
    }
}

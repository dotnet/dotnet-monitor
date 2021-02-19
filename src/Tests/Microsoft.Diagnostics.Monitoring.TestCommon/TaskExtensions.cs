// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TaskExtensions
    {
        public static async Task SafeAwait(this Task task)
        {
            if (task != null)
            {
                try
                {
                    await task;
                }
                catch
                {
                }
            }
        }

        public static async Task<T> SafeAwait<T>(this Task<T> task, T fallbackValue = default(T))
        {
            if (task != null)
            {
                try
                {
                    return await task;
                }
                catch
                {
                }
            }
            return fallbackValue;
        }
    }
}

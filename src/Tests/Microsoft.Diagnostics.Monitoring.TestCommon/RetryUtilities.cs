﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class RetryUtilities
    {
        public static void Retry(Action func, Func<Exception, bool> shouldRetry, ITestOutputHelper outputHelper, int maxRetryCount = 3)
        {
            int attemptIteration = 0;
            while (true)
            {
                attemptIteration++;
                outputHelper.WriteLine("===== Attempt #{0} =====", attemptIteration);
                try
                {
                    func();
                    break;
                }
                catch (Exception ex) when (attemptIteration < maxRetryCount && shouldRetry(ex))
                {
                    // JSFIX: Log
                }
            }
        }

        public static async Task RetryAsync(Func<Task> func, Func<Exception, bool> shouldRetry, ITestOutputHelper outputHelper, int maxRetryCount = 3)
        {
            int attemptIteration = 0;
            while (true)
            {
                attemptIteration++;
                outputHelper.WriteLine("===== Attempt #{0} =====", attemptIteration);
                try
                {
                    await func();
                    break;
                }
                catch (Exception ex) when (attemptIteration < maxRetryCount && shouldRetry(ex))
                {
                    // JSFIX: Log
                }
            }
        }
    }
}

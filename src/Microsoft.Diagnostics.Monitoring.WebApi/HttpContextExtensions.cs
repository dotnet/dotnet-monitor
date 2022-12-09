// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class HttpContextExtensions
    {
        public static void AllowSynchronousIO(this HttpContext httpContext)
        {
            var syncIOFeature = httpContext.Features.Get<IHttpBodyControlFeature>();
            if (null == syncIOFeature)
            {
                Debug.Fail($"Unable to obtain {nameof(IHttpBodyControlFeature)}");
            }
            else
            {
                syncIOFeature.AllowSynchronousIO = true;
            }
        }
    }
}

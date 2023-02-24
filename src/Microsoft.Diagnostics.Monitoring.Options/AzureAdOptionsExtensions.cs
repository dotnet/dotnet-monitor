// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class AzureAdOptionsExtensions
    {
        public static Uri GetInstance(this AzureAdOptions options)
        {
            return options.Instance ?? new Uri(AzureAdOptionsDefaults.DefaultInstance);
        }

        public static string GetTenantId(this AzureAdOptions options)
        {
            return options.TenantId ?? AzureAdOptionsDefaults.DefaultTenantId;
        }

        public static Uri GetAppIdUri(this AzureAdOptions options)
        {
            return options.AppIdUri ?? new Uri(FormattableString.Invariant($"api://{options.ClientId}"));
        }
    }
}

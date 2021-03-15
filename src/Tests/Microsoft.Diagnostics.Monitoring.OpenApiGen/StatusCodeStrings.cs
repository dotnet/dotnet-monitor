﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal class StatusCodeStrings
    {
        public static readonly string Status400BadRequest = StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture);
        public static readonly string Status401Unauthorized = StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture);
        public static readonly string Status429TooManyRequests = StatusCodes.Status429TooManyRequests.ToString(CultureInfo.InvariantCulture);
    }
}

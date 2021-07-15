﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi
#endif
{
    internal class DiagnosticPortOptionsDefaults
    {
        public const DiagnosticPortConnectionMode ConnectionMode = DiagnosticPortConnectionMode.Connect;
    }
}

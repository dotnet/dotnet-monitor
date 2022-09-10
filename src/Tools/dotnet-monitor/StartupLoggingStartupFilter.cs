// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class StartupLoggingStartupFilter : IStartupFilter
    {
        private readonly StartupLogging _logging;

        public StartupLoggingStartupFilter(StartupLogging logging)
        {
            _logging = logging;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                _logging.Initialize();

                next(builder);
            };
        }
    }
}

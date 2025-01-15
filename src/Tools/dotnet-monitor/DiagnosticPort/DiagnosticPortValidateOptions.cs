// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class DiagnosticPortValidateOptions :
        IValidateOptions<DiagnosticPortOptions>
    {
        public ValidateOptionsResult Validate(string? name, DiagnosticPortOptions options)
        {
            var failures = new List<string>();

            if (options.ConnectionMode == DiagnosticPortConnectionMode.Listen
                && string.IsNullOrEmpty(options.EndpointName))
            {
                failures.Add(Strings.ErrorMessage_DiagnosticPortMissingInListenMode);
            }

            // On Windows, the server is implemented using Named Pipes.
            // -1 means use as many as resources allow.
            // Otherwise, the allowed instances are between 1 and 254.

            // For Linux domain sockets, all values are valid with 0 representing 1 connection
            // and other invalid values being normalized to a system defined maximum.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
               ((options.MaxConnections < -1) || (options.MaxConnections == 0) || (options.MaxConnections > 254)))
            {
                failures.Add(Strings.ErrorMessage_MaxConnections);
            }

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}

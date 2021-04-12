// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class DiagnosticPortValidateOptions :
        IValidateOptions<DiagnosticPortOptions>
    {
        public ValidateOptionsResult Validate(string name, DiagnosticPortOptions options)
        {
            if (options.ConnectionMode == DiagnosticPortConnectionMode.Listen
                && string.IsNullOrEmpty(options.EndpointName))
            {
                return ValidateOptionsResult.Fail("In 'Listen' mode, the diagnostic port endpoint name must be specified.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}

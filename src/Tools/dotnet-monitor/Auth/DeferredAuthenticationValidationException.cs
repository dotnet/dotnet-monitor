// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth
{
    internal sealed class DeferredAuthenticationValidationException : Exception
    {
        public string ConfigurationPath { get; }
        public IEnumerable<string> FailureMessages { get; }

        public DeferredAuthenticationValidationException(string configurationPath, IEnumerable<ValidationResult> results) : base()
        {
            ConfigurationPath = configurationPath;

            List<string> failureMessages = new(results.Count());
            foreach (ValidationResult result in results)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    failureMessages.Add(result.ErrorMessage);
                }
            }

            FailureMessages = failureMessages;
        }

        public override string Message => string.Format(Strings.ErrorMessage_StartupConfigurationValidationException, ConfigurationPath, string.Join(Environment.NewLine, FailureMessages));
    }
}

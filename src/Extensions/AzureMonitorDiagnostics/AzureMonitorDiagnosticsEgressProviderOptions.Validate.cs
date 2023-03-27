// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

internal partial class AzureMonitorDiagnosticsEgressProviderOptions :
    IValidatableObject
{
    /// <summary>
    /// Validates this object.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A list of validation results.</returns>
    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        // Validate Compression
        switch (Compression)
        {
            case CompressionType.None:
            case CompressionType.GZip:
                break;

            default:
                yield return new ValidationResult(Strings.ErrorMessage_UnsupportedCompressionType);
                break;
        }

        // Validate ConnectionString
        if (string.IsNullOrEmpty(ConnectionString))
        {
            yield return new ValidationResult(Strings.ErrorMessage_ConnectionStringIsMissing);
        }

        // The connection string may be just a GUID. This represents an
        // instrumentation key for an Application Insights resource in Azure
        // Public cloud using the global, public endpoints.
        if (Guid.TryParse(ConnectionString, out _))
        {
            ValidatedConnectionString = AzureMonitorDiagnostics.ConnectionString.FromInstrumentationKey(ConnectionString);
        }

        if (!TokenString.TryParse(ConnectionString, out TokenString? tokens))
        {
            yield return new ValidationResult(Strings.ErrorMessage_ConnectionStringIsMalformed);
            yield break;
        }

        if (!tokens.TryGetValue(nameof(AzureMonitorDiagnostics.ConnectionString.InstrumentationKey), out _))
        {
            yield return new ValidationResult(Strings.ErrorMessage_ConnectionStringMissingInstrumentationKey);
        }

        ValidatedConnectionString = new ConnectionString(tokens);
    }

    internal ConnectionString? ValidatedConnectionString { get; private set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ValidationResultExtensions
    {
        public static bool IsSuccess([NotNullWhen(false)] this ValidationResult? result)
        {
            // ValidationResult.Success is null, and a null value indicates there are no validation errors.
            return result == ValidationResult.Success;
        }
    }
}

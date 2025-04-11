// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // The Validation source generator doesn't run for libraries that don't call AddValidation,
    // so we can't generate IValidatableInfo by using [ValidatableType] directly on types defined
    // in ProjectReferences. This is a workaround to force the generator running in this project to
    // generate IValidatableInfo for the referenced types. The containing class is not used otherwise.
    [ValidatableType]
    internal class ValidatableTypes
    {
        public required MetricsOptions MetricsOptions { get; init; }

        public required AuthenticationOptions AuthenticationOptions { get; init; }
    }
}

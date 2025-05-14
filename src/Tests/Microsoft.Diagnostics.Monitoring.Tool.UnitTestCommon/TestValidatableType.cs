// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Http.Validation.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    [ValidatableType]
    internal sealed class TestValidatableTypes
    {
        public required PassThroughOptions PassThroughOptions { get; init; }

        public static void AddValidation(IServiceCollection services)
        {
            TestGeneratedServiceCollectionExtensions.AddValidation(services);
        }
    }
}

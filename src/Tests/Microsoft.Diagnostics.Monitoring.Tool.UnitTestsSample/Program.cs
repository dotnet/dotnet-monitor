// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;

var builder = WebApplication.CreateBuilder(args);
Microsoft.AspNetCore.Http.Validation.Generated.GeneratedServiceCollectionExtensions.AddValidation(builder.Services);
builder.Build();

public partial class Program {}

[ValidatableType]
sealed class TestValidatableType
{
    public required ExtensionManifest ExtensionManifest { get; init; }

    // public RootOptions RootOptions { get; init; } // TODO: this hits bad generated code.
    // Take a more granular approach for now.
    public required FileSystemEgressProviderOptions FileSystemEgressProviderOptions { get; init; }

    public required ExecuteOptions ExecuteOptions { get; init; }
}

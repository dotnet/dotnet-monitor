// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddValidation();
builder.Build();

public partial class Program {}

[ValidatableType]
sealed class TestValidatableType
{
    public required ExtensionManifest ExtensionManifest { get; init; }
}

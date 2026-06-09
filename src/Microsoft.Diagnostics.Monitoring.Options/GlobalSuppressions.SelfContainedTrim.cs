// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Trim-analysis suppressions for the self-contained, fully-trimmed tool build
// (dotnet-monitor-selfcontained; see src/Tools/dotnet-monitor/SelfContainedTool.targets).
//
// The types listed here perform DataAnnotations validation ([MinLength], and the
// IValidatableObject.Validate funnel) that the trimmer flags under TrimMode=full. The
// validated type sets live in assemblies that are preserved whole by the TrimmerRootAssembly
// items in SelfContainedTool.targets (including Microsoft.Diagnostics.Monitoring.Options
// itself), so the members the validation reflection touches are not actually removed and the
// flagged code is safe.
//
// GlobalCounterOptions is declared in the Microsoft.Diagnostics.Monitoring.WebApi namespace
// but compiled into this (Options) assembly, hence the WebApi-namespaced doc id below.
//
// These attributes are inert in the framework-dependent build and the container image (no
// trimming happens there); they only take effect when the self-contained tool is published.

using System.Diagnostics.CodeAnalysis;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.Options.MonitorCapability", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.GlobalCounterOptions", Justification = "DataAnnotations validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]

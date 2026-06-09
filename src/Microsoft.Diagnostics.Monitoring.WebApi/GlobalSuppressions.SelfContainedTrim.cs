// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Trim-analysis suppressions for the self-contained, fully-trimmed tool build
// (dotnet-monitor-selfcontained; see src/Tools/dotnet-monitor/SelfContainedTool.targets).
//
// Every type listed here performs reflection that the trimmer flags under TrimMode=full
// (reflection-based JSON serialization of the response/model types, minimal-API endpoint
// mapping, or DataAnnotations [MinLength] validation on the request models). The
// reflected/serialized/validated type sets live in assemblies that are preserved whole by
// the TrimmerRootAssembly items in SelfContainedTool.targets (in particular
// Microsoft.Diagnostics.Monitoring.WebApi itself), so the members the reflection touches are
// not actually removed and the flagged code is safe.
//
// These attributes are inert in the framework-dependent build and the container image (no
// trimming happens there); they only take effect when the self-contained tool is published.

using System.Diagnostics.CodeAnalysis;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.JsonProfilerMessage", Justification = "Reflection-based JSON serialization over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Controllers.DiagController", Justification = "Minimal-API endpoint mapping over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Controllers.DiagnosticsControllerBase", Justification = "Minimal-API link generation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Controllers.ExceptionsController", Justification = "Minimal-API endpoint mapping over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Controllers.MetricsController", Justification = "Minimal-API endpoint mapping over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Controllers.OperationsController", Justification = "Minimal-API endpoint mapping over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing.CapturedParametersJsonFormatter", Justification = "Reflection-based JSON serialization over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Stacks.JsonStacksFormatter", Justification = "Reflection-based JSON serialization over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Stacks.SpeedscopeStacksFormatter", Justification = "Reflection-based JSON serialization over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.CaptureParametersConfiguration", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.EventMetricsMeter", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.EventMetricsProvider", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.EventPipeConfiguration", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.EventPipeProvider", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.WebApi.Models.MethodDescription", Justification = "[MinLength] validation over a type set preserved via TrimmerRootAssembly in the self-contained build.")]

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Trim-analysis suppressions for the self-contained, fully-trimmed egress extensions
// (dotnet-monitor-egress-azureblobstorage / -s3storage; see src/Extensions/SelfContainedExtension.targets).
//
// Both extensions share this assembly, so the suppressions below cover both of them.
//
// EgressHelper drives the reflection-based egress infrastructure: it serializes the result
// (JsonSerializer.Serialize), deserializes the inbound payload (JsonSerializer.Deserialize), and
// binds the provider options from configuration (ConfigurationBinder.Bind). DataAnnotationValidateOptions
// runs DataAnnotations validation (ValidationContext / Validator.TryValidateObject) over the same options.
// All of these reach members of the serialized/bound/validated types by reflection, which the trimmer flags
// under TrimMode=full.
//
// Those type sets are preserved whole by the TrimmerRootAssembly items in
// src/Extensions/SelfContainedExtension.targets: the extension's own assembly (carrying each
// *EgressProviderOptions and EgressProvider implementation), this Extension.Common assembly (carrying
// ExtensionEgressPayload / EgressArtifactResult), and System.ComponentModel.TypeConverter (carrying the
// intrinsic TypeConverters used by [Range(typeof(TimeSpan), ...)] validation and TimeSpan binding).
// Reflection-based JSON and configuration binding are intentionally enabled
// (JsonSerializerIsReflectionEnabledByDefault=true, EnableConfigurationBindingGenerator=false), so the
// flagged members are not actually removed and the code is safe. The IL2091 that the trimmer reports for
// the DI registration of the provider type is addressed precisely with [DynamicallyAccessedMembers] on the
// TProvider type parameter in EgressHelper rather than suppressed here.
//
// These attributes are inert in the framework-dependent extension build (no trimming happens there); the
// GlobalSuppressions.SelfContainedTrim.cs file is only compiled into this assembly when the
// DotNetMonitorBuildSelfContainedTool toggle is on (see the project file).

using System.Diagnostics.CodeAnalysis;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.Extension.Common.EgressHelper", Justification = "Reflection-based JSON (de)serialization of the egress payload/result and reflection-based configuration binding of the provider options; the referenced type sets are preserved via TrimmerRootAssembly in the self-contained extension build and reflection JSON/binding are intentionally enabled.")]
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.Extension.Common.DataAnnotationValidateOptions`1", Justification = "DataAnnotations validation over provider options whose type is preserved via TrimmerRootAssembly in the self-contained extension build.")]

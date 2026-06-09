// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Trim-analysis suppression for the self-contained, fully-trimmed S3Storage egress extension
// (dotnet-monitor-egress-s3storage; see src/Extensions/SelfContainedExtension.targets).
//
// S3StorageEgressProviderOptions.PreSignedUrlExpiry is annotated with
// [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]. The RangeAttribute(Type, string, string)
// constructor is marked [RequiresUnreferencedCode] because it converts the bound min/max strings to the
// target type with a reflection-resolved TypeConverter at validation time - here the NullableConverter
// wrapping TimeSpanConverter (the property is TimeSpan?). The self-contained build roots
// System.ComponentModel.TypeConverter whole (see SelfContainedExtension.targets), so both converters are
// preserved and the range validation keeps working; the warning is therefore safe to suppress.
//
// This warning only appears once the S3Storage assembly itself is rooted for the self-contained publish, so
// it cannot be addressed by the shared external link-attributes file (that document is also applied to the
// AzureBlobStorage build, whose closure does not contain this assembly, which would produce IL2008). The
// suppression must be compiled into this assembly, and only when the self-contained toggle is on (the file is
// excluded from the default / shipping extension build via the project file).

using System.Diagnostics.CodeAnalysis;

[assembly: UnconditionalSuppressMessage("Trimming", "IL2026", Scope = "type", Target = "T:Microsoft.Diagnostics.Monitoring.Extension.S3Storage.S3StorageEgressProviderOptions", Justification = "[Range(typeof(TimeSpan), ...)] validation uses a reflection-resolved TypeConverter (NullableConverter over TimeSpanConverter) that is preserved whole via the System.ComponentModel.TypeConverter TrimmerRootAssembly in the self-contained extension build.")]

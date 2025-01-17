// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Workaround for using the required modifier in .NET 6: https://github.com/dotnet/core/issues/8016
#if NET6_0

namespace System.Runtime.CompilerServices;

[System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public class RequiredMemberAttribute : Attribute { }

[System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name) { }
}

#endif 

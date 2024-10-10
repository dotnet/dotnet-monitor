// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
#endif
{
    internal static class NameIdentificationEvents
    {
        public static class FunctionDescPayloads
        {
            public const int FunctionId = 0;
            public const int MethodToken = 1;
            public const int ClassId = 2;
            public const int ClassToken = 3;
            public const int ModuleId = 4;
            public const int StackTraceHidden = 5;
            public const int Name = 6;
            public const int TypeArgs = 7;
            public const int ParameterTypes = 8;
        }

        public static class ClassDescPayloads
        {
            public const int ClassId = 0;
            public const int ModuleId = 1;
            public const int Token = 2;
            public const int Flags = 3;
            public const int StackTraceHidden = 4;
            public const int TypeArgs = 5;
        }

        public static class ModuleDescPayloads
        {
            public const int ModuleId = 0;
            public const int ModuleVersionId = 1;
            public const int Name = 2;
        }

        public static class TokenDescPayloads
        {
            public const int ModuleId = 0;
            public const int Token = 1;
            public const int OuterToken = 2;
            public const int StackTraceHidden = 3;
            public const int Name = 4;
            public const int Namespace = 5;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TestAppScenarios
    {
        public static class Commands
        {
            public const string EndScenario = nameof(EndScenario);
            public const string StartScenario = nameof(StartScenario);
            public const string PrintEnvironmentVariables = nameof(PrintEnvironmentVariables);
        }

        public enum ScenarioState
        {
            Waiting,
            Ready,
            Executing,
            Finished
        }

        public static class AspNet
        {
            public const string Name = nameof(AspNet);

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }

        public static class AsyncWait
        {
            public const string Name = nameof(AsyncWait);

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }

        public static class Exceptions
        {
            public const string Name = nameof(Exceptions);

            public static class SubScenarios
            {
                public const string SingleException = nameof(SingleException);
                public const string MultipleExceptions = nameof(MultipleExceptions);
                public const string RepeatException = nameof(RepeatException);
                public const string AsyncException = nameof(AsyncException);
                public const string FrameworkException = nameof(FrameworkException);
                public const string CustomException = nameof(CustomException);
                public const string EsotericStackFrameTypes = nameof(EsotericStackFrameTypes);
                public const string ReversePInvokeException = nameof(ReversePInvokeException);
                public const string DynamicMethodException = nameof(DynamicMethodException);
                public const string ArrayException = nameof(ArrayException);
                public const string InnerUnthrownException = nameof(InnerUnthrownException);
                public const string InnerThrownException = nameof(InnerThrownException);
                public const string EclipsingException = nameof(EclipsingException);
                public const string EclipsingExceptionFromMethodCall = nameof(EclipsingExceptionFromMethodCall);
                public const string AggregateException = nameof(AggregateException);
                public const string ReflectionTypeLoadException = nameof(ReflectionTypeLoadException);
                public const string HiddenFramesExceptionCommand = nameof(HiddenFramesExceptionCommand);
            }

            public static class Commands
            {
                public const string Begin = nameof(Begin);
                public const string End = nameof(End);
            }
        }

        public static class FunctionProbes
        {
            public const string Name = nameof(FunctionProbes);

            public static class SubScenarios
            {
#if NET7_0_OR_GREATER
                /* Probe management */
                public const string ProbeInstallation = nameof(ProbeInstallation);
                public const string ProbeUninstallation = nameof(ProbeUninstallation);
                public const string ProbeReinstallation = nameof(ProbeReinstallation);

                /* Parameter capturing */
                public const string CapturePrimitives = nameof(CapturePrimitives);
                public const string CaptureNativeIntegers = nameof(CaptureNativeIntegers);
                public const string CaptureValueTypes = nameof(CaptureValueTypes);
                public const string CaptureTypeRefValueTypes = nameof(CaptureTypeRefValueTypes);
                public const string CaptureImplicitThis = nameof(CaptureImplicitThis);
                public const string CaptureExplicitThis = nameof(CaptureExplicitThis);

                public const string CaptureNoParameters = nameof(CaptureNoParameters);
                public const string CaptureUnsupportedParameters = nameof(CaptureUnsupportedParameters);
                public const string CaptureValueTypeImplicitThis = nameof(CaptureValueTypeImplicitThis);
                public const string CaptureValueTypeTypeSpecs = nameof(CaptureValueTypeTypeSpecs);
                public const string CaptureGenerics = nameof(CaptureGenerics);

                /* Interesting methods */
                public const string AsyncMethod = nameof(AsyncMethod);
                public const string GenericMethods = nameof(GenericMethods);
                public const string ExceptionRegionAtBeginningOfMethod = nameof(ExceptionRegionAtBeginningOfMethod);

                /* Fault injection */
                public const string ExceptionThrownByProbe = nameof(ExceptionThrownByProbe);
                public const string RecursingProbe = nameof(RecursingProbe);
                public const string RequestInstallationOnProbeFunction = nameof(RequestInstallationOnProbeFunction);

                /* Monitor context */
                public const string ProbeInMonitorContext = nameof(ProbeInMonitorContext);

                /* Self tests */
                public const string AssertsInProbesAreCaught = nameof(AssertsInProbesAreCaught);
#else // NET7_0_OR_GREATER
                public const string ValidateNoMutatingProfiler = nameof(ValidateNoMutatingProfiler);
#endif // NET7_0_OR_GREATER
            }
        }

        public static class Stacks
        {
            public const string Name = nameof(Stacks);

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }

        public static class EnvironmentVariables
        {
            public const string Name = nameof(EnvironmentVariables);
            public const string CustomVariableName = nameof(CustomVariableName);
            public const string IncrementVariableName = nameof(IncrementVariableName);

            public static class Commands
            {
                public const string IncVar = nameof(IncVar);
                public const string ShutdownScenario = nameof(ShutdownScenario);
            }
        }

        public static class Execute
        {
            public const string Name = nameof(Execute);
        }

        public static class Logger
        {
            public const string Name = nameof(Logger);

            public static class Categories
            {
                public const string LoggerCategory1 = nameof(LoggerCategory1);
                public const string LoggerCategory2 = nameof(LoggerCategory2);
                public const string LoggerCategory3 = nameof(LoggerCategory3);
                // Used to denote that no more relevant data will be sent via logging.
                public const string SentinelCategory = nameof(SentinelCategory);
                // Use to flush the event entries through eventing buffers.
                public const string FlushCategory = nameof(FlushCategory);
            }

            public static class Commands
            {
                public const string StartLogging = nameof(StartLogging);
            }
        }

        public static class Metrics
        {
            public const string Name = nameof(Metrics);

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }
        public static class ParameterCapturing
        {
            public const string Name = nameof(ParameterCapturing);

            public static class SubScenarios
            {
                public const string AspNetApp = nameof(AspNetApp);
                public const string NonAspNetApp = nameof(NonAspNetApp);
            }

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }

        public static class SpinWait
        {
            public const string Name = nameof(SpinWait);

            public static class Commands
            {
                public const string StartSpin = nameof(StartSpin);

                public const string StopSpin = nameof(StopSpin);
            }
        }

        public static class TraceEvents
        {
            public const string Name = nameof(TraceEvents);
            public const string UniqueEventName = "UniqueEvent";
            public const string EventProviderName = "TestScenario";
            public const string UniqueEventMessage = "FooBar";
            public const string UniqueEventPayloadField = "message";

            public const TraceEventOpcode UniqueEventOpcode = TraceEventOpcode.Reply;

            public static class Commands
            {
                public const string EmitUniqueEvent = nameof(EmitUniqueEvent);
                public const string ShutdownScenario = nameof(ShutdownScenario);
            }
        }
    }
}

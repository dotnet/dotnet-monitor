// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "corprof.h"
#include "corhdr.h"
#include "AssemblyProbePrep.h"
#include "CallbackDefinitions.h"

#include <vector>
#include <memory>

typedef struct _INSTRUMENTATION_REQUEST
{
    ULONG64 uniquifier;
    std::vector<ULONG32> boxingTypes;

    ModuleID moduleId;
    mdMethodDef methodDef;

    std::shared_ptr<AssemblyProbePrepData> pAssemblyData;
} INSTRUMENTATION_REQUEST;

class ProbeInjector
{
    public:
        static HRESULT InstallProbe(
            ICorProfilerInfo* pICorProfilerInfo,
            ICorProfilerFunctionControl* pICorProfilerFunctionControl,
            FaultingProbeCallback pFaultingProbeCallback,
            const INSTRUMENTATION_REQUEST& request);

    private:
        static HRESULT GetBoxingToken(
            ULONG32 typeInfo,
            const COR_LIB_TYPE_TOKENS& corLibTypeTokens,
            mdToken& boxedType);
};
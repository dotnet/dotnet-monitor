// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "corprof.h"
#include "corhdr.h"
#include "AssemblyProbePrep.h"
#include "CallbackDefinitions.h"

#include <vector>
#include <memory>

enum class InstructionType : USHORT
{
    UNKNOWN = 0,
    SPECIAL_CASE_TOKEN,
    METADATA_TOKEN
};

enum class SpecialCaseBoxingTypes : ULONG32
{
    TYPE_UNKNOWN = 0,
    TYPE_OBJECT,
    TYPE_BOOLEAN,
    TYPE_CHAR,
    TYPE_SBYTE,
    TYPE_BYTE,
    TYPE_INT16,
    TYPE_UINT16,
    TYPE_INT32,
    TYPE_UINT32,
    TYPE_INT64,
    TYPE_UINT64,
    TYPE_INTPTR,
    TYPE_UINTPTR,
    TYPE_SINGLE,
    TYPE_DOUBLE
};

typedef struct _PARAMETER_BOXING_INSTRUCTIONS
{
    InstructionType instructionType;
    union {
        ULONG32 mdToken;
        SpecialCaseBoxingTypes specialCaseToken;
    } token;
} PARAMETER_BOXING_INSTRUCTIONS;

typedef struct _INSTRUMENTATION_REQUEST
{
    ULONG64 uniquifier;
    std::vector<PARAMETER_BOXING_INSTRUCTIONS> boxingInstructions;

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
        static HRESULT GetSpecialCaseBoxingToken(
            SpecialCaseBoxingTypes specialCaseType,
            const COR_LIB_TYPE_TOKENS& corLibTypeTokens,
            mdToken& boxedType);
};

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
    METADATA_TOKEN,
    TYPESPEC
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

typedef union _BOXING_INSTRUCTION_TOKEN_UNION
{
    ULONG32 mdToken;
    SpecialCaseBoxingTypes specialCaseToken;
} BOXING_INSTRUCTION_TOKEN_UNION;

// Ensure that the size of SpecialCaseBoxingTypes is the same size as ULONG32
// so that the union used below for easy type access doesn't alter the size of the struct.
static_assert(sizeof(SpecialCaseBoxingTypes) == sizeof(ULONG32), "SpecialCaseBoxingTypes should be same size as ULONG32");
// Also make sure the union is the expected size.
static_assert(sizeof(BOXING_INSTRUCTION_TOKEN_UNION) == sizeof(ULONG32), "BOXING_INSTRUCTION_TOKEN_UNION should be same size as ULONG32");

typedef struct _PARAMETER_BOXING_INSTRUCTIONS
{
    InstructionType instructionType;
    BOXING_INSTRUCTION_TOKEN_UNION token;

    PCCOR_SIGNATURE signatureBufferPointer;
    ULONG32 signatureBufferLength;
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

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "cor.h"
#include "corprof.h"
#include "macros.h"
#include "ProbeInjector.h"
#include "../Utilities/ILRewriter.h"

#include <vector>

//
// SpecialCaseBoxingTypes shares the same format as other mdTokens.
// Instrumentation requests provide special boxing instructions by using SpecialCaseBoxingTypeFlag
// as the "token type" and the SpecialCaseBoxingTypes enum as the RID.
//
constexpr ULONG32 SpecialCaseBoxingTypeFlag = 0x7f000000;
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
    TYPE_SINGLE,
    TYPE_DOUBLE
};

HRESULT ProbeInjector::InstallProbe(
    ICorProfilerInfo* pICorProfilerInfo,
    ICorProfilerFunctionControl* pICorProfilerFunctionControl,
    FaultingProbeCallback pFaultingProbeCallback,
    const INSTRUMENTATION_REQUEST& request)
{
    constexpr OPCODE CEE_LDC_NATIVE_I = sizeof(size_t) == 8 ? CEE_LDC_I8 : CEE_LDC_I4;

    ExpectedPtr(pICorProfilerInfo);
    ExpectedPtr(pICorProfilerFunctionControl);
    ExpectedPtr(pFaultingProbeCallback);

    if (request.boxingTypes.size() > UINT32_MAX)
    {
        return E_INVALIDARG;
    }

    HRESULT hr;
    ILRewriter rewriter(pICorProfilerInfo, pICorProfilerFunctionControl, request.moduleId, request.methodDef);
    IfFailRet(rewriter.Import());

    const COR_LIB_TYPE_TOKENS& corLibTypeTokens = request.pAssemblyData->GetCorLibTypeTokens();

    ILInstr* pInsertProbeBeforeThisInstr = rewriter.GetILList()->m_pNext;
    ILInstr* pNewInstr = nullptr;

    ILInstr* pTryBegin = nullptr;
    ILInstr* pCatchBegin = nullptr;
    ILInstr* pCatchEnd = nullptr;

    ILInstr* pNestedTryBegin = nullptr;
    ILInstr* pNestedTryLeave = nullptr;
    ILInstr* pNestedCatchBegin = nullptr;
    ILInstr* pNestedCatchEnd = nullptr;

    UINT32 numArgs = static_cast<UINT32>(request.boxingTypes.size());

    //
    // The below IL is equivalent to:
    // try {
    //   ProbeFunction(uniquifier, new object[] { arg1, arg2, ... });
    // } catch {
    //   try {
    //     (*pFaultingProbeCallback)(uniquifier);
    //   } catch {
    //   }
    // }
    //
    // When an argument isn't supported, pass null in its place.
    //

    // START: Try block

     /* uniquifier */
    pTryBegin = rewriter.NewILInstr();
    pTryBegin->m_opcode = CEE_LDC_I8;
    pTryBegin->m_Arg64 = request.uniquifier;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pTryBegin);

    /* Args */

    // Size of array
    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LDC_I4;
    pNewInstr->m_Arg32 = numArgs;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    // Create the array
    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_NEWARR;
    pNewInstr->m_Arg32 = corLibTypeTokens.systemObjectType;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    for (UINT32 i = 0; i < numArgs; i++)
    {
        // New entry on the evaluation stack
        pNewInstr = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_DUP;
        rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

        // Index to set
        pNewInstr = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDC_I4;
        pNewInstr->m_Arg32 = i;
        rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

        // Load arg
        ULONG32 typeInfo = request.boxingTypes.at(i);
        if (typeInfo == (SpecialCaseBoxingTypeFlag | static_cast<ULONG32>(SpecialCaseBoxingTypes::TYPE_UNKNOWN)))
        {
            pNewInstr = rewriter.NewILInstr();
            pNewInstr->m_opcode = CEE_LDNULL;
            rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);
        }
        else
        {
            pNewInstr = rewriter.NewILInstr();
            pNewInstr->m_opcode = CEE_LDARG_S;
            pNewInstr->m_Arg32 = i;
            rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

            // Resolve the boxing token.
            mdToken boxedTypeToken;
            IfFailRet(GetBoxingToken(typeInfo, corLibTypeTokens, boxedTypeToken));
            if (boxedTypeToken != mdTokenNil)
            {
                pNewInstr = rewriter.NewILInstr();
                pNewInstr->m_opcode = CEE_BOX;
                pNewInstr->m_Arg32 = boxedTypeToken;
                rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);
            }
        }

        // Replace the i'th element in our new array with what we just pushed on the stack
        pNewInstr = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_STELEM_REF;
        rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);
    }

    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32 = request.pAssemblyData->GetProbeMemberRef();
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    pNewInstr = pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LEAVE;
    pNewInstr->m_pTarget = pInsertProbeBeforeThisInstr;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    // END: Try block
    // START: Catch block

    pCatchBegin = rewriter.NewILInstr();
    pCatchBegin->m_opcode = CEE_POP;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pCatchBegin);

    // START: Try block (nested)

    pNestedTryBegin = rewriter.NewILInstr();
    pNestedTryBegin->m_opcode = CEE_LDC_I8;
    pNestedTryBegin->m_Arg64 = request.uniquifier;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNestedTryBegin);

    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LDC_NATIVE_I;
    pNewInstr->m_Arg64 = reinterpret_cast<INT64>(pFaultingProbeCallback);
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_CALLI;
    pNewInstr->m_Arg32 = request.pAssemblyData->GetFaultingProbeCallbackSignature();
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

    pNestedTryLeave = rewriter.NewILInstr();
    pNestedTryLeave->m_opcode = CEE_LEAVE;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNestedTryLeave);

    // END: Try block (nested)
    // START: Catch block (nested)

    pNestedCatchBegin = rewriter.NewILInstr();
    pNestedCatchBegin->m_opcode = CEE_POP;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNestedCatchBegin);

    pNestedCatchEnd = rewriter.NewILInstr();
    pNestedCatchEnd->m_opcode = CEE_LEAVE;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNestedCatchEnd);

    pCatchEnd = rewriter.NewILInstr();
    pCatchEnd->m_opcode = CEE_LEAVE;
    pCatchEnd->m_pTarget = pInsertProbeBeforeThisInstr;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pCatchEnd);

    pNestedTryLeave->m_pTarget = pNestedCatchEnd->m_pTarget = pCatchEnd;

    // The nested protected region **must** be registered in the exception handler table first (ref: I.12.4.2.5)
    rewriter.InsertTryCatch(
        pNestedTryBegin,
        pNestedCatchBegin,
        pNestedCatchEnd,
        corLibTypeTokens.systemObjectType);

    rewriter.InsertTryCatch(
        pTryBegin,
        pCatchBegin,
        pCatchEnd,
        corLibTypeTokens.systemObjectType);

    IfFailRet(rewriter.Export());

    return S_OK;
}

HRESULT ProbeInjector::GetBoxingToken(
    UINT32 typeInfo,
    const COR_LIB_TYPE_TOKENS& corLibTypeTokens,
    mdToken& boxedType)
{
    boxedType = mdTokenNil;

    //
    // typeInfo is either:
    // 1. A real metadata token that is what should be used for boxing.
    // 2. A special case (one of the SpecialCaseBoxingTypes enum values)
    //
    if (TypeFromToken(typeInfo) != SpecialCaseBoxingTypeFlag)
    {
        boxedType = static_cast<mdToken>(typeInfo);
        return S_OK;
    }

    switch(static_cast<SpecialCaseBoxingTypes>(RidFromToken(typeInfo)))
    {
    case SpecialCaseBoxingTypes::TYPE_BOOLEAN:
        boxedType = corLibTypeTokens.systemBooleanType;
        break;
    case SpecialCaseBoxingTypes::TYPE_BYTE:
        boxedType = corLibTypeTokens.systemByteType;
        break;
    case SpecialCaseBoxingTypes::TYPE_CHAR:
        boxedType = corLibTypeTokens.systemCharType;
        break;
    case SpecialCaseBoxingTypes::TYPE_DOUBLE:
        boxedType = corLibTypeTokens.systemDoubleType;
        break;
    case SpecialCaseBoxingTypes::TYPE_INT16:
        boxedType = corLibTypeTokens.systemInt16Type;
        break;
    case SpecialCaseBoxingTypes::TYPE_INT32:
        boxedType = corLibTypeTokens.systemInt32Type;
        break;
    case SpecialCaseBoxingTypes::TYPE_INT64:
        boxedType = corLibTypeTokens.systemInt64Type;
        break;
    case SpecialCaseBoxingTypes::TYPE_SBYTE:
        boxedType = corLibTypeTokens.systemSByteType;
        break;
    case SpecialCaseBoxingTypes::TYPE_SINGLE:
        boxedType = corLibTypeTokens.systemSingleType;
        break;
    case SpecialCaseBoxingTypes::TYPE_UINT16:
        boxedType = corLibTypeTokens.systemUInt16Type;
        break;
    case SpecialCaseBoxingTypes::TYPE_UINT32:
        boxedType = corLibTypeTokens.systemUInt32Type;
        break;
    case SpecialCaseBoxingTypes::TYPE_UINT64:
        boxedType = corLibTypeTokens.systemUInt64Type;
        break;

    case SpecialCaseBoxingTypes::TYPE_OBJECT:
        // No boxing needed.
        break;

    case SpecialCaseBoxingTypes::TYPE_UNKNOWN:
    default:
        return E_FAIL;
    }

    return S_OK;
}
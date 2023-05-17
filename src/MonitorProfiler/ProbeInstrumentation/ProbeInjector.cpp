// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "cor.h"
#include "corprof.h"
#include "ProbeInjector.h"
#include "../Utilities/ILRewriter.h"

#include <vector>

//
// SpecialCaseBoxingTypes shares the same format as other mdTokens.
// Instrumentation requests provide special boxing instructions by using SpecialCaseBoxingTypeFlag
// as the "token type" and the SpecialCaseBoxingTypes enum as the RID.
//
constexpr ULONG32 SpecialCaseBoxingTypeFlag = 0xff000000;
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
    const INSTRUMENTATION_REQUEST& request)
{
    IfNullRet(pICorProfilerInfo);
    IfNullRet(pICorProfilerFunctionControl);

    if (request.boxingTypes.size() > UINT32_MAX)
    {
        return E_INVALIDARG;
    }

    HRESULT hr;
    ILRewriter rewriter(pICorProfilerInfo, pICorProfilerFunctionControl, request.moduleId, request.methodDef);
    IfFailRet(rewriter.Import());

    const COR_LIB_TYPE_TOKENS corLibTypeTokens = request.pAssemblyData->GetCorLibTypeTokens();

    //
    // JSFIX: Wrap the probe in a try/catch.
    // Consider: In the catch, try/catch a PINVOKE into the profiler,
    // notifying that a probe exception occurred and the probes need to be uninstalled.
    //

    ILInstr* pInsertProbeBeforeThisInstr = rewriter.GetILList()->m_pNext;
    ILInstr* pNewInstr = nullptr;

    UINT32 numArgs = static_cast<UINT32>(request.boxingTypes.size());

    //
    // The below IL is equivalent to: ProbeFunction(uniquifier, new object[] { arg1, arg2, ... })
    // When an argument isn't supported, pass null in its place.
    //

    /* uniquifier */
    pNewInstr = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LDC_I8;
    pNewInstr->m_Arg64 = request.uniquifier;
    rewriter.InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

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

    IfFailRet(rewriter.Export());

    return S_OK;
}

HRESULT ProbeInjector::GetBoxingToken(
    UINT32 typeInfo,
    const COR_LIB_TYPE_TOKENS& corLibTypeTokens,
    mdToken& boxedType)
{
    boxedType = mdTokenNil;

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
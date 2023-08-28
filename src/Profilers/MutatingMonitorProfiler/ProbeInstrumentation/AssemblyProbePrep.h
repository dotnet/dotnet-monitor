// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "tstring.h"
#include "Logging/Logger.h"
#include "CommonUtilities/NameCache.h"

#include <unordered_map>
#include <vector>
#include <memory>

typedef struct _COR_LIB_TYPE_TOKENS
{
    mdToken
        systemBooleanType,
        systemByteType,
        systemCharType,
        systemDoubleType,
        systemInt16Type,
        systemInt32Type,
        systemInt64Type,
        systemIntPtrType,
        systemObjectType,
        systemSByteType,
        systemSingleType,
        systemUInt16Type,
        systemUInt32Type,
        systemUInt64Type,
        systemUIntPtrType;
} COR_LIB_TYPE_TOKENS;

class AssemblyProbePrepData
{
public:
    AssemblyProbePrepData(mdMemberRef probeMemberRef, mdSignature faultingProbeCallbackSignature, COR_LIB_TYPE_TOKENS corLibTypeTokens) :
        m_probeMemberRef(probeMemberRef), m_faultingProbeCallbackSignature(faultingProbeCallbackSignature), m_corLibTypeTokens(corLibTypeTokens)
    {
    }

    const mdMemberRef GetProbeMemberRef() const { return m_probeMemberRef; }
    const mdSignature GetFaultingProbeCallbackSignature() const { return m_faultingProbeCallbackSignature; }
    const COR_LIB_TYPE_TOKENS& GetCorLibTypeTokens() const { return m_corLibTypeTokens; }

private:
    mdMemberRef m_probeMemberRef;
    mdSignature m_faultingProbeCallbackSignature;
    COR_LIB_TYPE_TOKENS m_corLibTypeTokens;
};

typedef struct _PROBE_INFO_CACHE
{
    tstring assemblyName;
    std::vector<BYTE> signature;
    std::vector<BYTE> publicKey;
    ASSEMBLYMETADATA assemblyMetadata;
    DWORD assemblyFlags;
} PROBE_INFO_CACHE;

class AssemblyProbePrep
{
    private:
        ICorProfilerInfo12* m_pCorProfilerInfo;

        NameCache m_nameCache;

        ModuleID m_resolvedCorLibId;
        tstring m_resolvedCorLibName;

        FunctionID m_probeFunctionId;
        bool m_didHydrateProbeCache;
        PROBE_INFO_CACHE m_probeCache;

        std::unordered_map<ModuleID, std::shared_ptr<AssemblyProbePrepData>> m_assemblyProbeCache;

    public:
        AssemblyProbePrep(
            ICorProfilerInfo12* profilerInfo,
            FunctionID probeFunctionId);

        HRESULT PrepareAssemblyForProbes(
            ModuleID moduleId);

        bool TryGetAssemblyPrepData(
            ModuleID moduleId,
            std::shared_ptr<AssemblyProbePrepData>& data);

    private:
        HRESULT HydrateResolvedCorLib();
        HRESULT HydrateProbeMetadata();

        HRESULT GetTokenForType(
            IMetaDataEmit* pMetadataEmit,
            mdToken resolutionScope,
            tstring name,
            mdToken& typeToken);

        HRESULT EmitProbeReference(
            ModuleID moduleId,
            mdMemberRef& probeMemberRef);

        HRESULT EmitFaultingProbeCallbackSignature(
            ModuleID moduleId,
            mdSignature& faultingProbeCallbackSignature);

        HRESULT EmitNecessaryCorLibTypeTokens(
            ModuleID moduleId,
            COR_LIB_TYPE_TOKENS& pCorLibTypeTokens);

        HRESULT GetOrEmitTokenForCorLibAssemblyRef(
            IMetaDataImport* pMetadataImport,
            IMetaDataEmit* pMetadataEmit,
            mdAssemblyRef& corlibAssemblyRef);

        HRESULT EmitCorLibAssemblyRef(
            IMetaDataEmit* pMetadataEmit,
            mdAssemblyRef& corlibAssemblyRef);
};

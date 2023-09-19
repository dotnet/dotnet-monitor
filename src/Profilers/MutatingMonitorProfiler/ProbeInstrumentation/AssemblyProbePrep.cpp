// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "corhlpr.h"
#include "macros.h"
#include "AssemblyProbePrep.h"
#include "CallbackDefinitions.h"
#include "CommonUtilities/TypeNameUtilities.h"
#include "CommonUtilities/MetadataEnumCloser.h"
#include "CommonUtilities/StringUtilities.h"

using namespace std;

#define ENUM_BUFFER_SIZE 10
#define STRING_BUFFER_LEN 256

AssemblyProbePrep::AssemblyProbePrep(ICorProfilerInfo12* profilerInfo, FunctionID probeFunctionId) :
    m_pCorProfilerInfo(profilerInfo),
    m_resolvedCorLibId(0),
    m_probeFunctionId(probeFunctionId),
    m_didHydrateProbeCache(false)
{
}

bool AssemblyProbePrep::TryGetAssemblyPrepData(ModuleID moduleId, shared_ptr<AssemblyProbePrepData>& data)
{
    auto const& it = m_assemblyProbeCache.find(moduleId);
    if (it != m_assemblyProbeCache.end())
    {
        data = it->second;
        return true;
    }

    return false;
}

HRESULT AssemblyProbePrep::PrepareAssemblyForProbes(ModuleID moduleId)
{
    HRESULT hr;

    auto const& it = m_assemblyProbeCache.find(moduleId);
    if (it != m_assemblyProbeCache.end())
    {
        return S_OK;
    }

    COR_LIB_TYPE_TOKENS corLibTypeTokens = {};
    IfFailRet(EmitNecessaryCorLibTypeTokens(moduleId, corLibTypeTokens));

    mdMemberRef probeMemberRef;
    IfFailRet(EmitProbeReference(moduleId, probeMemberRef));


    mdSignature faultingProbeCallbackSignature;
    IfFailRet(EmitFaultingProbeCallbackSignature(moduleId, faultingProbeCallbackSignature));

    shared_ptr<AssemblyProbePrepData> data(new (nothrow) AssemblyProbePrepData(probeMemberRef, faultingProbeCallbackSignature, corLibTypeTokens));
    IfNullRet(data);
    IfOomRetMem(m_assemblyProbeCache.insert({moduleId, data}));

    return S_OK;
}

HRESULT AssemblyProbePrep::EmitFaultingProbeCallbackSignature(
    ModuleID moduleId,
    mdSignature& faultingProbeCallbackSignature)
{
    HRESULT hr;
    faultingProbeCallbackSignature = mdSignatureNil;

    ComPtr<IMetaDataEmit> pMetadataEmit;
    IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
        moduleId,
        ofRead | ofWrite,
        IID_IMetaDataEmit,
        reinterpret_cast<IUnknown **>(&pMetadataEmit)));

    mdSignature signature;
    IfFailRet(pMetadataEmit->GetTokenFromSig(
        FaultingProbeCallbackCorSignature,
        sizeof(FaultingProbeCallbackCorSignature),
        &signature));

    faultingProbeCallbackSignature = signature;

    return S_OK;
}

HRESULT AssemblyProbePrep::EmitProbeReference(
    ModuleID moduleId,
    mdMemberRef& probeMemberRef)
{
    HRESULT hr;
    probeMemberRef = mdMemberRefNil;

    IfFailRet(HydrateProbeMetadata());

    std::shared_ptr<FunctionData> probeFunctionData;
    std::shared_ptr<ModuleData> probeModuleData;
    if (!m_nameCache.TryGetFunctionData(m_probeFunctionId, probeFunctionData) ||
        !m_nameCache.TryGetModuleData(probeFunctionData->GetModuleId(), probeModuleData))
    {
        return E_UNEXPECTED;
    }

    ComPtr<IMetaDataEmit> pMetadataEmit;
    IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
        moduleId,
        ofRead | ofWrite,
        IID_IMetaDataEmit,
        reinterpret_cast<IUnknown **>(&pMetadataEmit)));

    ComPtr<IMetaDataAssemblyEmit> pMetadataAssemblyEmit;
    mdAssemblyRef probeAssemblyRefToken = mdAssemblyRefNil;
    IfFailRet(pMetadataEmit->QueryInterface(IID_IMetaDataAssemblyEmit, reinterpret_cast<void **>(&pMetadataAssemblyEmit)));
    IfFailRet(pMetadataAssemblyEmit->DefineAssemblyRef(
        reinterpret_cast<const void *>(m_probeCache.publicKey.data()),
        static_cast<ULONG>(m_probeCache.publicKey.size()),
        m_probeCache.assemblyName.c_str(),
        &m_probeCache.assemblyMetadata,
        nullptr,
        0,
        m_probeCache.assemblyFlags,
        &probeAssemblyRefToken));

    tstring typeName;
    IfFailRet(m_nameCache.GetFullyQualifiedTypeName(probeFunctionData->GetClass(), typeName));

    mdTypeRef classTypeRef;
    IfFailRet(pMetadataEmit->DefineTypeRefByName(
        probeAssemblyRefToken,
        typeName.c_str(),
        &classTypeRef));

    mdMemberRef memberRef;
    IfFailRet(pMetadataEmit->DefineMemberRef(
        classTypeRef,
        probeFunctionData->GetName().c_str(),
        m_probeCache.signature.data(),
        static_cast<ULONG>(m_probeCache.signature.size()),
        &memberRef));

    probeMemberRef = memberRef;

    return S_OK;
}

HRESULT AssemblyProbePrep::EmitNecessaryCorLibTypeTokens(
    ModuleID moduleId,
    COR_LIB_TYPE_TOKENS& corLibTypeTokens)
{
    HRESULT hr;

    ComPtr<IMetaDataImport> pMetadataImport;
    IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
        moduleId,
        ofRead,
        IID_IMetaDataImport,
        reinterpret_cast<IUnknown **>(&pMetadataImport)));

    ComPtr<IMetaDataEmit> pMetadataEmit;
    IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
        moduleId,
        ofRead | ofWrite,
        IID_IMetaDataEmit,
        reinterpret_cast<IUnknown **>(&pMetadataEmit)));

    mdAssemblyRef corlibAssemblyRef;
    IfFailRet(GetOrEmitTokenForCorLibAssemblyRef(
        pMetadataImport,
        pMetadataEmit,
        corlibAssemblyRef));

#define GET_OR_DEFINE_TYPE_TOKEN(name, token) \
    IfFailRet(GetTokenForType( \
        pMetadataEmit, \
        corlibAssemblyRef, \
        name, \
        token))

    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Boolean"), corLibTypeTokens.systemBooleanType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Byte"), corLibTypeTokens.systemByteType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Char"), corLibTypeTokens.systemCharType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Double"), corLibTypeTokens.systemDoubleType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Int16"), corLibTypeTokens.systemInt16Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Int32"), corLibTypeTokens.systemInt32Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Int64"), corLibTypeTokens.systemInt64Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.IntPtr"), corLibTypeTokens.systemIntPtrType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Object"), corLibTypeTokens.systemObjectType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.SByte"), corLibTypeTokens.systemSByteType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.Single"), corLibTypeTokens.systemSingleType);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.UInt16"), corLibTypeTokens.systemUInt16Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.UInt32"), corLibTypeTokens.systemUInt32Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.UInt64"), corLibTypeTokens.systemUInt64Type);
    GET_OR_DEFINE_TYPE_TOKEN(_T("System.UIntPtr"), corLibTypeTokens.systemUIntPtrType);

    return S_OK;
}

HRESULT AssemblyProbePrep::GetTokenForType(
    IMetaDataEmit* pMetadataEmit,
    mdToken resolutionScope,
    tstring name,
    mdToken& typeToken)
{
    IfNullRet(pMetadataEmit);

    HRESULT hr;

    typeToken = mdTokenNil;

    mdTypeRef typeRefToken;
    // DefineTypeRefByName will return an existing token if available.
    IfFailRet(pMetadataEmit->DefineTypeRefByName(
        resolutionScope,
        name.c_str(),
        &typeRefToken));

    typeToken = typeRefToken;
    return S_OK;
}

HRESULT AssemblyProbePrep::GetOrEmitTokenForCorLibAssemblyRef(
    IMetaDataImport* pMetadataImport,
    IMetaDataEmit* pMetadataEmit,
    mdAssemblyRef& corlibAssemblyRef)
{
    IfNullRet(pMetadataImport);
    IfNullRet(pMetadataEmit);

    HRESULT hr;
    corlibAssemblyRef = mdAssemblyRefNil;

    IfFailRet(HydrateResolvedCorLib());

    ComPtr<IMetaDataAssemblyImport> pMetadataAssemblyImport;
    IfFailRet(pMetadataImport->QueryInterface(IID_IMetaDataAssemblyImport, reinterpret_cast<void **>(&pMetadataAssemblyImport)));

    mdAssemblyRef mdRefs[ENUM_BUFFER_SIZE];

    const ULONG expectedLength = (ULONG)m_resolvedCorLibName.length();
    unique_ptr<WCHAR[]> assemblyName(new (nothrow) WCHAR[expectedLength]);
    IfNullRet(assemblyName);

    MetadataEnumCloser<IMetaDataAssemblyImport> enumCloser(pMetadataAssemblyImport, NULL);
    ULONG count = 0;
    while ((hr = pMetadataAssemblyImport->EnumAssemblyRefs(enumCloser.GetEnumPtr(), mdRefs, ENUM_BUFFER_SIZE, &count)) == S_OK)
    {
        for (ULONG i = 0; i < count; i++)
        {
            mdAssemblyRef curRef = mdRefs[i];

            ULONG nameLength = 0;
            hr = pMetadataAssemblyImport->GetAssemblyRefProps(
                curRef,
                nullptr,
                nullptr,
                assemblyName.get(),
                expectedLength,
                &nameLength,
                nullptr,
                nullptr,
                nullptr,
                nullptr);

            if (hr == CLDB_S_TRUNCATION)
            {
                // Current assembly's name is longer than corlib's
                continue;
            }
            else if (hr != S_OK)
            {
                return hr;
            }

            if (nameLength != expectedLength)
            {
                continue;
            }

            tstring assemblyNameStr = tstring(assemblyName.get());
            if (assemblyNameStr == m_resolvedCorLibName)
            {
                corlibAssemblyRef = curRef;
                return S_OK;
            }
        }
    }

    IfFailRet(EmitCorLibAssemblyRef(pMetadataEmit, corlibAssemblyRef));

    return S_OK;
}

HRESULT AssemblyProbePrep::EmitCorLibAssemblyRef(IMetaDataEmit* pMetadataEmit, mdAssemblyRef& corlibAssemblyRef)
{
    IfNullRet(pMetadataEmit);

    HRESULT hr;
    corlibAssemblyRef = mdAssemblyRefNil;

    ComPtr<IMetaDataAssemblyEmit> pMetadataAssemblyEmit;
    IfFailRet(pMetadataEmit->QueryInterface(IID_IMetaDataAssemblyEmit, reinterpret_cast<void **>(&pMetadataAssemblyEmit)));

    BYTE publicKeyToken[] = { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e };
    ASSEMBLYMETADATA corLibMetadata = {};
    corLibMetadata.usMajorVersion = 4;

    mdAssemblyRef newAssemblyRef;
    IfFailRet(pMetadataAssemblyEmit->DefineAssemblyRef(
        publicKeyToken,
        sizeof(publicKeyToken),
        m_resolvedCorLibName.c_str(),
        &corLibMetadata,
        nullptr,
        0,
        afContentType_Default,
        &newAssemblyRef));

    corlibAssemblyRef = newAssemblyRef;

    return S_OK;
}

HRESULT AssemblyProbePrep::HydrateResolvedCorLib()
{
    if (m_resolvedCorLibId != 0)
    {
        return S_OK;
    }

    HRESULT hr;
    ModuleID corLibId = 0;
    ComPtr<ICorProfilerModuleEnum> pEnum;
    ModuleID curModuleId;
    mdTypeDef sysObjectTypeDef = mdTypeDefNil;

    IfFailRet(m_pCorProfilerInfo->EnumModules(&pEnum));
    while (pEnum->Next(1, &curModuleId, NULL) == S_OK)
    {
        //
        // Determine the identity of the System assembly by querying if the Assembly defines the
        // well known type "System.Object" as that type must be defined by the System assembly
        //
        mdTypeDef objectTypeDef = mdTypeDefNil;

        ComPtr<IMetaDataImport> pMetadataImport;
        hr = m_pCorProfilerInfo->GetModuleMetaData(
            curModuleId,
            ofRead,
            IID_IMetaDataImport,
            reinterpret_cast<IUnknown **>(&pMetadataImport));
        if (hr != S_OK)
        {
            continue;
        }

        if (pMetadataImport->FindTypeDefByName(_T("System.Object"), mdTokenNil, &objectTypeDef) != S_OK)
        {
            continue;
        }

        DWORD classAttributes = 0;
        mdToken extendsToken = mdTokenNil;
        if (pMetadataImport->GetTypeDefProps(
            objectTypeDef,
            nullptr,
            0,
            nullptr,
            &classAttributes,
            &extendsToken) != S_OK)
        {
            continue;
        }

        //
        // Also check the type properties to make sure it is a class and not a value-type definition
        // and that this type definition isn't extending another type.
        //
        bool doesExtend = pMetadataImport->IsValidToken(extendsToken);
        bool isClass = ((classAttributes & tdClassSemanticsMask) == tdClass);
        if (isClass && !doesExtend)
        {
            corLibId = curModuleId;
            sysObjectTypeDef = objectTypeDef;
            break;
        }
    }

    if (corLibId == 0 || sysObjectTypeDef == mdTypeDefNil)
    {
        return E_FAIL;
    }

    tstring corLibName;
    TypeNameUtilities nameUtilities(m_pCorProfilerInfo);
    nameUtilities.CacheModuleNames(m_nameCache, corLibId);

    std::shared_ptr<ModuleData> moduleData;
    if (!m_nameCache.TryGetModuleData(corLibId, moduleData))
    {
        return E_UNEXPECTED;
    }

    corLibName = moduleData->GetName();

    // Trim the .dll file extension
    const tstring dllExtension = _T(".dll");
    if (StringUtilities::EndsWithCaseInsensitive(corLibName, dllExtension))
    {
        corLibName.erase(corLibName.length() - dllExtension.length());
    }

    m_resolvedCorLibName = corLibName;
    m_resolvedCorLibId = corLibId;
    return S_OK;
}

HRESULT AssemblyProbePrep::HydrateProbeMetadata()
{
    if (m_didHydrateProbeCache)
    {
        return S_OK;
    }

    HRESULT hr;
    TypeNameUtilities typeNameUtilities(m_pCorProfilerInfo);
    IfFailRet(typeNameUtilities.CacheNames(m_nameCache, m_probeFunctionId, NULL));

    std::shared_ptr<FunctionData> probeFunctionData;
    std::shared_ptr<ModuleData> probeModuleData;
    if (!m_nameCache.TryGetFunctionData(m_probeFunctionId, probeFunctionData) ||
        !m_nameCache.TryGetModuleData(probeFunctionData->GetModuleId(), probeModuleData))
    {
        return E_UNEXPECTED;
    }

    ComPtr<IMetaDataImport> pProbeMetadataImport;
    IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
        probeFunctionData->GetModuleId(),
        ofRead,
        IID_IMetaDataImport,
        reinterpret_cast<IUnknown **>(&pProbeMetadataImport)));

    ComPtr<IMetaDataAssemblyImport> pProbeAssemblyImport;
    IfFailRet(pProbeMetadataImport->QueryInterface(IID_IMetaDataAssemblyImport, reinterpret_cast<void **>(&pProbeAssemblyImport)));
    mdAssembly probeAssemblyToken;
    IfFailRet(pProbeAssemblyImport->GetAssemblyFromScope(&probeAssemblyToken));

    const BYTE *pPublicKey;
    ULONG publicKeyLength;
    ASSEMBLYMETADATA metadata = {};
    DWORD assemblyFlags;
    WCHAR assemblyName[STRING_BUFFER_LEN];

    IfFailRet(pProbeAssemblyImport->GetAssemblyProps(
        probeAssemblyToken,
        (const void **)&pPublicKey,
        &publicKeyLength,
        nullptr,
        assemblyName,
        STRING_BUFFER_LEN,
        nullptr,
        &metadata,
        &assemblyFlags));

    m_probeCache.assemblyFlags = assemblyFlags;
    m_probeCache.assemblyMetadata = metadata;
    m_probeCache.assemblyName = tstring(assemblyName);
    m_probeCache.publicKey = vector<BYTE>(pPublicKey, pPublicKey + publicKeyLength);

    PCCOR_SIGNATURE pProbeSignature;
    ULONG signatureLength;
    IfFailRet(pProbeMetadataImport->GetMethodProps(
        probeFunctionData->GetMethodToken(),
        nullptr,
        nullptr,
        NULL,
        nullptr,
        nullptr,
        &pProbeSignature,
        &signatureLength,
        nullptr,
        nullptr));

    m_probeCache.signature = vector<BYTE>(pProbeSignature, pProbeSignature + signatureLength);

    m_didHydrateProbeCache = true;
    return S_OK;
}

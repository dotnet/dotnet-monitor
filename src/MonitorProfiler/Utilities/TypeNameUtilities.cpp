// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "TypeNameUtilities.h"
#include "corhlpr.h"

TypeNameUtilities::TypeNameUtilities(ICorProfilerInfo12* profilerInfo) : _profilerInfo(profilerInfo)
{
}

HRESULT TypeNameUtilities::CacheNames(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo, NameCache& nameCache)
{
    std::shared_ptr<FunctionData> functionData;
    if (!nameCache.GetFunctionData(functionId, functionData))
    {
        return GetFunctionInfo(nameCache, functionId, frameInfo);
    }

    return S_OK;
}

HRESULT TypeNameUtilities::GetFunctionInfo(NameCache& nameCache, FunctionID id, COR_PRF_FRAME_INFO frameInfo)
{
    if (id == 0)
    {
        return E_INVALIDARG;
    }

    ClassID classId = 0;
    ModuleID moduleId = 0;
    mdToken token = mdTokenNil;
    ULONG32 typeArgsCount = 0;
    ClassID typeArgs[32];
    HRESULT hr;

    IfFailRet(_profilerInfo->GetFunctionInfo2(id,
        frameInfo,
        &classId,
        &moduleId,
        &token,
        sizeof(typeArgs) / sizeof(ClassID),
        &typeArgsCount,
        typeArgs));

    ComPtr<IMetaDataImport> pIMDImport;
    IfFailRet(_profilerInfo->GetModuleMetaData(moduleId,
        ofRead,
        IID_IMetaDataImport,
        (IUnknown**)&pIMDImport));

    //TODO Convert this to dynamically allocate the needed size.
    WCHAR funcName[256];
    mdTypeDef classToken = mdTypeDefNil;
    IfFailRet(pIMDImport->GetMethodProps(token,
        &classToken,
        funcName,
        256,
        0,
        0,
        NULL,
        NULL,
        NULL,
        NULL));

    IfFailRet(GetModuleInfo(nameCache, moduleId));

    nameCache.AddFunctionData(moduleId, id, std::move(std::wstring(funcName)), classId, classToken, typeArgs, typeArgsCount);

    // If the ClassID returned from GetFunctionInfo is 0, then the function is a shared generic function.
    if (classId != 0)
    {
        IfFailRet(GetClassInfo(nameCache, classId));
    }
    else
    {
        IfFailRet(GetTypeDefName(nameCache, moduleId, classToken));
    }

    for (ULONG32 i = 0; i < typeArgsCount; i++)
    {
        if (typeArgs[i] != 0)
        {
            IfFailRet(GetClassInfo(nameCache, typeArgs[i]));
        }
    }

    return S_OK;
}

HRESULT TypeNameUtilities::GetClassInfo(NameCache& nameCache, ClassID classId)
{
    std::shared_ptr<ClassData> classData;
    if (nameCache.GetClassData(classId, classData))
    {
        return S_OK;
    }

    if (classId == 0)
    {
        return E_INVALIDARG;
    }

    ModuleID modId = 0;
    mdTypeDef classToken = mdTokenNil;
    ULONG32 nTypeArgs = 0;
    ClassID typeArgs[32];
    HRESULT hr = S_OK;
    ClassFlags flags = ClassFlags::None;

    tstring placeholderName;

    IfFailRet(_profilerInfo->GetClassIDInfo2(classId,
        &modId,
        &classToken,
        nullptr,
        32,
        &nTypeArgs,
        typeArgs));

    if (CORPROF_E_CLASSID_IS_ARRAY == hr)
    {
        flags = ClassFlags::Array;
    }
    else if (CORPROF_E_CLASSID_IS_COMPOSITE == hr)
    {
        // We have a composite class
        flags = ClassFlags::Composite;
    }
    else if (CORPROF_E_DATAINCOMPLETE == hr)
    {
        // type-loading is not yet complete. Cannot do anything about it.
        flags = ClassFlags::IncompleteData;
    }
    else if (FAILED(hr))
    {
        flags = ClassFlags::Error;
    }

    if (flags == ClassFlags::None)
    {
        IfFailRet(GetTypeDefName(nameCache, modId, classToken));

        for (ULONG32 i = 0; i < nTypeArgs; i++)
        {
            if (typeArgs[i] != 0)
            {
                IfFailRet(GetClassInfo(nameCache, typeArgs[i]));
            }
        }
    }

    nameCache.AddClassData(modId, classId, classToken, flags, typeArgs, nTypeArgs);

    return S_OK;
}

HRESULT TypeNameUtilities::GetTypeDefName(NameCache& nameCache, ModuleID moduleId, mdTypeDef classToken)
{
    HRESULT hr;
    ComPtr<IMetaDataImport2> pMDImport;
    IfFailRet(_profilerInfo->GetModuleMetaData(moduleId,
        (ofRead | ofWrite),
        IID_IMetaDataImport2,
        (IUnknown**)&pMDImport));

    mdToken tokenToProcess = classToken;
    while (tokenToProcess != mdTokenNil)
    {
        std::shared_ptr<TokenData> tokenData;
        if (nameCache.GetTokenData(moduleId, tokenToProcess, tokenData))
        {
            //We already processed this type (and therefore all of its outer classes)
            break;
        }

        WCHAR wName[256];
        DWORD dwTypeDefFlags = 0;
        IfFailRet(pMDImport->GetTypeDefProps(tokenToProcess,
            wName,
            256,
            NULL,
            &dwTypeDefFlags,
            NULL));

        mdTypeDef outerTokenType = mdTokenNil;
        if (IsTdNested(dwTypeDefFlags))
        {
            IfFailRet(pMDImport->GetNestedClassProps(tokenToProcess, &outerTokenType));
        }
        nameCache.AddTokenData(moduleId, tokenToProcess, outerTokenType, std::wstring(wName));
        tokenToProcess = outerTokenType;
    }

    return S_OK;
}

HRESULT TypeNameUtilities::GetModuleInfo(NameCache& nameCache, ModuleID moduleId)
{
    if (moduleId == 0)
    {
        return E_INVALIDARG;
    }

    HRESULT hr;

    std::shared_ptr<ModuleData> mod;
    if (nameCache.GetModuleData(moduleId, mod))
    {
        return S_OK;
    }

    WCHAR moduleFullName[256];
    ULONG nameLength = 0;
    AssemblyID assemID;

    IfFailRet(_profilerInfo->GetModuleInfo(moduleId,
        nullptr,
        256,
        &nameLength,
        moduleFullName,
        &assemID));

    WCHAR* ptr = nullptr;

    int index = nameLength - 1;
    while (index >= 0)
    {
        if (moduleFullName[index] == '\\' || moduleFullName[index] == '/')
        {
            break;
        }
        index--;
    }

    tstring moduleName;
    if (index < 0)
    {
        moduleName = moduleFullName;
    }
    else
    {
        moduleName = tstring(moduleFullName, index + 1, nameLength - index - 1);
    }

    nameCache.AddModuleData(moduleId, std::move(moduleName));

    return S_OK;
}

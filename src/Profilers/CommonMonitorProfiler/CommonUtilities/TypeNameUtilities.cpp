// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "TypeNameUtilities.h"
#include "corhlpr.h"

TypeNameUtilities::TypeNameUtilities(ICorProfilerInfo12* profilerInfo) : _profilerInfo(profilerInfo)
{
}

HRESULT TypeNameUtilities::CacheModuleNames(NameCache& nameCache, ModuleID moduleId)
{
    std::shared_ptr<ModuleData> moduleData;
    if (!nameCache.TryGetModuleData(moduleId, moduleData))
    {
        return GetModuleInfo(nameCache, moduleId);
    }

    return S_OK;
}

HRESULT TypeNameUtilities::CacheNames(NameCache& nameCache, ClassID classId)
{
    std::shared_ptr<ClassData> classData;
    if (!nameCache.TryGetClassData(classId, classData))
    {
        return GetClassInfo(nameCache, classId);
    }

    return S_OK;
}

HRESULT TypeNameUtilities::CacheNames(NameCache& nameCache, FunctionID functionId, COR_PRF_FRAME_INFO frameInfo)
{
    std::shared_ptr<FunctionData> functionData;
    if (!nameCache.TryGetFunctionData(functionId, functionData))
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

    bool stackTraceHidden = ShouldHideFromStackTrace(moduleId, token);

    nameCache.AddFunctionData(moduleId, id, tstring(funcName), classId, token, classToken, typeArgs, typeArgsCount, stackTraceHidden);

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
    if (classId == 0)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<ClassData> classData;
    if (nameCache.TryGetClassData(classId, classData))
    {
        return S_OK;
    }

    ModuleID modId = 0;
    mdTypeDef classToken = mdTokenNil;
    ULONG32 typeArgsCount = 0;
    ClassID typeArgs[32];
    HRESULT hr = S_OK;
    ClassFlags flags = ClassFlags::None;

    IfFailRet(_profilerInfo->GetClassIDInfo2(classId,
        &modId,
        &classToken,
        nullptr,
        32,
        &typeArgsCount,
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

        for (ULONG32 i = 0; i < typeArgsCount; i++)
        {
            if (typeArgs[i] != 0)
            {
                IfFailRet(GetClassInfo(nameCache, typeArgs[i]));
            }
        }
    }

    bool stackTraceHidden = ShouldHideFromStackTrace(modId, classToken);

    nameCache.AddClassData(modId, classId, classToken, flags, typeArgs, typeArgsCount, stackTraceHidden);

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
        if (nameCache.TryGetTokenData(moduleId, tokenToProcess, tokenData))
        {
            //We already processed this type (and therefore all of its outer classes)
            break;
        }

        bool stackTraceHidden = ShouldHideFromStackTrace(moduleId, tokenToProcess);

        WCHAR wName[256];

        DWORD dwTypeDefFlags = 0;
        IfFailRet(pMDImport->GetTypeDefProps(tokenToProcess,
            wName,
            256,
            NULL,
            &dwTypeDefFlags,
            NULL));

        mdTypeDef outerTokenType = mdTokenNil;

        tstring wNameString = tstring(wName);
        tstring wNamespaceString = tstring();

        if (IsTdNested(dwTypeDefFlags))
        {
            IfFailRet(pMDImport->GetNestedClassProps(tokenToProcess, &outerTokenType));
        }
        else
        {
            std::string::size_type found = tstring(wName).find_last_of(_T('.'));

            if (found != std::string::npos)
            {
                wNamespaceString = wNameString.substr(0, found);
                wNameString = wNameString.substr(found + 1);
            }
        }
        nameCache.AddTokenData(moduleId, tokenToProcess, outerTokenType, tstring(wNameString), tstring(wNamespaceString), stackTraceHidden);
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
    if (nameCache.TryGetModuleData(moduleId, mod))
    {
        return S_OK;
    }

    ComPtr<IMetaDataImport> pIMDImport;
    IfFailRet(_profilerInfo->GetModuleMetaData(moduleId,
        ofRead,
        IID_IMetaDataImport,
        (IUnknown**)&pIMDImport));

    WCHAR moduleFullName[256];
    ULONG nameLength = 0;
    GUID mvid = {0};
    IfFailRet(pIMDImport->GetScopeProps(
        moduleFullName,
        256,
        &nameLength,
        &mvid));

    int pathSeparatorIndex = nameLength - 1;
    while (pathSeparatorIndex >= 0)
    {
        if (moduleFullName[pathSeparatorIndex] == '\\' || moduleFullName[pathSeparatorIndex] == '/')
        {
            break;
        }
        pathSeparatorIndex--;
    }

    tstring moduleName;
    if (pathSeparatorIndex < 0)
    {
        moduleName = moduleFullName;
    }
    else
    {
        moduleName = tstring(moduleFullName, pathSeparatorIndex + 1, nameLength - pathSeparatorIndex - 1);
    }

    nameCache.AddModuleData(moduleId, std::move(moduleName), mvid);

    return S_OK;
}

bool TypeNameUtilities::ShouldHideFromStackTrace(ModuleID moduleId, mdToken token)
{
    bool hasAttribute = false;
    if (HasStackTraceHiddenAttribute(moduleId, token, hasAttribute) != S_OK) {
        // When encountering an error while checking for the attribute show the frame.
        return false;
    }

    return hasAttribute;
}

HRESULT TypeNameUtilities::HasStackTraceHiddenAttribute(ModuleID moduleId, mdToken token, bool& hasAttribute)
{
    HRESULT hr;
    hasAttribute = false;

    ComPtr<IMetaDataImport> pIMDImport;
    IfFailRet(_profilerInfo->GetModuleMetaData(moduleId,
        ofRead,
        IID_IMetaDataImport,
        (IUnknown**)&pIMDImport));

    // GetCustomAttributeByName will return S_FALSE if the attribute is not found.
    IfFailRet(pIMDImport->GetCustomAttributeByName(
        token,
        _T("System.Diagnostics.StackTraceHiddenAttribute"),
        nullptr,
        nullptr));

    hasAttribute = (hr == S_OK);

    return S_OK;
}

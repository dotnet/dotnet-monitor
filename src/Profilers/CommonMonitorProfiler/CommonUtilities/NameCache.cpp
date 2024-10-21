// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "NameCache.h"
#include "macros.h"
#include "corhlpr.h"

const tstring NameCache::CompositeClassName = _T("_CompositeClass_");
const tstring NameCache::ArrayClassName = _T("_ArrayClass_");
const tstring NameCache::UnknownName = _T("_Unknown_");
const tstring NameCache::ModuleSeparator = _T("!");
const tstring NameCache::FunctionSeparator = _T(".");
const tstring NameCache::NamespaceSeparator = _T(".");
const tstring NameCache::NestedSeparator = _T("+");
const tstring NameCache::GenericBegin = _T("<");
const tstring NameCache::GenericSeparator = _T(",");
const tstring NameCache::GenericEnd = _T(">");

bool NameCache::TryGetFunctionData(FunctionID id, std::shared_ptr<FunctionData>& data)
{
    return GetData(_functionNames, id, data);
}
bool NameCache::TryGetClassData(ClassID id, std::shared_ptr<ClassData>& data)
{
    return GetData(_classNames, id, data);
}
bool NameCache::TryGetModuleData(ModuleID id, std::shared_ptr<ModuleData>& data)
{
    return GetData(_moduleNames, id, data);
}
bool NameCache::TryGetTokenData(ModuleID modId, mdTypeDef token, std::shared_ptr<TokenData>& data)
{
    auto const& it = _names.find(std::make_pair(modId, token));

    if (it != _names.end())
    {
        data = it->second;
        return true;
    }
    return false;
}

void NameCache::AddModuleData(ModuleID moduleId, tstring&& name, GUID mvid)
{
    _moduleNames.emplace(moduleId, std::make_shared<ModuleData>(std::move(name), mvid));
}

HRESULT NameCache::GetFullyQualifiedName(FunctionID id, tstring& name)
{
    HRESULT hr;

    if (id == 0)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<FunctionData> functionData;
    if (!TryGetFunctionData(id, functionData))
    {
        return E_NOT_SET;
    }

    //TODO Consider making each naming function append to a sstream and consider adding flags to disable certain parts of the name such as the module.
    // Currently some functions append name information while others assign it.
    if (functionData->GetClass() != 0)
    {
        IfFailRet(GetFullyQualifiedTypeName(functionData->GetClass(), name));
    }
    else
    {
        IfFailRet(GetFullyQualifiedTypeName(functionData->GetModuleId(), functionData->GetClassToken(), name));
    }

    name += FunctionSeparator + functionData->GetName();

    IfFailRet(GetGenericParameterNames(functionData->GetTypeArgs(), name));

    std::shared_ptr<ModuleData> moduleData;
    if (TryGetModuleData(functionData->GetModuleId(), moduleData))
    {
        name = moduleData->GetName() + ModuleSeparator + name;
    }

    return S_OK;
}

HRESULT NameCache::GetFullyQualifiedTypeName(ClassID classId, tstring& name)
{
    HRESULT hr;

    if (classId == 0)
    {
        return E_INVALIDARG;
    }

    std::shared_ptr<ClassData> classData;
    if (!TryGetClassData(classId, classData))
    {
        return E_NOT_SET;
    }

    switch (classData->GetFlags())
    {
        case ClassFlags::None:
            IfFailRet(GetFullyQualifiedTypeName(classData->GetModuleId(), classData->GetToken(), name));
            break;
        case ClassFlags::Array:
            name = ArrayClassName;
            break;
        case ClassFlags::Composite:
            name = CompositeClassName;
            break;
        case ClassFlags::IncompleteData:
        case ClassFlags::Error:
        default:
            name = UnknownName;
            break;
    }

    IfFailRet(GetGenericParameterNames(classData->GetTypeArgs(), name));

    return S_OK;
}

HRESULT NameCache::GetFullyQualifiedTypeName(ModuleID moduleId, mdTypeDef token, tstring& name)
{
    while (token != 0)
    {
        std::shared_ptr<TokenData> tokenData;
        if (TryGetTokenData(moduleId, token, tokenData))
        {
            if (name.size() > 0)
            {
                name = NestedSeparator + name;
            }
            name = tokenData->GetNamespace() + NamespaceSeparator + tokenData->GetName() + name;
            token = tokenData->GetOuterToken();
        }
        else
        {
            token = 0;
        }
    }

    return S_OK;
}

HRESULT NameCache::GetGenericParameterNames(const std::vector<UINT64>& typeArgs, tstring& name)
{
    HRESULT hr;

    for (size_t i = 0; i < typeArgs.size(); i++)
    {
        if (i == 0)
        {
            name += GenericBegin;
        }

        tstring genericParamName;
        IfFailRet(GetFullyQualifiedTypeName(static_cast<ClassID>(typeArgs[i]), genericParamName));
        name += genericParamName;

        if (i == (typeArgs.size() - 1))
        {
            name += GenericEnd;
        }
        else
        {
            name += GenericSeparator;
        }
    }

    return S_OK;
}

const std::unordered_map<ClassID, std::shared_ptr<ClassData>>& NameCache::GetClasses()
{
    return _classNames;
}

const std::unordered_map<FunctionID, std::shared_ptr<FunctionData>>& NameCache::GetFunctions()
{
    return _functionNames;
}

const std::unordered_map<ModuleID, std::shared_ptr<ModuleData>>& NameCache::GetModules()
{
    return _moduleNames;
}

const std::unordered_map<std::pair<ModuleID, mdTypeDef>, std::shared_ptr<TokenData>, PairHash<ModuleID, mdTypeDef>>& NameCache::GetTypeNames()
{
    return _names;
}

void NameCache::AddFunctionData(ModuleID moduleId, FunctionID id, tstring&& name, ClassID parent, mdToken methodToken, mdTypeDef parentToken, ClassID* typeArgs, int typeArgsCount, bool stackTraceHidden)
{
    std::shared_ptr<FunctionData> functionData = std::make_shared<FunctionData>(moduleId, parent, std::move(name), methodToken, parentToken, stackTraceHidden);
    for (int i = 0; i < typeArgsCount; i++)
    {
        functionData->AddTypeArg(typeArgs[i]);
    }
    _functionNames.emplace(id, functionData);
}

void NameCache::AddClassData(ModuleID moduleId, ClassID id, mdTypeDef typeDef, ClassFlags flags, ClassID* typeArgs, int typeArgsCount, bool stackTraceHidden)
{
    std::shared_ptr<ClassData> classData = std::make_shared<ClassData>(moduleId, typeDef, flags, stackTraceHidden);
    for (int i = 0; i < typeArgsCount; i++)
    {
        classData->AddTypeArg(typeArgs[i]);
    }
    _classNames.emplace(id, classData);
}

void NameCache::AddTokenData(ModuleID moduleId, mdTypeDef typeDef, mdTypeDef outerToken, tstring&& name, tstring&& Namespace, bool stackTraceHidden)
{
    std::shared_ptr<TokenData> tokenData = std::make_shared<TokenData>(std::move(name), std::move(Namespace), outerToken, stackTraceHidden);

    _names.emplace(std::make_pair(moduleId, typeDef), tokenData);
}

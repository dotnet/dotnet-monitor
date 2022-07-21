// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "NameCache.h"
#include "macros.h"
#include "corhlpr.h"

const tstring NameCache::CompositeClassName = _T("_CompositeClass_");
const tstring NameCache::ArrayClassName = _T("_ArrayClass_");
const tstring NameCache::UnknownName = _T("_Unknown_");
const tstring NameCache::ModuleSeparator = _T("!");
const tstring NameCache::FunctionSeperator = _T(".");
const tstring NameCache::NestedSeparator = _T("+");
const tstring NameCache::GenericBegin = _T("<");
const tstring NameCache::GenericSeparator = _T(",");
const tstring NameCache::GenericEnd = _T(">");

bool NameCache::GetFunctionData(FunctionID id, std::shared_ptr<FunctionData>& data)
{
    return GetData(_functionNames, id, data);
}
bool NameCache::GetClassData(ClassID id, std::shared_ptr<ClassData>& data)
{
    return GetData(_classNames, id, data);
}
bool NameCache::GetModuleData(ModuleID id, std::shared_ptr<ModuleData>& data)
{
    return GetData(_moduleNames, id, data);
}
bool NameCache::GetTokenData(ModuleID modId, mdTypeDef token, std::shared_ptr<TokenData>& data)
{
    auto const& it = _names.find(std::make_pair(modId, token));

    if (it != _names.end())
    {
        data = it->second;
        return true;
    }
    return false;
}

void NameCache::AddModuleData(ModuleID moduleId, tstring&& name)
{
    _moduleNames.emplace(moduleId, std::make_shared<ModuleData>(std::move(name)));
}

HRESULT NameCache::GetFullyQualifiedName(FunctionID id, tstring& name)
{
    HRESULT hr;

    std::shared_ptr<FunctionData> functionData;
    if (GetFunctionData(id, functionData))
    {
        std::shared_ptr<ClassData> classData;
        if (functionData->GetClass() != 0)
        {
            IfFailRet(GetFullyQualifiedClassName(functionData->GetClass(), name));
        }
        else
        {
            IfFailRet(GetFullyQualifiedClassName(functionData->GetModuleId(), functionData->GetClassToken(), name));
        }

        name += FunctionSeperator + functionData->GetName();

        for (int i = 0; i < functionData->GetTypeArgs().size(); i++)
        {
            if (i == 0)
            {
                name += GenericBegin;
            }

            tstring genericParamName;
            IfFailRet(GetFullyQualifiedClassName(functionData->GetTypeArgs()[i], genericParamName));
            name += genericParamName;

            if (i == (functionData->GetTypeArgs().size() - 1))
            {
                name += GenericEnd;
            }
            else
            {
                name += GenericSeparator;
            }
        }

        std::shared_ptr<ModuleData> moduleData;
        if (GetModuleData(functionData->GetModuleId(), moduleData))
        {
            name = moduleData->GetName() + ModuleSeparator + name;
        }
    }

    return S_OK;
}

HRESULT NameCache::GetFullyQualifiedClassName(ClassID classId, tstring& name)
{
    HRESULT hr;

    std::shared_ptr<ClassData> classData;
    if (classId != 0 && GetClassData(classId, classData))
    {
        mdTypeDef token = classData->GetToken();

        if (classData->GetFlags() == ClassFlags::None)
        {
            IfFailRet(GetFullyQualifiedClassName(classData->GetModuleId(), classData->GetToken(), name));
        }
        else
        {
            switch (classData->GetFlags())
            {
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
        }

        for (int i = 0; i < classData->GetTypeArgs().size(); i++)
        {
            if (i == 0)
            {
                name += GenericBegin;
            }

            tstring genericParamName;
            IfFailRet(GetFullyQualifiedClassName(classData->GetTypeArgs()[i], genericParamName));
            name += genericParamName;

            if (i == (classData->GetTypeArgs().size() - 1))
            {
                name += GenericEnd;
            }
            else
            {
                name += GenericSeparator;
            }
        }
    }

    return S_OK;
}

HRESULT NameCache::GetFullyQualifiedClassName(ModuleID moduleId, mdTypeDef token, tstring& name)
{
    while (token != 0)
    {
        std::shared_ptr<TokenData> tokenData;
        if (GetTokenData(moduleId, token, tokenData))
        {
            if (name.size() > 0)
            {
                name = NestedSeparator + name;
            }
            name = tokenData->GetName() + name;
            token = tokenData->GetOuterToken();
        }
        else
        {
            token = 0;
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

void NameCache::AddFunctionData(ModuleID moduleId, FunctionID id, tstring&& name, ClassID parent, mdTypeDef parentToken, ClassID* typeArgs, int typeArgsCount)
{
    std::shared_ptr<FunctionData> functionData = std::make_shared<FunctionData>(moduleId, parent, std::move(name), parentToken);
    for (int i = 0; i < typeArgsCount; i++)
    {
        functionData->AddTypeArg(typeArgs[i]);
    }
    _functionNames.emplace(id, functionData);
}

void NameCache::AddClassData(ModuleID moduleId, ClassID id, mdTypeDef typeDef, ClassFlags flags, ClassID* typeArgs, int typeArgsCount)
{
    std::shared_ptr<ClassData> classData = std::make_shared<ClassData>(moduleId, typeDef, flags);
    for (int i = 0; i < typeArgsCount; i++)
    {
        classData->AddTypeArg(typeArgs[i]);
    }
    _classNames.emplace(id, classData);
}

void NameCache::AddTokenData(ModuleID moduleId, mdTypeDef typeDef, mdTypeDef outerToken, tstring&& name)
{
    std::shared_ptr<TokenData> tokenData = std::make_shared<TokenData>(std::move(name), outerToken);

    _names.emplace(std::make_pair(moduleId, typeDef), tokenData);
}

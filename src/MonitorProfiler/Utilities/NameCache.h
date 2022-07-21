// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "tstring.h"
#include "ClrData.h"
#include "PairHash.h"
#include <memory>
#include <functional>
#include <unordered_map>
#include <vector>

/// <summary>
/// Stores mappings between Clr objects and their names.
/// </summary>
class NameCache
{
public:
    bool GetFunctionData(FunctionID id, std::shared_ptr<FunctionData>& data);
    bool GetClassData(ClassID id, std::shared_ptr<ClassData>& data);
    bool GetModuleData(ModuleID id, std::shared_ptr<ModuleData>& data);
    bool GetTokenData(ModuleID modId, mdTypeDef token, std::shared_ptr<TokenData>& data);

    void AddModuleData(ModuleID moduleId, tstring&& name);
    void AddFunctionData(ModuleID moduleId, FunctionID id, tstring&& name, ClassID parent, mdTypeDef parentToken, ClassID* typeArgs, int typeArgsCount);
    void AddClassData(ModuleID moduleId, ClassID id, mdTypeDef typeDef, ClassFlags flags, ClassID* typeArgs, int typeArgsCount);
    void AddTokenData(ModuleID moduleId, mdTypeDef typeDef, mdTypeDef outerToken, tstring&& name);

    HRESULT GetFullyQualifiedName(FunctionID id, tstring& name);
    HRESULT GetFullyQualifiedClassName(ClassID classId, tstring& name);
    HRESULT GetFullyQualifiedClassName(ModuleID moduleId, mdTypeDef token, tstring& name);

    const std::unordered_map<ClassID, std::shared_ptr<ClassData>>& GetClasses();
    const std::unordered_map<FunctionID, std::shared_ptr<FunctionData>>& GetFunctions();
    const std::unordered_map<ModuleID, std::shared_ptr<ModuleData>>& GetModules();
    const std::unordered_map<std::pair<ModuleID, mdTypeDef>, std::shared_ptr<TokenData>, PairHash<ModuleID, mdTypeDef>>& GetTypeNames();

private:
    static const tstring CompositeClassName;
    static const tstring ArrayClassName;
    static const tstring UnknownName;
    static const tstring ModuleSeparator;
    static const tstring FunctionSeperator;
    static const tstring NestedSeparator;
    static const tstring GenericBegin;
    static const tstring GenericSeparator;
    static const tstring GenericEnd;

    template<typename T, typename U>
    bool GetData(std::unordered_map<T, std::shared_ptr<U>> map, T id, std::shared_ptr<U>& name);

    std::unordered_map<ClassID, std::shared_ptr<ClassData>> _classNames;
    std::unordered_map<FunctionID, std::shared_ptr<FunctionData>> _functionNames;
    std::unordered_map<ModuleID, std::shared_ptr<ModuleData>> _moduleNames;
    std::unordered_map<std::pair<ModuleID, mdTypeDef>, std::shared_ptr<TokenData>, PairHash<ModuleID, mdTypeDef>> _names;
};

template<typename T, typename U>
bool NameCache::GetData(std::unordered_map<T, std::shared_ptr<U>> map, T id, std::shared_ptr<U>& name)
{
    std::unordered_map<T, std::shared_ptr<U>>::iterator it = map.find(id);

    if (it != map.end())
    {
        name = it->second;
        return true;
    }
    return false;
}
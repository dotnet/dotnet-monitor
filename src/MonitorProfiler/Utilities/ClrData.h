// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <vector>
#include "cor.h"
#include "corprof.h"
#include "tstring.h"

class ModuleData
{
public:
    ModuleData(tstring&& name) :
        _moduleName(name)
    {
    }

    const tstring& GetName() const { return _moduleName; }

private:
    tstring _moduleName;
};

enum class ClassFlags : UINT32
{
    None = 0,
    Array,
    Composite,
    IncompleteData,
    Error = 0xff
};

class ClassData
{
public:
    ClassData(ModuleID moduleId, mdTypeDef token, ClassFlags flags) :
        _moduleId(moduleId), _token(token), _flags(flags)
    {
    }

    const ModuleID GetModuleId() const { return _moduleId; }
    const mdTypeDef GetToken() const { return _token; }
    const ClassFlags GetFlags() const { return _flags; }
    const std::vector<ClassID>& GetTypeArgs() const { return _typeArgs; }
    void AddTypeArg(ClassID id) { _typeArgs.push_back(id); }

private:
    ModuleID _moduleId;
    mdTypeDef _token;
    ClassFlags _flags;
    std::vector<ClassID> _typeArgs;
};

class TokenData
{
public:
    TokenData(tstring&& name, mdTypeDef outerClass) : _name(name), _outerClass(outerClass)
    {
    }

    const tstring& GetName() const { return _name; }
    const mdTypeDef& GetOuterToken() const { return _outerClass; }
private:
    tstring _name;
    mdTypeDef _outerClass;
};

class FunctionData
{
public:
    FunctionData(ModuleID moduleId, ClassID containingClass, tstring&& name, mdTypeDef classToken) :
        _moduleId(moduleId), _class(containingClass), _functionName(name), _classToken(classToken)
    {
    }

    const ModuleID GetModuleId() const { return _moduleId; }
    const tstring& GetName() const { return _functionName; }
    const ClassID GetClass() const { return _class; }
    const mdTypeDef GetClassToken() const { return _classToken; }
    const std::vector<ClassID>& GetTypeArgs() const { return _typeArgs; }
    void AddTypeArg(ClassID classID) { _typeArgs.push_back(classID); }

private:
    ModuleID _moduleId;
    ClassID _class;
    tstring _functionName;
    mdTypeDef _classToken;
    std::vector<ClassID> _typeArgs;
};
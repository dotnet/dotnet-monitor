// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>
#include "cor.h"
#include "corprof.h"
#include "tstring.h"

class ModuleData
{
public:
    ModuleData(tstring&& name, GUID mvid) :
        _moduleName(name), _mvid(mvid)
    {
    }

    const tstring& GetName() const { return _moduleName; }
    const GUID GetMvid() const { return _mvid; }

private:
    tstring _moduleName;
    GUID _mvid;
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
    ClassData(ModuleID moduleId, mdTypeDef token, ClassFlags flags, bool stackTraceHidden) :
        _moduleId(moduleId), _token(token), _flags(flags), _stackTraceHidden(stackTraceHidden)
    {
    }

    const ModuleID GetModuleId() const { return _moduleId; }
    const mdTypeDef GetToken() const { return _token; }
    const ClassFlags GetFlags() const { return _flags; }
    const bool GetStackTraceHidden() const { return _stackTraceHidden; }
    const std::vector<UINT64>& GetTypeArgs() const { return _typeArgs; }
    void AddTypeArg(ClassID id) { _typeArgs.push_back(static_cast<UINT64>(id)); }

private:
    ModuleID _moduleId;
    mdTypeDef _token;
    ClassFlags _flags;
    bool _stackTraceHidden;
    std::vector<UINT64> _typeArgs;
};

class TokenData
{
public:
    TokenData(tstring&& name, tstring&& Namespace, mdTypeDef outerClass, bool stackTraceHidden) :
        _name(name), _namespace(Namespace), _outerClass(outerClass), _stackTraceHidden(stackTraceHidden)
    {
    }

    const tstring& GetName() const { return _name; }
    const tstring& GetNamespace() const { return _namespace; }
    const mdTypeDef& GetOuterToken() const { return _outerClass; }
    const bool GetStackTraceHidden() const { return _stackTraceHidden; }
private:
    tstring _name;
    tstring _namespace;
    mdTypeDef _outerClass;
    bool _stackTraceHidden;
};

class FunctionData
{
public:
    FunctionData(ModuleID moduleId, ClassID containingClass, tstring&& name, mdToken methodToken, mdTypeDef classToken, bool stackTraceHidden) :
        _moduleId(moduleId), _class(containingClass), _functionName(name), _methodToken(methodToken), _classToken(classToken), _stackTraceHidden(stackTraceHidden)
    {
    }

    const ModuleID GetModuleId() const { return _moduleId; }
    const tstring& GetName() const { return _functionName; }
    const ClassID GetClass() const { return _class; }
    const mdToken GetMethodToken() const { return _methodToken; }
    const mdTypeDef GetClassToken() const { return _classToken; }
    const bool GetStackTraceHidden() const { return _stackTraceHidden; }
    const std::vector<UINT64>& GetTypeArgs() const { return _typeArgs; }
    const std::vector<UINT64>& GetParameterTypes() const { return _parameterTypes; }
    void AddTypeArg(ClassID classID) { _typeArgs.push_back(static_cast<UINT64>(classID)); }

private:
    ModuleID _moduleId;
    ClassID _class;
    tstring _functionName;
    mdToken _methodToken;
    mdTypeDef _classToken;
    bool _stackTraceHidden;
    std::vector<UINT64> _typeArgs;
    std::vector<UINT64> _parameterTypes;
};

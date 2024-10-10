// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "tstring.h"
#include "NameCache.h"

/// <summary>
/// Retrieves the names of functions and stores them into a cache.
/// </summary>
class TypeNameUtilities
{
    public:
        TypeNameUtilities(ICorProfilerInfo12* profilerInfo);
        HRESULT CacheNames(NameCache& nameCache, ClassID classId);
        HRESULT CacheNames(NameCache& nameCache, FunctionID functionId, COR_PRF_FRAME_INFO frameInfo);
        HRESULT CacheModuleNames(NameCache& nameCache, ModuleID moduleId);
    private:
        HRESULT GetFunctionInfo(NameCache& nameCache, FunctionID id, COR_PRF_FRAME_INFO frameInfo);
        HRESULT GetClassInfo(NameCache& nameCache, ClassID classId);
        HRESULT GetModuleInfo(NameCache& nameCache, ModuleID moduleId);
        HRESULT GetTypeDefName(NameCache& nameCache, ModuleID moduleId, mdTypeDef classToken);
        HRESULT HasStackTraceHiddenAttribute(ModuleID moduleId, mdToken token, bool& hasAttribute);
        // A wrapper around HasStackTraceHiddenAttribute to ensure consistent behavior when checking for the attribute
        // encounters errors.
        bool ShouldHideFromStackTrace(ModuleID moduleId, mdToken token);
    private:
        ComPtr<ICorProfilerInfo12> _profilerInfo;
};

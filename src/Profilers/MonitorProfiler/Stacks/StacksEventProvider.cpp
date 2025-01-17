// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "StacksEventProvider.h"
#include "corhlpr.h"
#include "cor.h"

const WCHAR* StacksEventProvider::ProviderName = _T("DotnetMonitorStacksEventProvider");

HRESULT StacksEventProvider::CreateProvider(ICorProfilerInfo12* profilerInfo, std::unique_ptr<StacksEventProvider>& eventProvider)
{
    std::unique_ptr<ProfilerEventProvider> provider;
    HRESULT hr;

    IfFailRet(ProfilerEventProvider::CreateProvider(ProviderName, profilerInfo, provider));

    eventProvider = std::unique_ptr<StacksEventProvider>(new StacksEventProvider(profilerInfo, provider));
    IfFailRet(eventProvider->DefineEvents());

    return S_OK;
}

HRESULT StacksEventProvider::DefineEvents()
{
    HRESULT hr;

    IfFailRet(_provider->DefineEvent(_T("Callstack"), _callstackEvent, CallstackPayloads));
    IfFailRet(_provider->DefineEvent(_T("FunctionDesc"), _functionEvent, FunctionPayloads));
    IfFailRet(_provider->DefineEvent(_T("ClassDesc"), _classEvent, ClassPayloads));
    IfFailRet(_provider->DefineEvent(_T("ModuleDesc"), _moduleEvent, ModulePayloads));
    IfFailRet(_provider->DefineEvent(_T("TokenDesc"), _tokenEvent, TokenPayloads));
    IfFailRet(_provider->DefineEvent(_T("End"), _endEvent, EndPayloads));

    return S_OK;
}

HRESULT StacksEventProvider::WriteCallstack(const Stack& stack)
{
    return _callstackEvent->WritePayload(stack.GetThreadId(), stack.GetName(), stack.GetFunctionIds(), stack.GetOffsets());
}

HRESULT StacksEventProvider::WriteClassData(ClassID classId, const ClassData& classData)
{
    return _classEvent->WritePayload(
        static_cast<UINT64>(classId),
        static_cast<UINT64>(classData.GetModuleId()),
        classData.GetToken(),
        static_cast<UINT32>(classData.GetFlags()),
        classData.GetStackTraceHidden(),
        classData.GetTypeArgs());
}

HRESULT StacksEventProvider::WriteFunctionData(FunctionID functionId, const FunctionData& functionData)
{
    return _functionEvent->WritePayload(
        static_cast<UINT64>(functionId),
        functionData.GetMethodToken(),
        static_cast<UINT64>(functionData.GetClass()),
        functionData.GetClassToken(),
        static_cast<UINT64>(functionData.GetModuleId()),
        functionData.GetStackTraceHidden(),
        functionData.GetName(),
        functionData.GetTypeArgs(),
        functionData.GetParameterTypes());
}

HRESULT StacksEventProvider::WriteModuleData(ModuleID moduleId, const ModuleData& moduleData)
{
    return _moduleEvent->WritePayload(
        moduleId,
        moduleData.GetMvid(),
        moduleData.GetName());
}

HRESULT StacksEventProvider::WriteTokenData(ModuleID moduleId, mdTypeDef typeDef, const TokenData& tokenData)
{
    return _tokenEvent->WritePayload(
        moduleId,
        typeDef,
        tokenData.GetOuterToken(),
        tokenData.GetStackTraceHidden(),
        tokenData.GetName(),
        tokenData.GetNamespace());
}

HRESULT StacksEventProvider::WriteEndEvent()
{
    return _endEvent->WritePayload(0);
}

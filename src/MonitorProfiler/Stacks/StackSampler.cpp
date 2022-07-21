// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "StackSampler.h"
#include "corhlpr.h"
#include "Stack.h"
#include <functional>
#include <memory>
#include "../Utilities/TypeNameUtilities.h"

StackSamplerState::StackSamplerState(ICorProfilerInfo12* profilerInfo, std::shared_ptr<NameCache> nameCache)
    : _profilerInfo(profilerInfo), _nameCache(nameCache)
{
}

Stack& StackSamplerState::GetStack()
{
    return _stack;
}

std::shared_ptr<NameCache> StackSamplerState::GetNameCache()
{
    return _nameCache;
}

ICorProfilerInfo12* StackSamplerState::GetProfilerInfo()
{
    return _profilerInfo;
}

StackSampler::StackSampler(ICorProfilerInfo12* profilerInfo) : _profilerInfo(profilerInfo)
{
}

void StackSampler::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_ENABLE_STACK_SNAPSHOT;
}

HRESULT StackSampler::CreateCallstack(std::vector<std::unique_ptr<StackSamplerState>>& stackStates, std::shared_ptr<NameCache>& nameCache)
{
    HRESULT hr;

    IfFailRet(_profilerInfo->SuspendRuntime());
    auto resumeRuntime = [](ICorProfilerInfo12* profilerInfo) { profilerInfo->ResumeRuntime(); };
    std::unique_ptr<ICorProfilerInfo12, decltype(resumeRuntime)> resumeRuntimeHandle(static_cast<ICorProfilerInfo12*>(_profilerInfo), resumeRuntime);

    ComPtr<ICorProfilerThreadEnum> threadEnum = nullptr;
    IfFailRet(_profilerInfo->EnumThreads(&threadEnum));

    ThreadID threadID;
    ULONG numReturned;

    if (nameCache == nullptr)
    {
        nameCache = std::make_shared<NameCache>();
    }

    while ((hr = threadEnum->Next(1, &threadID, &numReturned)) == S_OK)
    {
        std::unique_ptr<StackSamplerState> stackState = std::unique_ptr<StackSamplerState>(new StackSamplerState(_profilerInfo, nameCache));
        stackState->GetStack().SetThreadId(threadID);

        //TODO According to docs, need to block ThreadDestroyed while stack walking. Is this still a  requirement?
        hr = _profilerInfo->DoStackSnapshot(threadID, DoStackSnapshotStackSnapShotCallbackWrapper, COR_PRF_SNAPSHOT_REGISTER_CONTEXT, stackState.get(), nullptr, 0);

        stackStates.push_back(std::move(stackState));
    }

    return S_OK;
}

HRESULT __stdcall StackSampler::DoStackSnapshotStackSnapShotCallbackWrapper(FunctionID funcId, UINT_PTR ip, COR_PRF_FRAME_INFO frameInfo, ULONG32 contextSize, BYTE context[], void* clientData)
{
    HRESULT hr;

    StackSamplerState* state = reinterpret_cast<StackSamplerState*>(clientData);
    Stack& stack = state->GetStack();
    stack.AddFrame(funcId, ip);

    std::shared_ptr<NameCache> nameCache = state->GetNameCache();
    TypeNameUtilities nameUtilities(state->GetProfilerInfo());
    IfFailRet(nameUtilities.CacheNames(funcId, frameInfo, *nameCache));

    return S_OK;
}

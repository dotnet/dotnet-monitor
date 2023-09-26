// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "tstring.h"
#include "Stack.h"
#include "CommonUtilities/NameCache.h"
#include "CommonUtilities/ThreadNameCache.h"

class StackSamplerState
{
    public:
        StackSamplerState(ICorProfilerInfo12* profilerInfo, std::shared_ptr<NameCache> nameCache);
        Stack& GetStack();
        std::shared_ptr<NameCache> GetNameCache();
        ICorProfilerInfo12* GetProfilerInfo();
    private:
        ComPtr<ICorProfilerInfo12> _profilerInfo;
        Stack _stack;
        std::shared_ptr<NameCache> _nameCache;
};

class StackSampler
{
    public:
        StackSampler(ICorProfilerInfo12* profilerInfo);
        HRESULT CreateCallstack(std::vector<std::unique_ptr<StackSamplerState>>& stackStates,
            std::shared_ptr<NameCache>& nameCache,
            std::shared_ptr<ThreadNameCache>& threadNames);
        static void AddProfilerEventMask(DWORD& eventsLow);
    private:
        static HRESULT __stdcall DoStackSnapshotCallbackWrapper(
            FunctionID functionId,
            UINT_PTR ip,
            COR_PRF_FRAME_INFO frameInfo,
            ULONG32 contextSize,
            BYTE context[],
            void* clientData);

        ComPtr<ICorProfilerInfo12> _profilerInfo;
};

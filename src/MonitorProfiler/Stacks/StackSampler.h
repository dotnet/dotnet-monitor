// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "tstring.h"
#include "Stack.h"
#include "../Utilities/NameCache.h"

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
        HRESULT CreateCallstack(std::vector<std::unique_ptr<StackSamplerState>>& stackStates, std::shared_ptr<NameCache>& nameCache);
        static void AddProfilerEventMask(DWORD& eventsLow);
    private:
        static HRESULT __stdcall DoStackSnapshotStackSnapShotCallbackWrapper(
            FunctionID funcId,
            UINT_PTR ip,
            COR_PRF_FRAME_INFO frameInfo,
            ULONG32 contextSize,
            BYTE context[],
            void* clientData);

        ComPtr<ICorProfilerInfo12> _profilerInfo;
};
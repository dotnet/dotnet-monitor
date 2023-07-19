// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "corhlpr.h"
#include "ProfilerEvent.h"
#include <memory>

class ProfilerEventProvider
{
    public:
        static HRESULT CreateProvider(const WCHAR* providerName, ICorProfilerInfo12* profilerInfo, std::unique_ptr<ProfilerEventProvider>& provider);

        template<typename... TArgs>
        HRESULT DefineEvent(const WCHAR* eventName, std::unique_ptr<ProfilerEvent<TArgs...>>& profilerEventDescriptor, const WCHAR* (&names)[sizeof...(TArgs)]);

        EVENTPIPE_PROVIDER GetProvider() { return _provider; }

    private:
        ProfilerEventProvider(ICorProfilerInfo12* profilerInfo, EVENTPIPE_PROVIDER provider);
        EVENTPIPE_PROVIDER _provider = 0;
        int _currentEventId = 1;
        ComPtr<ICorProfilerInfo12> _profilerInfo;
};

template<typename... TArgs>
HRESULT ProfilerEventProvider::DefineEvent(const WCHAR* eventName, typename std::unique_ptr<ProfilerEvent<TArgs...>>& profilerEventDescriptor, const WCHAR* (&names)[sizeof...(TArgs)])
{
    EVENTPIPE_EVENT event = 0;
    HRESULT hr;

    auto newEvent = typename std::unique_ptr<ProfilerEvent<TArgs...>>(new ProfilerEvent<TArgs...>(_profilerInfo));
    hr = newEvent->template Initialize<0, TArgs...>(names);
    IfFailRet(hr);

    IfFailRet(_profilerInfo->EventPipeDefineEvent(
        _provider,
        eventName,
        _currentEventId,
        0, //We not use keywords
        1, // eventVersion
        COR_PRF_EVENTPIPE_LOGALWAYS,
        0, //We not use opcodes
        FALSE, //No need for stacks
        sizeof...(TArgs),
        newEvent->_descriptor,
        &event));

    profilerEventDescriptor.reset(newEvent.release());
    profilerEventDescriptor->_event = event;
    _currentEventId++;
    return S_OK;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ProfilerEventProvider.h"
#include "corhlpr.h"

HRESULT ProfilerEventProvider::CreateProvider(const WCHAR* providerName, ICorProfilerInfo12* profilerInfo, std::unique_ptr<ProfilerEventProvider>& provider)
{
    EVENTPIPE_PROVIDER eventProvider = 0;
    HRESULT hr;

    IfFailRet(profilerInfo->EventPipeCreateProvider(providerName, &eventProvider));
    provider.reset(new ProfilerEventProvider(profilerInfo, eventProvider));

    return S_OK;
}

ProfilerEventProvider::ProfilerEventProvider(ICorProfilerInfo12* profilerInfo, EVENTPIPE_PROVIDER provider) : _provider(provider), _profilerInfo(profilerInfo)
{
}

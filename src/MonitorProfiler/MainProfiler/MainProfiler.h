// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../ProfilerBase.h"

class MainProfiler :
    public ProfilerBase
{
public:
    static GUID GetClsid();

    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(LoadAsNotficationOnly)(BOOL *pbNotificationOnly) override;
};


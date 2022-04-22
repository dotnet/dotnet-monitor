// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "corhlpr.h"
#include "MainProfiler.h"
#include "macros.h"
#include "productversion.h"
#include "tstring.h"
#include "tostream.h"
#include <iostream>
#include <iomanip>

#define HRESULTHEX(x) \
   "0x" << std::setw(8) << std::setfill('0') << std::hex << x

GUID MainProfiler::GetClsid()
{
    // {6A494330-5848-4A23-9D87-0E57BBF6DE79}
    return { 0x6A494330, 0x5848, 0x4A23,{ 0x9D, 0x87, 0x0E, 0x57, 0xBB, 0xF6, 0xDE, 0x79 } };
}

STDMETHODIMP MainProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    HRESULT hr = S_OK;

    if (FAILED(hr = ProfilerBase::Initialize(pICorProfilerInfoUnk)))
    {
        std::cerr << "Failed to initialize profiler." << std::endl;
    }

    IfFailRet(m_pCorProfilerInfo->SetEnvironmentVariable(
        _T("DOTNETMONITOR_ProductVersion"),
        MonitorProductVersion_TSTR
        ));

    return S_OK;
}

STDMETHODIMP MainProfiler::LoadAsNotficationOnly(BOOL *pbNotificationOnly)
{
    ExpectedPtr(pbNotificationOnly);

    *pbNotificationOnly = TRUE;

    return S_OK;
}

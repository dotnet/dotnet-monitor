// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "MainProfiler.h"
#include "macros.h"
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

    std::cout << "Getting variable length." << std::endl;

    ULONG cchLen = 0;
    if (FAILED(hr = m_pCorProfilerInfo->GetEnvironmentVariable(
        _T("MainProfiler_TestVariable"),
        0,
        &cchLen,
        nullptr
        )))
    {
        std::cerr << "Unable to get MainProfiler_TestVariable length: " << HRESULTHEX(hr) << std::endl;
        return hr;
    }

    std::cout << "Allocating buffer." << std::endl;

    std::unique_ptr<WCHAR> pwszTestVariableValue(new WCHAR[cchLen]);

    std::cout << "Getting variable value." << std::endl;

    if (FAILED(hr = m_pCorProfilerInfo->GetEnvironmentVariable(
        _T("MainProfiler_TestVariable"),
        cchLen,
        &cchLen,
        pwszTestVariableValue.get()
        )))
    {
        std::cerr << "Unable to get MainProfiler_TestVariable content: " << HRESULTHEX(hr) << std::endl;
        return hr;
    }

    tstring tstrTestVariableValue(pwszTestVariableValue.get());

    std::cout << "Value: " << tstrTestVariableValue << std::endl;

    return S_OK;
}

STDMETHODIMP MainProfiler::LoadAsNotficationOnly(BOOL *pbNotificationOnly)
{
    ExpectedPtr(pbNotificationOnly);

    *pbNotificationOnly = TRUE;

    return S_OK;
}

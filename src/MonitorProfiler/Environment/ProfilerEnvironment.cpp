// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ProfilerEnvironment.h"

using namespace std;

ProfilerEnvironment::ProfilerEnvironment(ICorProfilerInfo12* pCorProfilerInfo) :
    m_pCorProfilerInfo(pCorProfilerInfo)
{
}

STDMETHODIMP ProfilerEnvironment::GetEnvironmentVariable(const tstring name, tstring& value)
{
    HRESULT hr = S_OK;

    ULONG cchValue;
    IfFailRet(m_pCorProfilerInfo->GetEnvironmentVariable(
        name.c_str(),
        0,
        &cchValue,
        nullptr));

    unique_ptr<WCHAR[]> pwszValue(new (nothrow) WCHAR[cchValue + 1]);
    IfNullRet(pwszValue);

    IfFailRet(m_pCorProfilerInfo->GetEnvironmentVariable(
        name.c_str(),
        cchValue + 1,
        nullptr,
        pwszValue.get()
        ));

    value.assign(pwszValue.get());

    return S_OK;
}

STDMETHODIMP ProfilerEnvironment::SetEnvironmentVariable(const tstring name, const tstring value)
{
    HRESULT hr = S_OK;

    IfFailRet(m_pCorProfilerInfo->SetEnvironmentVariable(
        name.c_str(),
        value.c_str()
        ));

    return S_OK;
}
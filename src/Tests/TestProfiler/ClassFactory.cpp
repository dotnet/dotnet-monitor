// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClassFactory.h"
#include "EnvProfiler/EnvProfiler.h"
#include "ProfilerBase.h"

ClassFactory::ClassFactory(REFCLSID guidClsid) :
    m_guidClsid(guidClsid)
{
}

STDMETHODIMP ClassFactory::CreateInstance(
    IUnknown *pUnkOuter,
    REFIID riid,
    void **ppvObject)
{
    if (nullptr == ppvObject)
    {
        return E_POINTER;
    }

    if (nullptr != pUnkOuter)
    {
        *ppvObject = nullptr;
        return CLASS_E_NOAGGREGATION;
    }

    ComPtr<ProfilerBase> pProfiler;
    if (m_guidClsid == EnvProfiler::GetClsid())
    {
        pProfiler = new EnvProfiler();
        if (nullptr == pProfiler)
        {
            return E_OUTOFMEMORY;
        }
    }
    else
    {
        return E_FAIL;
    }

    return pProfiler->QueryInterface(riid, ppvObject);
}

STDMETHODIMP ClassFactory::LockServer(BOOL fLock)
{
    return S_OK;
}

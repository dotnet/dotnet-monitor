// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ClassFactory.h"
#include "corhlpr.h"
#include "macros.h"
#include "MainProfiler/MainProfiler.h"
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
    ExpectedPtr(ppvObject);

    *ppvObject = nullptr;

    if (nullptr != pUnkOuter)
    {
        return CLASS_E_NOAGGREGATION;
    }

    ComPtr<ProfilerBase> pProfiler;
    if (m_guidClsid == MainProfiler::GetClsid())
    {
        pProfiler = new MainProfiler();
        IfNullRet(pProfiler);
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

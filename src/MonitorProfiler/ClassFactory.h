// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "unknwn.h"
#include "com.h"
#include "refcount.h"

class ClassFactory final :
    public RefCount,
    public IClassFactory
{
private:
    CLSID m_guidClsid;

public:
    ClassFactory(REFCLSID guidClsid);
    ~ClassFactory() {}

    DEFINE_DELEGATED_REFCOUNT_ADDREF(ClassFactory)
    DEFINE_DELEGATED_REFCOUNT_RELEASE(ClassFactory)
    BEGIN_COM_MAP(ClassFactory)
        COM_INTERFACE_ENTRY(IUnknown)
        COM_INTERFACE_ENTRY(IClassFactory)
    END_COM_MAP()

    STDMETHOD(CreateInstance)(IUnknown *pUnkOuter, REFIID riid, void **ppvObject) override;
    STDMETHOD(LockServer)(BOOL fLock) override;
};

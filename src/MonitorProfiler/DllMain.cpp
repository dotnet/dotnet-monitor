// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClassFactory.h"
#include "corhlpr.h"
#include "macros.h"
#include "guids.h"

#ifndef DLLEXPORT
#define DLLEXPORT
#endif // DLLEXPORT

STDMETHODIMP_(BOOL) DLLEXPORT DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}

STDAPI DLLEXPORT DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
    IfNullPointer(ppv);

    *ppv = nullptr;

    ComPtr<ClassFactory> pFactory(new ClassFactory(rclsid));
    IfNullRet(pFactory);

    return pFactory->QueryInterface(riid, ppv);
}

STDAPI DLLEXPORT DllCanUnloadNow()
{
    return S_OK;
}

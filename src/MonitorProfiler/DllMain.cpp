// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

_Check_return_
STDAPI DLLEXPORT DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID* ppv)
{
    ExpectedPtr(ppv);

    *ppv = nullptr;

    ComPtr<ClassFactory> pFactory(new ClassFactory(rclsid));
    IfNullRet(pFactory);

    return pFactory->QueryInterface(riid, ppv);
}

__control_entrypoint(DllExport)
STDAPI DLLEXPORT DllCanUnloadNow(void)
{
    return S_OK;
}

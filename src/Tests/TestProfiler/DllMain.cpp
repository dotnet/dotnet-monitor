// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClassFactory.h"
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
    if (nullptr == ppv)
    {
        return E_POINTER;
    }

    ComPtr<ClassFactory> pfactory(new ClassFactory(rclsid));
    if (nullptr == pfactory)
    {
        return E_OUTOFMEMORY;
    }

    return pfactory->QueryInterface(riid, ppv);
}

__control_entrypoint(DllExport)
STDAPI DLLEXPORT DllCanUnloadNow(void)
{
    return S_OK;
}

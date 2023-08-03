// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "macros.h"
#include "corprof.h"

class RuntimeUtilities
{
    public:
        static HRESULT IsRuntimeSupported(ICorProfilerInfo12* pCorProfilerInfo, BOOL& supported)
        {
            HRESULT hr;

            ExpectedPtr(pCorProfilerInfo);

            supported = FALSE;

            COR_PRF_RUNTIME_TYPE runtimeType;
            IfFailRet(pCorProfilerInfo->GetRuntimeInformation(
                nullptr, // instance id
                &runtimeType,
                nullptr, nullptr, nullptr, nullptr, // version info
                0, nullptr, nullptr // version string
            ));


            supported = (runtimeType == COR_PRF_CORE_CLR);
            return S_OK;
        }
};
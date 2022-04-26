// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "EnvironmentHelper.h"
#include "productversion.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(pLogger, EXPR)

HRESULT EnvironmentHelper::SetProductVersion(
    const shared_ptr<IEnvironment>& pEnvironment,
    const shared_ptr<ILogger>& pLogger)
{
    HRESULT hr = S_OK;

    IfFailLogRet(pEnvironment->SetEnvironmentVariable(
        s_wszProfilerVersionEnvVar,
        MonitorProductVersion_TSTR
        ));

    return S_OK;
}

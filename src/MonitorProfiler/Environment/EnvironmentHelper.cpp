// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "EnvironmentHelper.h"
#include "../Logging/LogLevelHelper.h"
#include "productversion.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(pLogger, EXPR)

HRESULT EnvironmentHelper::GetDebugLoggerLevel(
    const std::shared_ptr<IEnvironment>& pEnvironment,
    LogLevel& level)
{
    HRESULT hr = S_OK;

    tstring tstrLevel;
    IfFailRet(pEnvironment->GetEnvironmentVariable(
        s_wszDebugLoggerLevelEnvVar,
        tstrLevel
        ));

    IfFailRet(LogLevelHelper::ToLogLevel(tstrLevel, level));

    return S_OK;
}

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

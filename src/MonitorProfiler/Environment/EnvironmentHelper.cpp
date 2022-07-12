// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "EnvironmentHelper.h"
#include "../Logging/LogLevelHelper.h"
#include "productversion.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(_logger, EXPR)

EnvironmentHelper::EnvironmentHelper(
    const std::shared_ptr<IEnvironment>& pEnvironment,
    const std::shared_ptr<ILogger>& pLogger) : _environment(pEnvironment), _logger(pLogger)
{
}

HRESULT EnvironmentHelper::GetDebugLoggerLevel(LogLevel& level)
{
    HRESULT hr = S_OK;

    tstring tstrLevel;
    IfFailRet(_environment->GetEnvironmentVariable(
        DebugLoggerLevelEnvVar,
        tstrLevel
        ));

    IfFailRet(LogLevelHelper::ToLogLevel(tstrLevel, level));

    return S_OK;
}

HRESULT EnvironmentHelper::SetProductVersion()
{
    HRESULT hr = S_OK;

    IfFailLogRet(_environment->SetEnvironmentVariable(
        ProfilerVersionEnvVar,
        MonitorProductVersion_TSTR
        ));

    return S_OK;
}

HRESULT EnvironmentHelper::GetRuntimeInstanceId(tstring& instanceId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_environment->GetEnvironmentVariable(RuntimeInstanceEnvVar, instanceId));

    return S_OK;
}

HRESULT EnvironmentHelper::GetStdErrLoggerLevel(LogLevel& level)
{
    HRESULT hr = S_OK;

    tstring tstrLevel;
    IfFailRet(_environment->GetEnvironmentVariable(
        StdErrLoggerLevelEnvVar,
        tstrLevel
    ));

    IfFailRet(LogLevelHelper::ToLogLevel(tstrLevel, level));

    return S_OK;
}

HRESULT EnvironmentHelper::GetTempFolder(tstring& tempFolder)
{
    HRESULT hr = S_OK;

    tstring tmpDir;
#if TARGET_WINDOWS
    IfFailLogRet(_environment->GetEnvironmentVariable(TempEnvVar, tmpDir));
#else
    hr = _environment->GetEnvironmentVariable(TempEnvVar, tmpDir);
    if (FAILED(hr))
    {
        if (hr != HRESULT_FROM_WIN32(ERROR_ENVVAR_NOT_FOUND))
        {
            return hr;
        }
        tmpDir = DefaultTempFolder;
    }
#endif

    tempFolder = std::move(tmpDir);

    return S_OK;
}

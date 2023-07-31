// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "EnvironmentHelper.h"
#include "../Logging/LogLevelHelper.h"
#include "runtime_version.h"

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

HRESULT EnvironmentHelper::SetProductVersion(const tstring& envVarName)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_environment->SetEnvironmentVariable(
        envVarName,
        QUOTE_MACRO_T(RuntimeProductVersion)
        ));

    return S_OK;
}

HRESULT EnvironmentHelper::GetRuntimeInstanceId(tstring& instanceId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_environment->GetEnvironmentVariable(RuntimeInstanceEnvVar, instanceId));

    return S_OK;
}

HRESULT EnvironmentHelper::GetSharedPath(tstring& sharedPath)
{
    HRESULT hr = S_OK;

    hr = _environment->GetEnvironmentVariable(SharedPathEnvVar, sharedPath);
    if (FAILED(hr))
    {
        if (hr != HRESULT_FROM_WIN32(ERROR_ENVVAR_NOT_FOUND))
        {
            return hr;
        }
        IfFailRet(GetTempFolder(sharedPath));
    }

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

HRESULT EnvironmentHelper::GetIsFeatureEnabled(const LPCWSTR featureName, bool& isEnabled)
{
    HRESULT hr;

    isEnabled = false;

    tstring envValue;
    hr = _environment->GetEnvironmentVariable(featureName, envValue);
    if (FAILED(hr))
    {
        if (hr != HRESULT_FROM_WIN32(ERROR_ENVVAR_NOT_FOUND))
        {
            return hr;
        }
    }
    else
    {
        //
        // Case sensitive comparision is okay here as this value is "1"
        // and managed by dotnet-monitor.
        //
        isEnabled = (envValue == EnableEnvVarValue);
    }


    return S_OK;
}
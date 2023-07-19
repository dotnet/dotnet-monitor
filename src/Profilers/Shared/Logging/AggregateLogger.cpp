// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AggregateLogger.h"

using namespace std;

STDMETHODIMP AggregateLogger::Add(const shared_ptr<ILogger> pLogger)
{
    m_loggers.push_back(pLogger);

    return S_OK;
}

STDMETHODIMP_(bool) AggregateLogger::IsEnabled(LogLevel level)
{
    // Check if any logger accepts the specified level
    // CONSIDER: Further optimization could be made to classify the loggers
    // into different lists for each level such that the aggregate logger would
    // not need to check if the level is enabled for each IsEnabled call.
    for (shared_ptr<ILogger>& pLogger : m_loggers)
    {
        if (pLogger->IsEnabled(level))
        {
            return true;
        }
    }

    return false;
}

STDMETHODIMP AggregateLogger::Log(LogLevel level, const lstring& message)
{
    HRESULT hr = S_OK;

    for (shared_ptr<ILogger>& pLogger : m_loggers)
    {
        // Only pass to logger if it accepts the level
        // CONSIDER: Further optimization could be made to classify the loggers
        // into different lists for each level such that the aggregate logger would
        // not need to check if the level is enabled for each Log call.
        if (pLogger->IsEnabled(level))
        {
            IfFailRet(pLogger->Log(level, message));
        }
    }

    return S_OK;
}

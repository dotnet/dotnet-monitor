// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "AggregateLogger.h"

using namespace std;

STDMETHODIMP AggregateLogger::Add(const shared_ptr<ILogger> pLogger)
{
    m_loggers.push_back(pLogger);

    return S_OK;
}

STDMETHODIMP AggregateLogger::Log(LogLevel level, const string format, va_list args)
{
    HRESULT hr = S_OK;

    for (shared_ptr<ILogger>& pLogger : m_loggers)
    {
        IfFailRet(pLogger->Log(level, format, args));
    }

    return S_OK;
}

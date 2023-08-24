// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Logger.h"
#include "AggregateLogger.h"
#include "DebugLogger.h"
#include "StdErrLogger.h"
#include "Environment/Environment.h"

#include <memory>

class LoggerFactory
{
    public:
        static HRESULT Create(std::shared_ptr<IEnvironment> pEnvironment, std::shared_ptr<ILogger>& pLogger)
        {
            HRESULT hr = S_OK;

            // Create an aggregate logger to allow for multiple logging implementations
            std::unique_ptr<AggregateLogger> pAggregateLogger(new (std::nothrow) AggregateLogger());
            IfNullRet(pAggregateLogger);

            std::shared_ptr<StdErrLogger> pStdErrLogger = std::make_shared<StdErrLogger>(pEnvironment);
            IfNullRet(pStdErrLogger);
            pAggregateLogger->Add(pStdErrLogger);

#ifdef _DEBUG
#ifdef TARGET_WINDOWS
            // Add the debug output logger for when debugging on Windows
            std::shared_ptr<DebugLogger> pDebugLogger = std::make_shared<DebugLogger>(pEnvironment);
            IfNullRet(pDebugLogger);
            pAggregateLogger->Add(pDebugLogger);
#endif
#endif

            pLogger.reset(pAggregateLogger.release());

            return S_OK;
        } 
};
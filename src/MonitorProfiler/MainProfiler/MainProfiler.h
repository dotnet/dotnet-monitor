// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../ProfilerBase.h"
#include "../Environment/Environment.h"
#include "../Environment/EnvironmentHelper.h"
#include "../Logging/Logger.h"
#include "../Communication/CommandServer.h"
#include <memory>
#include "ThreadDataManager.h"
#include "ExceptionTracker.h"

class MainProfiler final :
    public ProfilerBase
{
private:
    std::shared_ptr<IEnvironment> m_pEnvironment;
    std::shared_ptr<EnvironmentHelper> _environmentHelper;
    std::shared_ptr<ILogger> m_pLogger;
    std::shared_ptr<ThreadDataManager> _threadDataManager;
    std::unique_ptr<ExceptionTracker> _exceptionTracker;

public:
    static GUID GetClsid();

    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(Shutdown)() override;
    STDMETHOD(ThreadCreated)(ThreadID threadId) override;
    STDMETHOD(ThreadDestroyed)(ThreadID threadId) override;
    STDMETHOD(ExceptionThrown)(ObjectID thrownObjectId) override;
    STDMETHOD(ExceptionSearchCatcherFound)(FunctionID functionId) override;
    STDMETHOD(ExceptionUnwindFunctionEnter)(FunctionID functionId) override;
    STDMETHOD(LoadAsNotficationOnly)(BOOL *pbNotificationOnly) override;

private:
    HRESULT InitializeEnvironment();
    HRESULT InitializeEnvironmentHelper();
    HRESULT InitializeLogging();
    HRESULT InitializeCommandServer();
    HRESULT MessageCallback(const IpcMessage& message);
    HRESULT ProcessCallstackMessage();
private:
    std::unique_ptr<CommandServer> _commandServer;
};


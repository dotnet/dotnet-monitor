// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../Communication/CommandServer.h"

#include "ProfilerBase.h"
#include "Environment/Environment.h"
#include "Environment/EnvironmentHelper.h"
#include "Logging/Logger.h"
#include "CommonUtilities/ThreadNameCache.h"
#include <memory>

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
#include "ThreadDataManager.h"
#include "ExceptionTracker.h"
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

class MainProfiler final :
    public ProfilerBase
{
private:
    static constexpr LPCWSTR ProfilerVersionEnvVar = _T("DotnetMonitor_MonitorProfiler_ProductVersion");

private:
    std::shared_ptr<IEnvironment> m_pEnvironment;
    std::shared_ptr<EnvironmentHelper> _environmentHelper;
    std::shared_ptr<ILogger> m_pLogger;
    std::shared_ptr<ThreadNameCache> _threadNameCache;
#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    std::shared_ptr<ThreadDataManager> _threadDataManager;
    std::unique_ptr<ExceptionTracker> _exceptionTracker;
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS


public:
    static GUID GetClsid();

    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(Shutdown)() override;
    STDMETHOD(ThreadCreated)(ThreadID threadId) override;
    STDMETHOD(ThreadDestroyed)(ThreadID threadId) override;
    STDMETHOD(ThreadNameChanged)(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    STDMETHOD(ExceptionThrown)(ObjectID thrownObjectId) override;
    STDMETHOD(ExceptionSearchCatcherFound)(FunctionID functionId) override;
    STDMETHOD(ExceptionUnwindFunctionEnter)(FunctionID functionId) override;
    STDMETHOD(InitializeForAttach)(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override;
    STDMETHOD(LoadAsNotificationOnly)(BOOL *pbNotificationOnly) override;

private:
    HRESULT InitializeCommon();
    HRESULT InitializeEnvironment();
    HRESULT InitializeEnvironmentHelper();
    HRESULT InitializeCommandServer();
    HRESULT MessageCallback(const IpcMessage& message);
    HRESULT ValidateMessage(const IpcMessage& message);
    HRESULT ProfilerCommandSetCallback(const IpcMessage& message);
    HRESULT ProcessCallstackMessage();
private:
    std::unique_ptr<CommandServer> _commandServer;
};


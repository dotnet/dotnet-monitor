// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "MainProfiler.h"
#include "../Environment/EnvironmentHelper.h"
#include "../Environment/ProfilerEnvironment.h"
#include "../Logging/AggregateLogger.h"
#include "../Logging/DebugLogger.h"
#include "corhlpr.h"
#include "macros.h"
#include <memory>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

GUID MainProfiler::GetClsid()
{
    // {6A494330-5848-4A23-9D87-0E57BBF6DE79}
    return { 0x6A494330, 0x5848, 0x4A23,{ 0x9D, 0x87, 0x0E, 0x57, 0xBB, 0xF6, 0xDE, 0x79 } };
}

STDMETHODIMP MainProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    ExpectedPtr(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;

    //These should always be initialized first
    IfFailRet(ProfilerBase::Initialize(pICorProfilerInfoUnk));
    IfFailRet(InitializeEnvironment());
    IfFailRet(InitializeLogging());

    // Logging is initialized and can now be used

    _threadDataManager = make_shared<ThreadDataManager>(m_pLogger);
    IfNullRet(_threadDataManager);
    _exceptionTracker.reset(new (nothrow) ExceptionTracker(m_pLogger, _threadDataManager, m_pCorProfilerInfo));
    IfNullRet(_exceptionTracker);

    IfFailLogRet(InitializeCommandServer());

    // Set product version environment variable to allow discovery of if the profiler
    // as been applied to a target process. Diagnostic tools must use the diagnostic
    // communication channel's GetProcessEnvironment command to get this value.
    IfFailLogRet(EnvironmentHelper::SetProductVersion(m_pEnvironment, m_pLogger));

    DWORD eventsLow = COR_PRF_MONITOR::COR_PRF_MONITOR_NONE;
    ThreadDataManager::AddProfilerEventMask(eventsLow);
    ExceptionTracker::AddProfilerEventMask(eventsLow);

    IfFailRet(m_pCorProfilerInfo->SetEventMask2(
        eventsLow,
        COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_MONITOR_NONE));

    return S_OK;
}

STDMETHODIMP MainProfiler::Shutdown()
{
    m_pLogger.reset();
    m_pEnvironment.reset();
    _commandServer->Shutdown();
    _commandServer.reset();

    return ProfilerBase::Shutdown();
}

STDMETHODIMP MainProfiler::ThreadCreated(ThreadID threadId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_threadDataManager->ThreadCreated(threadId));

    return S_OK;
}

STDMETHODIMP MainProfiler::ThreadDestroyed(ThreadID threadId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_threadDataManager->ThreadDestroyed(threadId));

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    HRESULT hr = S_OK;

    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionThrown(threadId, thrownObjectId));

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    HRESULT hr = S_OK;

    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionSearchCatcherFound(threadId, functionId));

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    HRESULT hr = S_OK;

    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionUnwindFunctionEnter(threadId, functionId));

    return S_OK;
}

STDMETHODIMP MainProfiler::LoadAsNotficationOnly(BOOL *pbNotificationOnly)
{
    ExpectedPtr(pbNotificationOnly);

    *pbNotificationOnly = TRUE;

    return S_OK;
}

HRESULT MainProfiler::InitializeEnvironment()
{
    m_pEnvironment = make_shared<ProfilerEnvironment>(m_pCorProfilerInfo);
    IfNullRet(m_pEnvironment);

    return S_OK;
}

HRESULT MainProfiler::InitializeLogging()
{
    HRESULT hr = S_OK;

    // Create an aggregate logger to allow for multiple logging implementations
    unique_ptr<AggregateLogger> pAggregateLogger(new (nothrow) AggregateLogger());
    IfNullRet(pAggregateLogger);

#ifdef _DEBUG
#ifdef TARGET_WINDOWS
    // Add the debug output logger for when debugging on Windows
    shared_ptr<DebugLogger> pDebugLogger = make_shared<DebugLogger>(m_pEnvironment);
    IfNullRet(pDebugLogger);
    pAggregateLogger->Add(pDebugLogger);
#endif
#endif

    m_pLogger.reset(pAggregateLogger.release());

    return S_OK;
}

HRESULT MainProfiler::InitializeCommandServer()
{
    HRESULT hr = S_OK;

    //TODO For now we are using the process id to generate the unique server name. We should use the environment
    //value with the runtime instance id once it's available.
    unsigned long pid =
#if TARGET_WINDOWS
        GetCurrentProcessId();
#else
        getpid();
#endif

    tstring instanceId = to_tstring(to_string(pid));
    //IfFailRet(EnvironmentHelper::GetRuntimeInstanceId(m_pEnvironment, m_pLogger, instanceId));

#if TARGET_UNIX
    tstring separator = _T("/");
#else
    tstring separator = _T("\\");
#endif

    tstring tmpDir;
    IfFailRet(EnvironmentHelper::GetTempFolder(m_pEnvironment, m_pLogger, tmpDir));

    _commandServer = std::unique_ptr<CommandServer>(new CommandServer(m_pLogger));
    tstring socketPath = tmpDir + separator + instanceId + _T(".sock");

    IfFailRet(_commandServer->Start(to_string(socketPath), [this](const IpcMessage& message)-> HRESULT { return this->MessageCallback(message); }));

    return S_OK;
}

HRESULT MainProfiler::MessageCallback(const IpcMessage& message)
{
    m_pLogger->Log(LogLevel::Information, _T("Message received from client: %d %d"), message.MessageType, message.Parameters);
    return S_OK;
}

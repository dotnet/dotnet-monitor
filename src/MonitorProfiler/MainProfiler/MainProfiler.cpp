// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "MainProfiler.h"
#include "../Environment/EnvironmentHelper.h"
#include "../Environment/ProfilerEnvironment.h"
#include "../Logging/AggregateLogger.h"
#include "../Logging/DebugLogger.h"
#include "../Logging/StdErrLogger.h"
#include "../Stacks/StacksEventProvider.h"
#include "../Stacks/StackSampler.h"
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

    //These are created in dependency order!
    IfFailRet(InitializeEnvironment());
    IfFailRet(InitializeLogging());
    IfFailRet(InitializeEnvironmentHelper());

    // Logging is initialized and can now be used

    _threadDataManager = make_shared<ThreadDataManager>(m_pLogger);
    IfNullRet(_threadDataManager);
    _exceptionTracker.reset(new (nothrow) ExceptionTracker(m_pLogger, _threadDataManager, m_pCorProfilerInfo));
    IfNullRet(_exceptionTracker);

    IfFailLogRet(InitializeCommandServer());

    // Set product version environment variable to allow discovery of if the profiler
    // as been applied to a target process. Diagnostic tools must use the diagnostic
    // communication channel's GetProcessEnvironment command to get this value.
    IfFailLogRet(_environmentHelper->SetProductVersion());

    DWORD eventsLow = COR_PRF_MONITOR::COR_PRF_MONITOR_NONE;
    ThreadDataManager::AddProfilerEventMask(eventsLow);
    _exceptionTracker->AddProfilerEventMask(eventsLow);
    StackSampler::AddProfilerEventMask(eventsLow);

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
    if (m_pEnvironment)
    {
        return E_UNEXPECTED;
    }
    m_pEnvironment = make_shared<ProfilerEnvironment>(m_pCorProfilerInfo);
    return S_OK;
}

HRESULT MainProfiler::InitializeEnvironmentHelper()
{
    IfNullRet(m_pEnvironment);

    _environmentHelper = make_shared<EnvironmentHelper>(m_pEnvironment, m_pLogger);

    return S_OK;
}

HRESULT MainProfiler::InitializeLogging()
{
    HRESULT hr = S_OK;

    // Create an aggregate logger to allow for multiple logging implementations
    unique_ptr<AggregateLogger> pAggregateLogger(new (nothrow) AggregateLogger());
    IfNullRet(pAggregateLogger);

    shared_ptr<StdErrLogger> pStdErrLogger = make_shared<StdErrLogger>(m_pEnvironment);
    IfNullRet(pStdErrLogger);
    pAggregateLogger->Add(pStdErrLogger);

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

    tstring instanceId;
    IfFailRet(_environmentHelper->GetRuntimeInstanceId(instanceId));

#if TARGET_UNIX
    tstring separator = _T("/");
#else
    tstring separator = _T("\\");
#endif

    tstring tmpDir;
    IfFailRet(_environmentHelper->GetTempFolder(tmpDir));

    _commandServer = std::unique_ptr<CommandServer>(new CommandServer(m_pLogger, m_pCorProfilerInfo));
    tstring socketPath = tmpDir + separator + instanceId + _T(".sock");

    IfFailRet(_commandServer->Start(to_string(socketPath), [this](const IpcMessage& message)-> HRESULT { return this->MessageCallback(message); }));

    return S_OK;
}

HRESULT MainProfiler::MessageCallback(const IpcMessage& message)
{
    m_pLogger->Log(LogLevel::Information, _LS("Message received from client: %d %d"), message.MessageType, message.Parameters);

    HRESULT hr;

    if (message.MessageType == MessageType::Callstack)
    {
        //Currently we do not have any options for this message

        StackSampler stackSampler(m_pCorProfilerInfo);
        std::vector<std::unique_ptr<StackSamplerState>> stackStates;
        std::shared_ptr<NameCache> nameCache;

        IfFailLogRet(stackSampler.CreateCallstack(stackStates, nameCache));

        std::unique_ptr<StacksEventProvider> eventProvider;
        IfFailLogRet(StacksEventProvider::CreateProvider(m_pCorProfilerInfo, eventProvider));

        for (auto& entry : nameCache->GetFunctions())
        {
            tstring name;
            IfFailLogRet(nameCache->GetFullyQualifiedName(entry.first, name));
            m_pLogger->Log(LogLevel::Information, name);

            IfFailLogRet(eventProvider->WriteFunctionData(entry.first, *entry.second.get()));
        }
        for (auto& entry : nameCache->GetClasses())
        {
            IfFailLogRet(eventProvider->WriteClassData(entry.first, *entry.second.get()));
        }
        for (auto& entry : nameCache->GetModules())
        {
            IfFailLogRet(eventProvider->WriteModuleData(entry.first, *entry.second.get()));
        }
        for (auto& entry : nameCache->GetTypeNames())
        {
            //first: (Module,TypeDef)
            IfFailLogRet(eventProvider->WriteTokenData(entry.first.first, entry.first.second, *entry.second.get()));
        }

        for (std::unique_ptr<StackSamplerState>& stackState : stackStates)
        {
            IfFailLogRet(eventProvider->WriteCallstack(stackState->GetStack()));
        }

        IfFailLogRet(eventProvider->WriteEndEvent());
    }

    return S_OK;
}

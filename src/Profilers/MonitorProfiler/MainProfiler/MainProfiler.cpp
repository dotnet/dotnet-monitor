// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "MainProfiler.h"
#include "Environment/EnvironmentHelper.h"
#include "Environment/ProfilerEnvironment.h"
#include "Logging/LoggerFactory.h"
#include "CommonUtilities/ThreadUtilities.h"
#include "../Stacks/StacksEventProvider.h"
#include "../Stacks/StackSampler.h"
#include "corhlpr.h"
#include "macros.h"
#include <memory>
#include <mutex>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

#ifndef DLLEXPORT
#define DLLEXPORT
#endif

typedef INT32 (STDMETHODCALLTYPE *ManagedMessageCallback)(INT16, const BYTE*, UINT64);
mutex g_managedMessageCallbackMutex; // guards g_pManagedMessageCallback
ManagedMessageCallback g_pManagedMessageCallback = nullptr;

GUID MainProfiler::GetClsid()
{
    // {6A494330-5848-4A23-9D87-0E57BBF6DE79}
    return { 0x6A494330, 0x5848, 0x4A23,{ 0x9D, 0x87, 0x0E, 0x57, 0xBB, 0xF6, 0xDE, 0x79 } };
}

STDMETHODIMP MainProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    ExpectedPtr(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;

    // These should always be initialized first
    IfFailRet(ProfilerBase::Initialize(pICorProfilerInfoUnk));

    IfFailRet(InitializeCommon());

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

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    IfFailLogRet(_threadDataManager->ThreadCreated(threadId));
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    return S_OK;
}

STDMETHODIMP MainProfiler::ThreadDestroyed(ThreadID threadId)
{
    HRESULT hr = S_OK;

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    IfFailLogRet(_threadDataManager->ThreadDestroyed(threadId));
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    _threadNameCache->Remove(threadId);

    return S_OK;
}

STDMETHODIMP MainProfiler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (name != nullptr && cchName > 0)
    {
        _threadNameCache->Set(threadId, tstring(name, cchName));
    }

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    HRESULT hr = S_OK;

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionThrown(threadId, thrownObjectId));
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    HRESULT hr = S_OK;

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionSearchCatcherFound(threadId, functionId));
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    return S_OK;
}

STDMETHODIMP MainProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    HRESULT hr = S_OK;

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    ThreadID threadId;
    IfFailLogRet(m_pCorProfilerInfo->GetCurrentThreadID(&threadId));

    IfFailLogRet(_exceptionTracker->ExceptionUnwindFunctionEnter(threadId, functionId));
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    return S_OK;
}

STDMETHODIMP MainProfiler::InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    HRESULT hr = S_OK;

    // These should always be initialized first
    IfFailRet(ProfilerBase::Initialize(pCorProfilerInfoUnk));

    IfFailRet(InitializeCommon());

    return S_OK;
}

STDMETHODIMP MainProfiler::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    ExpectedPtr(pbNotificationOnly);

    *pbNotificationOnly = TRUE;

    return S_OK;
}

HRESULT MainProfiler::InitializeCommon()
{
    HRESULT hr = S_OK;

    // These are created in dependency order!
    IfFailRet(InitializeEnvironment());
    IfFailRet(LoggerFactory::Create(m_pEnvironment, m_pLogger));
    IfFailRet(InitializeEnvironmentHelper());

    // Logging is initialized and can now be used
    bool supported;
    IfFailLogRet(ProfilerBase::IsRuntimeSupported(supported));
    if (!supported)
    {
        m_pLogger->Log(LogLevel::Debug, _LS("Unsupported runtime."));
        return CORPROF_E_PROFILER_CANCEL_ACTIVATION;
    }

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    _threadDataManager = make_shared<ThreadDataManager>(m_pLogger);
    IfNullRet(_threadDataManager);
    _exceptionTracker.reset(new (nothrow) ExceptionTracker(m_pLogger, _threadDataManager, m_pCorProfilerInfo));
    IfNullRet(_exceptionTracker);
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS

    // Set product version environment variable to allow discovery of if the profiler
    // as been applied to a target process. Diagnostic tools must use the diagnostic
    // communication channel's GetProcessEnvironment command to get this value.
    IfFailLogRet(_environmentHelper->SetProductVersion(ProfilerVersionEnvVar));

    DWORD eventsLow = COR_PRF_MONITOR::COR_PRF_MONITOR_NONE;
#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
    ThreadDataManager::AddProfilerEventMask(eventsLow);
    _exceptionTracker->AddProfilerEventMask(eventsLow);
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS
    StackSampler::AddProfilerEventMask(eventsLow);

    _threadNameCache = make_shared<ThreadNameCache>();

    IfFailRet(m_pCorProfilerInfo->SetEventMask2(
        eventsLow,
        COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_MONITOR_NONE));

    //Initialize this last. The CommandServer creates secondary threads, which will be difficult to cleanup if profiler initialization fails.
    IfFailLogRet(InitializeCommandServer());

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

    tstring sharedPath;
    IfFailRet(_environmentHelper->GetSharedPath(sharedPath));

    _commandServer = std::unique_ptr<CommandServer>(new CommandServer(m_pLogger, m_pCorProfilerInfo));
    tstring socketPath = sharedPath + separator + instanceId + _T(".sock");

    IfFailRet(_commandServer->Start(to_string(socketPath), [this](const IpcMessage& message)-> HRESULT { return this->MessageCallback(message); }));

    return S_OK;
}

HRESULT MainProfiler::MessageCallback(const IpcMessage& message)
{
    m_pLogger->Log(LogLevel::Debug, _LS("Message received from client %d"), message.Command);

    switch (message.Command)
    {
    case IpcCommand::Unknown:
        return E_FAIL;
    case IpcCommand::Callstack:
        return ProcessCallstackMessage();
    default:
        lock_guard<mutex> lock(g_managedMessageCallbackMutex);
        if (g_pManagedMessageCallback == nullptr)
        {
            return E_FAIL;
        }

        return g_pManagedMessageCallback(
            static_cast<INT16>(message.Command),
            message.Payload.data(),
            message.Payload.size());
    }

    return E_FAIL;
}

HRESULT MainProfiler::ProcessCallstackMessage()
{
    HRESULT hr;

    StackSampler stackSampler(m_pCorProfilerInfo);
    std::vector<std::unique_ptr<StackSamplerState>> stackStates;
    std::shared_ptr<NameCache> nameCache;

    IfFailLogRet(stackSampler.CreateCallstack(stackStates, nameCache, _threadNameCache));

    std::unique_ptr<StacksEventProvider> eventProvider;
    IfFailLogRet(StacksEventProvider::CreateProvider(m_pCorProfilerInfo, eventProvider));

    for (auto& entry : nameCache->GetFunctions())
    {
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

    //HACK See https://github.com/dotnet/runtime/issues/76704
    // We sleep here for 200ms to ensure that our event is timestamped. Since we are on a dedicated message
    // thread we should not be interfering with the app itself.
    ThreadUtilities::Sleep(200);

    IfFailLogRet(eventProvider->WriteEndEvent());

    return S_OK;
}

STDAPI DLLEXPORT RegisterMonitorMessageCallback(
    ManagedMessageCallback pCallback)
{
    //
    // Note: Require locking to access g_pManagedMessageCallback as it is
    // used on another thread (in ProcessCallstackMessage).
    //
    // A lock-free approach could be used to safely update and observe the value of the callback,
    // however that would introduce the edge case where the provided callback is unregistered
    // right before it is invoked.
    // This means that the unregistered callback would still be invoked, leading to potential issues
    // such as calling into an instanced method that has been disposed.
    //
    // For simplicitly just use locking for now as it prevents the above edge case.
    //
    lock_guard<mutex> lock(g_managedMessageCallbackMutex);
    if (g_pManagedMessageCallback != nullptr)
    {
        return E_FAIL;
    }
    g_pManagedMessageCallback = pCallback;

    return S_OK;
}

STDAPI DLLEXPORT UnregisterMonitorMessageCallback()
{
    lock_guard<mutex> lock(g_managedMessageCallbackMutex);
    g_pManagedMessageCallback = nullptr;

    return S_OK;
}

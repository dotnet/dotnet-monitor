// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ExceptionTracker.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

#define LogDebugV(format, ...) LogDebugV_(m_pLogger, format, __VA_ARGS__)
#define LogInformationV(format, ...) LogInformationV_(m_pLogger, format, __VA_ARGS__)
#define LogErrorV(format, ...) LogErrorV_(m_pLogger, format, __VA_ARGS__)

/*
    Exception Callbacks

    When a managed exception is thrown, the runtime will invoke the profiler with several
    callback methods during the exception processing phases. For a thrown exception, the
    following callbacks are invoked in the following order:
    - Phase 1: Throwing
      - ExceptionThrown: Invoked when the exception object has been thrown.
    - Phase 2: Finding Exception Catcher. Each frame on the callstack is inspected for a matching handler.
      The following are invoked for each frame starting at the leaf frame (the frame from which the exception is thrown):
      - ExceptionSearchFunctionEnter: Invoked when beginning to inspect a function for a matching handler.
      - If the function has handlers with filters (e.g. C#'s when clause), the following are invoked for each filter:
        - ExceptionSearchFilterEnter: Invoked when beginning to evaluate the filter.
        - ExceptionSearchFilterLeave: Invoked when finishing the evaluation of the filter.
      - ExceptionSearchCatcherFound: Invoked only if a matching filter is found.
      - ExceptionSearchFunctionLeave: Invoked when finishing the inspection of the function for a matching handler.
      If a matching handler was found, the exception catcher finding phase ends after the ExceptionSearchFunctionLeave callback.
    - Phase 3: Unwinding.
      The following are invoked for each frame starting at the leaf frame (the frame from which the exception is thrown):
      - ExceptionUnwindFunctionEnter: Invoked when beginning to unwind a frame.
      If a matching handler was found, the unwind phase ends after the invocation of
      the ExceptionUnwindFunctionEnter callback for the frame that contains the matching handler.
      - ExceptionUnwindFunctionLeave: Invoked when finishing the unwind of a frame.
      If all of the frames are unwound to completion, the process is terminated.

    Unhandled Exception Detection Algorithm

    A subset of the aboved described callbacks is used to determine if an unhandled exception has occurred:
    - ExceptionThrown: Record the originating FunctionID and ObjectID of the exception that is thrown.
    - ExceptionSearchCatcherFound: Record that the exception will be handled in a catching FunctionID.
    - ExceptionUnwindFunctionEnter: If the exception was not handled (ExceptionSearchCatcherFound was not invoked),
      then the exception is unhandled at this point. Do some extra bookkeeping in the case when the exception is
      handled so that the exception is cleared once frame with the corresponding catching FunctionID is encountered.

    Additionally, use GC callbacks, such as MovedReferences#, to update the exception ObjectID when compacting occurs.

    Current pitfalls:
    - Algorithm doesn't account for exceptions thrown within exception filters. These will be seen as a new set of
      callback invocations starting with throwing, searching, and unwinding, however, the callstack does not unwind
      out of the exception filter and the original exception processing is resumed.
    - Algorithm doesn't account for exceptions thrown within finally blocks. Presumably, these will be seen as new set
      of callback invocations starting with throwing, searching, and unwinding. The original exception processing is
      superceded by the new set of callbacks for the new exception.
*/


ExceptionTracker::ExceptionTracker(
    const shared_ptr<ILogger>& pLogger,
    const shared_ptr<ThreadDataManager> pThreadDataManager,
    ICorProfilerInfo2* pCorProfilerInfo)
{
    m_pCorProfilerInfo = pCorProfilerInfo;
    m_pLogger = pLogger;
    m_pThreadDataManager = pThreadDataManager;
}

void ExceptionTracker::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_ENABLE_STACK_SNAPSHOT;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_GC;
}

HRESULT ExceptionTracker::ExceptionThrown(ThreadID threadId, ObjectID objectId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(m_pThreadDataManager->SetExceptionObject(threadId, objectId));

    // Exception throwing is common; don't pay to calculate method name if it won't be logged.
    if (m_pLogger->IsEnabled(LogLevel::Debug))
    {
        FunctionID functionId = 0;
        hr = m_pCorProfilerInfo->DoStackSnapshot(
            threadId,
            ExceptionThrownStackSnapshotCallback,
            COR_PRF_SNAPSHOT_INFO::COR_PRF_SNAPSHOT_DEFAULT,
            &functionId,
            nullptr,
            0);

        if (FAILED(hr) && hr != CORPROF_E_STACKSNAPSHOT_ABORTED)
        {
            LogErrorV(_T("DoStackSnapshot failed in function %s: 0x%08x"), to_tstring(__func__).c_str(), hr);
            return hr;
        }

        tstring methodName;
        IfFailLogRet(GetFullyQualifiedMethodName(functionId, methodName));
        LogDebugV(_T("Exception thrown: %s"), methodName.c_str());
    }

    return S_OK;
}

HRESULT ExceptionTracker::ExceptionSearchCatcherFound(ThreadID threadId, FunctionID functionId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(m_pThreadDataManager->SetExceptionCatcherFunction(threadId, functionId));

    return S_OK;
}

HRESULT ExceptionTracker::ExceptionUnwindFunctionEnter(ThreadID threadId, FunctionID functionId)
{
    HRESULT hr = S_OK;

    ObjectID thrownObjectId;
    FunctionID catcherFunctionId;
    IfFailLogRet(m_pThreadDataManager->GetException(threadId, &thrownObjectId, &catcherFunctionId));

    if (ThreadData::NoFunctionId == catcherFunctionId)
    {
        tstring methodName;
        IfFailLogRet(GetFullyQualifiedMethodName(functionId, methodName));
        LogInformationV(_T("Exception unhandled: %s"), methodName.c_str());

        // Future: Block thread until collection is initiated of the desired artifact.
        // Possible serialization of some context of the exception and surrounding method
        // information such as locals and parameters.
    }
    else if (functionId == catcherFunctionId)
    {
        IfFailLogRet(m_pThreadDataManager->ClearException(threadId));

        // Exception handling is common; don't pay to calculate method name if it won't be logged.
        if (m_pLogger->IsEnabled(LogLevel::Debug))
        {
            tstring methodName;
            IfFailLogRet(GetFullyQualifiedMethodName(functionId, methodName));
            LogDebugV(_T("Exception handled: %s"), methodName.c_str());
        }
    }

    return S_OK;
}

HRESULT ExceptionTracker::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    HRESULT hr = S_OK;

    IfFailLogRet(m_pThreadDataManager->MovedReferences(cMovedObjectIDRanges, oldObjectIDRangeStart, newObjectIDRangeStart, cObjectIDRangeLength));

    return S_OK;
}

HRESULT ExceptionTracker::ExceptionThrownStackSnapshotCallback(
    FunctionID funcId,
    UINT_PTR ip,
    COR_PRF_FRAME_INFO frameInfo,
    ULONG32 contextSize,
    BYTE context[],
    void* clientData)
{
    // Callback is only used to get the function ID of the top frame.
    *static_cast<FunctionID*>(clientData) = funcId;

    // Cancel stack snapshot callbacks after the top frame.
    return S_FALSE;
}

HRESULT ExceptionTracker::GetFullyQualifiedMethodName(FunctionID functionId, tstring& fullMethodName)
{
    HRESULT hr = S_OK;

    ClassID classId;
    ModuleID moduleId;
    mdToken token;
    IfFailLogRet(m_pCorProfilerInfo->GetFunctionInfo(
        functionId,
        &classId,
        &moduleId,
        &token
    ));

    if (0 == (token & mdtMethodDef))
    {
        return E_FAIL;
    }
    mdMethodDef methodDef = token;

    ComPtr<IMetaDataImport2> pMetadataImport;
    IfFailLogRet(m_pCorProfilerInfo->GetModuleMetaData(
        moduleId,
        CorOpenFlags::ofRead,
        IID_IMetaDataImport2,
        (IUnknown**)&pMetadataImport
    ));

    // Get Module Name: typically the full path to the assembly
    ULONG moduleNameCount = 0; // Includes null-terminater
    IfFailLogRet(m_pCorProfilerInfo->GetModuleInfo(
        moduleId,
        nullptr,
        0,
        &moduleNameCount,
        nullptr,
        nullptr
    ));

    unique_ptr<WCHAR[]> pModulePath(new (nothrow) WCHAR[moduleNameCount]);
    IfNullRet(pModulePath);

    IfFailLogRet(m_pCorProfilerInfo->GetModuleInfo(
        moduleId,
        nullptr,
        moduleNameCount,
        nullptr,
        pModulePath.get(),
        nullptr
    ));

    // Get Class Name: The namespace + type name in the form <Namespace>.<Type>
    tstring className;
    if (0 == classId)
    {
        // Probably a value type e.g. struct
        className.assign(_T("[Unknown]"));
    }
    else
    {
        mdTypeDef typeDef = mdTokenNil;
        IfFailLogRet(m_pCorProfilerInfo->GetClassIDInfo(
            classId,
            nullptr,
            &typeDef
        ));

        ULONG classNameCount = 0; // Includes null-terminater
        IfFailLogRet(pMetadataImport->GetTypeDefProps(
            typeDef,
            nullptr,
            0,
            &classNameCount,
            nullptr,
            nullptr
        ));

        unique_ptr<WCHAR[]> pClassName(new (nothrow) WCHAR[classNameCount]);
        IfNullRet(pClassName);

        IfFailLogRet(pMetadataImport->GetTypeDefProps(
            typeDef,
            pClassName.get(),
            classNameCount,
            nullptr,
            nullptr,
            nullptr
        ));

        className.assign(pClassName.get());
    }

    // Get Method Name
    ULONG methodNameCount = 0; // Includes null-terminater
    IfFailLogRet(pMetadataImport->GetMethodProps(
        methodDef,
        nullptr,
        nullptr,
        0,
        &methodNameCount,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr
    ));

    unique_ptr<WCHAR[]> pMethodName(new (nothrow) WCHAR[methodNameCount]);
    IfNullRet(pMethodName);

    IfFailLogRet(pMetadataImport->GetMethodProps(
        methodDef,
        nullptr,
        pMethodName.get(),
        methodNameCount,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr
    ));

    tstring modulePath(pModulePath.get());

    // The full method name should be in the following format: <ModuleName>!<Namespace>.<TypeName>.<MethodName>
    // Example: ConsoleApp1.dll!ConsoleApp1.Program.Main
    fullMethodName.clear();
    fullMethodName.append(modulePath.substr(modulePath.find_last_of(_T("/\\")) + 1));
    fullMethodName.append(_T("!"));
    fullMethodName.append(className);
    fullMethodName.append(_T("."));
    fullMethodName.append(pMethodName.get());

    return S_OK;
}

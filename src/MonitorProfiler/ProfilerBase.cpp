// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ProfilerBase.h"
#include "corhlpr.h"
#include "macros.h"

ProfilerBase::ProfilerBase() :
    m_pCorProfilerInfo(nullptr)
{
}

STDMETHODIMP ProfilerBase::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    ExpectedPtr(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;

    IfFailRet(pICorProfilerInfoUnk->QueryInterface(
        IID_ICorProfilerInfo12,
        reinterpret_cast<void **>(&m_pCorProfilerInfo)));

    return S_OK;
}

STDMETHODIMP ProfilerBase::Shutdown()
{
    m_pCorProfilerInfo.Release();

    return S_OK;
}

STDMETHODIMP ProfilerBase::AppDomainCreationStarted(AppDomainID appDomainId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AssemblyLoadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleLoadStarted(ModuleID moduleId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleUnloadStarted(ModuleID moduleId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ClassLoadStarted(ClassID classId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ClassUnloadStarted(ClassID classId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::FunctionUnloadStarted(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITFunctionPitched(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ThreadCreated(ThreadID threadId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ThreadDestroyed(ThreadID threadId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingClientInvocationStarted()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingClientSendingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingClientReceivingReply(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingClientInvocationFinished()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingServerReceivingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingServerInvocationStarted()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingServerInvocationReturned()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RemotingServerSendingReply(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeSuspendFinished()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeSuspendAborted()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeResumeStarted()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeResumeFinished()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeThreadSuspended(ThreadID threadId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RuntimeThreadResumed(ThreadID threadId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RootReferences(ULONG cRootRefs, ObjectID rootRefIds[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionThrown(ObjectID thrownObjectId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionSearchFunctionLeave()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionSearchFilterEnter(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionSearchFilterLeave()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionSearchCatcherFound(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionOSHandlerEnter(UINT_PTR __unused)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionOSHandlerLeave(UINT_PTR __unused)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionUnwindFunctionLeave()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionUnwindFinallyLeave()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionCatcherLeave()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionCLRCatcherFound()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ExceptionCLRCatcherExecute()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::GarbageCollectionFinished()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::HandleCreated(GCHandleID handleId, ObjectID initialObjectId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::HandleDestroyed(GCHandleID handleId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::InitializeForAttach(IUnknown *pCorProfilerInfoUnk, void *pvClientData, UINT cbClientData)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ProfilerAttachComplete()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ProfilerDetachSucceeded()
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::GetAssemblyReferences(const WCHAR *wszAssemblyPath, ICorProfilerAssemblyReferenceProvider *pAsmRefProvider)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::DynamicMethodUnloaded(FunctionID functionId)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::EventPipeEventDelivered(
        EVENTPIPE_PROVIDER provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        ThreadID eventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[])
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::EventPipeProviderCreated(EVENTPIPE_PROVIDER provider)
{
    return S_OK;
}

STDMETHODIMP ProfilerBase::LoadAsNotficationOnly(BOOL *pbNotificationOnly)
{
    return S_OK;
}

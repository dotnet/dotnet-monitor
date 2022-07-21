// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "com.h"
#include "cor.h"
#include "corprof.h"
#include "refcount.h"

class ProfilerBase :
    public RefCount,
    public ICorProfilerCallback11
{
protected:
    ComPtr<ICorProfilerInfo12> m_pCorProfilerInfo;

public:
    ProfilerBase();
    ~ProfilerBase() {}

    DEFINE_DELEGATED_REFCOUNT_ADDREF(ProfilerBase)
    DEFINE_DELEGATED_REFCOUNT_RELEASE(ProfilerBase)
    BEGIN_COM_MAP(ProfilerBase)
        COM_INTERFACE_ENTRY(IUnknown)
        COM_INTERFACE_ENTRY(ICorProfilerCallback11)
        COM_INTERFACE_ENTRY(ICorProfilerCallback10)
        COM_INTERFACE_ENTRY(ICorProfilerCallback9)
        COM_INTERFACE_ENTRY(ICorProfilerCallback8)
        COM_INTERFACE_ENTRY(ICorProfilerCallback7)
        COM_INTERFACE_ENTRY(ICorProfilerCallback6)
        COM_INTERFACE_ENTRY(ICorProfilerCallback5)
        COM_INTERFACE_ENTRY(ICorProfilerCallback4)
        COM_INTERFACE_ENTRY(ICorProfilerCallback3)
        COM_INTERFACE_ENTRY(ICorProfilerCallback2)
        COM_INTERFACE_ENTRY(ICorProfilerCallback)
    END_COM_MAP()

    // ICorProfilerCallback
    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(Shutdown)() override;
    STDMETHOD(AppDomainCreationStarted)(AppDomainID appDomainId) override;
    STDMETHOD(AppDomainCreationFinished)(AppDomainID appDomainId, HRESULT hrStatus) override;
    STDMETHOD(AppDomainShutdownStarted)(AppDomainID appDomainId) override;
    STDMETHOD(AppDomainShutdownFinished)(AppDomainID appDomainId, HRESULT hrStatus) override;
    STDMETHOD(AssemblyLoadStarted)(AssemblyID assemblyId) override;
    STDMETHOD(AssemblyLoadFinished)(AssemblyID assemblyId, HRESULT hrStatus) override;
    STDMETHOD(AssemblyUnloadStarted)(AssemblyID assemblyId) override;
    STDMETHOD(AssemblyUnloadFinished)(AssemblyID assemblyId, HRESULT hrStatus) override;
    STDMETHOD(ModuleLoadStarted)(ModuleID moduleId) override;
    STDMETHOD(ModuleLoadFinished)(ModuleID moduleId, HRESULT hrStatus) override;
    STDMETHOD(ModuleUnloadStarted)(ModuleID moduleId) override;
    STDMETHOD(ModuleUnloadFinished)(ModuleID moduleId, HRESULT hrStatus) override;
    STDMETHOD(ModuleAttachedToAssembly)(ModuleID moduleId, AssemblyID AssemblyId) override;
    STDMETHOD(ClassLoadStarted)(ClassID classId) override;
    STDMETHOD(ClassLoadFinished)(ClassID classId, HRESULT hrStatus) override;
    STDMETHOD(ClassUnloadStarted)(ClassID classId) override;
    STDMETHOD(ClassUnloadFinished)(ClassID classId, HRESULT hrStatus) override;
    STDMETHOD(FunctionUnloadStarted)(FunctionID functionId) override;
    STDMETHOD(JITCompilationStarted)(FunctionID functionId, BOOL fIsSafeToBlock) override;
    STDMETHOD(JITCompilationFinished)(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    STDMETHOD(JITCachedFunctionSearchStarted)(FunctionID functionId, BOOL* pbUseCachedFunction) override;
    STDMETHOD(JITCachedFunctionSearchFinished)(FunctionID functionId, COR_PRF_JIT_CACHE result) override;
    STDMETHOD(JITFunctionPitched)(FunctionID functionId) override;
    STDMETHOD(JITInlining)(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override;
    STDMETHOD(ThreadCreated)(ThreadID threadId) override;
    STDMETHOD(ThreadDestroyed)(ThreadID threadId) override;
    STDMETHOD(ThreadAssignedToOSThread)(ThreadID managedThreadId, DWORD osThreadId) override;
    STDMETHOD(RemotingClientInvocationStarted)() override;
    STDMETHOD(RemotingClientSendingMessage)(GUID* pCookie, BOOL fIsAsync) override;
    STDMETHOD(RemotingClientReceivingReply)(GUID* pCookie, BOOL fIsAsync) override;
    STDMETHOD(RemotingClientInvocationFinished)() override;
    STDMETHOD(RemotingServerReceivingMessage)(GUID* pCookie, BOOL fIsAsync) override;
    STDMETHOD(RemotingServerInvocationStarted)() override;
    STDMETHOD(RemotingServerInvocationReturned)() override;
    STDMETHOD(RemotingServerSendingReply)(GUID* pCookie, BOOL fIsAsync) override;
    STDMETHOD(UnmanagedToManagedTransition)(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
    STDMETHOD(ManagedToUnmanagedTransition)(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
    STDMETHOD(RuntimeSuspendStarted)(COR_PRF_SUSPEND_REASON suspendReason) override;
    STDMETHOD(RuntimeSuspendFinished)() override;
    STDMETHOD(RuntimeSuspendAborted)() override;
    STDMETHOD(RuntimeResumeStarted)() override;
    STDMETHOD(RuntimeResumeFinished)() override;
    STDMETHOD(RuntimeThreadSuspended)(ThreadID threadId) override;
    STDMETHOD(RuntimeThreadResumed)(ThreadID threadId) override;
    STDMETHOD(MovedReferences)(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
    STDMETHOD(ObjectAllocated)(ObjectID objectId, ClassID classId) override;
    STDMETHOD(ObjectsAllocatedByClass)(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]) override;
    STDMETHOD(ObjectReferences)(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override;
    STDMETHOD(RootReferences)(ULONG cRootRefs, ObjectID rootRefIds[]) override;
    STDMETHOD(ExceptionThrown)(ObjectID thrownObjectId) override;
    STDMETHOD(ExceptionSearchFunctionEnter)(FunctionID functionId) override;
    STDMETHOD(ExceptionSearchFunctionLeave)() override;
    STDMETHOD(ExceptionSearchFilterEnter)(FunctionID functionId) override;
    STDMETHOD(ExceptionSearchFilterLeave)() override;
    STDMETHOD(ExceptionSearchCatcherFound)(FunctionID functionId) override;
    STDMETHOD(ExceptionOSHandlerEnter)(UINT_PTR __unused) override;
    STDMETHOD(ExceptionOSHandlerLeave)(UINT_PTR __unused) override;
    STDMETHOD(ExceptionUnwindFunctionEnter)(FunctionID functionId) override;
    STDMETHOD(ExceptionUnwindFunctionLeave)() override;
    STDMETHOD(ExceptionUnwindFinallyEnter)(FunctionID functionId) override;
    STDMETHOD(ExceptionUnwindFinallyLeave)() override;
    STDMETHOD(ExceptionCatcherEnter)(FunctionID functionId, ObjectID objectId) override;
    STDMETHOD(ExceptionCatcherLeave)() override;
    STDMETHOD(COMClassicVTableCreated)(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots) override;
    STDMETHOD(COMClassicVTableDestroyed)(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable) override;
    STDMETHOD(ExceptionCLRCatcherFound)() override;
    STDMETHOD(ExceptionCLRCatcherExecute)() override;

    // ICorProfilerCallback2
    STDMETHOD(ThreadNameChanged)(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    STDMETHOD(GarbageCollectionStarted)(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
    STDMETHOD(SurvivingReferences)(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
    STDMETHOD(GarbageCollectionFinished)() override;
    STDMETHOD(FinalizeableObjectQueued)(DWORD finalizerFlags, ObjectID objectID) override;
    STDMETHOD(RootReferences2)(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[]) override;
    STDMETHOD(HandleCreated)(GCHandleID handleId, ObjectID initialObjectId) override;
    STDMETHOD(HandleDestroyed)(GCHandleID handleId) override;

    // ICorProfilerCallback3
    STDMETHOD(InitializeForAttach)(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override;
    STDMETHOD(ProfilerAttachComplete)() override;
    STDMETHOD(ProfilerDetachSucceeded)() override;

    // ICorProfilerCallback4
    STDMETHOD(ReJITCompilationStarted)(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock) override;
    STDMETHOD(GetReJITParameters)(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl) override;
    STDMETHOD(ReJITCompilationFinished)(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    STDMETHOD(ReJITError)(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus) override;
    STDMETHOD(MovedReferences2)(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
    STDMETHOD(SurvivingReferences2)(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;

    // ICorProfilerCallback5
    STDMETHOD(ConditionalWeakTableElementReferences)(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[]) override;

    // ICorProfilerCallback6
    STDMETHOD(GetAssemblyReferences)(const WCHAR* wszAssemblyPath, ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;

    // ICorProfilerCallback7
    STDMETHOD(ModuleInMemorySymbolsUpdated)(ModuleID moduleId) override;

    // ICorProfilerCallback8
    STDMETHOD(DynamicMethodJITCompilationStarted)(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader) override;
    STDMETHOD(DynamicMethodJITCompilationFinished)(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;

    // ICorProfilerCallback9
    STDMETHOD(DynamicMethodUnloaded)(FunctionID functionId) override;

    // ICorProfilerCallback10
    STDMETHOD(EventPipeEventDelivered)(
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
        UINT_PTR stackFrames[]) override;
    STDMETHOD(EventPipeProviderCreated)(EVENTPIPE_PROVIDER provider) override;

    // ICorProfilerCallback11
    STDMETHOD(LoadAsNotficationOnly)(BOOL *pbNotificationOnly) override;
};

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../EventProvider/ProfilerEventProvider.h"
#include "../Utilities/ClrData.h"
#include <memory>
#include "Stack.h"

/// <summary>
/// Represents callstack information.
/// Note that we serialize enough Clr metadata to reconstruct the names in dotnet-monitor. The stacks themselves are
/// offsets and FunctionId's.
/// </summary>
class StacksEventProvider
{
    public:
        static HRESULT CreateProvider(ICorProfilerInfo12* profilerInfo, std::unique_ptr<StacksEventProvider>& eventProvider);

        HRESULT WriteCallstack(const Stack& stack);
        HRESULT WriteClassData(ClassID classId, const ClassData& classData);
        HRESULT WriteFunctionData(FunctionID functionId, const FunctionData& classData);
        HRESULT WriteModuleData(ModuleID moduleId, const ModuleData& classData);
        HRESULT WriteTokenData(ModuleID moduleId, mdTypeDef typeDef, const TokenData& tokenData);
        HRESULT WriteEndEvent();

    private:
        StacksEventProvider(ICorProfilerInfo12* profilerInfo, std::unique_ptr<ProfilerEventProvider> & eventProvider) :
            _profilerInfo(profilerInfo), _provider(std::move(eventProvider))
        {
        }

        static const WCHAR* ProviderName;

        HRESULT DefineEvents();

        ComPtr<ICorProfilerInfo12> _profilerInfo;
        std::unique_ptr<ProfilerEventProvider> _provider;
        
        const WCHAR* CallstackPayloads[3] = { _T("ThreadId"), _T("FunctionIds"), _T("IpOffsets") };
        std::unique_ptr<ProfilerEvent<UINT64, std::vector<UINT64>, std::vector<UINT64>>> _callstackEvent;

        //Note we will either send a ClassId or a ClassToken. For Shared generic functions, there is no ClassID.
        const WCHAR* FunctionPayloads[6] = { _T("FunctionId"), _T("ClassId"), _T("ClassToken"), _T("ModuleId"), _T("Name"), _T("TypeArgs") };
        std::unique_ptr<ProfilerEvent<UINT64, UINT64, UINT32, UINT64, tstring, std::vector<UINT64>>> _functionEvent;

        //We cannot retrieve detailed information for some ClassIds. Flags is used to indicate these conditions.
        const WCHAR* ClassPayloads[5] = { _T("ClassId"), _T("ModuleId"), _T("Token"), _T("Flags"), _T("TypeArgs") };
        std::unique_ptr<ProfilerEvent<UINT64, UINT64, UINT32, UINT32, std::vector<UINT64>>> _classEvent;

        const WCHAR* TokenPayloads[4] = { _T("ModuleId"), _T("Token"), _T("OuterToken"), _T("Name") };
        std::unique_ptr<ProfilerEvent<UINT64, UINT32, UINT32, tstring>> _tokenEvent;

        const WCHAR* ModulePayloads[2] = { _T("ModuleId"), _T("Name") };
        std::unique_ptr<ProfilerEvent<UINT64, tstring>> _moduleEvent;

        //TODO Once ProfilerEvent supports it, use an event with no payload.
        const WCHAR* EndPayloads[1] = { _T("Unused") };
        std::unique_ptr<ProfilerEvent<UINT32>> _endEvent;
};
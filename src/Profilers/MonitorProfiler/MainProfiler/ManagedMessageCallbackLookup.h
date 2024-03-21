// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <unordered_map>
#include <string>
#include <mutex>
#include "cor.h"
#include "corprof.h"
#include "tstring.h"
#include "CommonUtilities/GuidHashCompare.h"


typedef INT32 (STDMETHODCALLTYPE *ManagedMessageCallback)(INT16, const BYTE*, UINT64);

class ManagedMessageCallbackLookup
{
    public:
        void Set(GUID id, ManagedMessageCallback callback);
        bool Invoke(GUID id, INT16 command, const BYTE* payload, UINT64 payloadSize, HRESULT& hr);
        void Remove(GUID id);
    private:
        std::unordered_map<GUID, ManagedMessageCallback, GuidHashCompare> m_callbacks;
        std::mutex m_mutex;
};

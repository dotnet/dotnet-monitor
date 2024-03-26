// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <unordered_map>
#include <string>
#include <mutex>
#include "cor.h"
#include "corprof.h"
#include "tstring.h"
#include "Messages.h"

typedef HRESULT (STDMETHODCALLTYPE *ManagedMessageCallback)(UINT16, const BYTE*, UINT64);

class ManagedMessageCallbackManager
{
    public:
        HRESULT DispatchMessage(const IpcMessage& message);
        bool TryRegister(unsigned short commandSet, ManagedMessageCallback callback);
        void Unregister(unsigned short commandSet);
    private:
        bool TryGetCallback(unsigned short commandSet, ManagedMessageCallback& callback);
        std::unordered_map<unsigned short, ManagedMessageCallback> m_callbacks;
        std::mutex m_mutex;
};

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <unordered_map>
#include <mutex>
#include <functional>
#include "cor.h"
#include "corprof.h"
#include "Messages.h"

typedef HRESULT (STDMETHODCALLTYPE *ManagedMessageCallback)(UINT16, const BYTE*, UINT64);

class MessageCallbackManager
{
    public:
        HRESULT DispatchMessage(const IpcMessage& message);
        bool IsRegistered(unsigned short commandSet);
        bool TryRegister(unsigned short commandSet, std::function<HRESULT (const IpcMessage& message)> callback);
        bool TryRegister(unsigned short commandSet, ManagedMessageCallback pCallback);
        void Unregister(unsigned short commandSet);
    private:
        bool TryGetCallback(unsigned short commandSet, std::function<HRESULT (const IpcMessage& message)>& callback);
        std::unordered_map<unsigned short, std::function<HRESULT (const IpcMessage& message)>> m_callbacks;
        //
        // Ideally we would use a single std::shared_mutex instead, but we are targeting C++11 without
        // an easy way to upgrade to C++17 at this time, so we use two separate mutexes instead to
        // allow for IsRegistered to be used while a DispatchMessage call is in progress.
        //
        std::mutex m_dispatchMutex;
        std::mutex m_lookupMutex;
};

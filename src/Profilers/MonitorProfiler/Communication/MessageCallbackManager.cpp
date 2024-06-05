// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "MessageCallbackManager.h"

bool MessageCallbackManager::IsRegistered(unsigned short commandSet)
{
    std::lock_guard<std::mutex> lookupLock(m_lookupMutex);

    std::function<HRESULT (const IpcMessage& message)> existingCallback;
    return TryGetCallback(commandSet, existingCallback);
}

bool MessageCallbackManager::TryRegister(unsigned short commandSet, ManagedMessageCallback pCallback)
{
    return TryRegister(commandSet, [pCallback](const IpcMessage& message)-> HRESULT
    {
        return pCallback(message.Command, message.Payload.data(), message.Payload.size());
    });
}

bool MessageCallbackManager::TryRegister(unsigned short commandSet, std::function<HRESULT (const IpcMessage& message)> callback)
{
    std::lock_guard<std::mutex> dispatchLock(m_dispatchMutex);
    std::lock_guard<std::mutex> lookupLock(m_lookupMutex);

    std::function<HRESULT (const IpcMessage& message)> existingCallback;
    if (TryGetCallback(commandSet, existingCallback))
    {
        return false;
    }

    m_callbacks[commandSet] = callback;
    return true;
}

HRESULT MessageCallbackManager::DispatchMessage(const IpcMessage& message)
{
    std::lock_guard<std::mutex> dispatchLock(m_dispatchMutex);

    std::function<HRESULT (const IpcMessage& message)> callback;
    if (!TryGetCallback(message.CommandSet, callback))
    {
        return E_FAIL;
    }

    return callback(message);
}

void MessageCallbackManager::Unregister(unsigned short commandSet)
{
    std::lock_guard<std::mutex> dispatchLock(m_dispatchMutex);
    std::lock_guard<std::mutex> lookupLock(m_lookupMutex);

    m_callbacks.erase(commandSet);
}

bool MessageCallbackManager::TryGetCallback(unsigned short commandSet, std::function<HRESULT (const IpcMessage& message)>& callback)
{
    // Do not lock here, this method is private and it's the responsibility of the caller to lock
    auto const& it = m_callbacks.find(commandSet);
    if (it != m_callbacks.end())
    {
        callback = it->second;
        return true;
    }

    return false;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "MessageCallbackManager.h"

bool MessageCallbackManager::IsRegistered(unsigned short commandSet)
{
    std::lock_guard<std::mutex> lock(m_commandSetsMutex);
    return m_commandSets.find(commandSet) != m_commandSets.end();
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
    std::lock_guard<std::mutex> lock(m_mutex);
    std::lock_guard<std::mutex> commandSetsLock(m_commandSetsMutex);

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
    std::lock_guard<std::mutex> lock(m_mutex);

    std::function<HRESULT (const IpcMessage& message)> callback;
    if (!TryGetCallback(message.CommandSet, callback))
    {
        return E_FAIL;
    }

    return callback(message);
}

void MessageCallbackManager::Unregister(unsigned short commandSet)
{
    std::lock_guard<std::mutex> lock(m_mutex);
    std::lock_guard<std::mutex> commandSetsLock(m_commandSetsMutex);

    m_callbacks.erase(commandSet);
    m_commandSets.erase(commandSet);
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

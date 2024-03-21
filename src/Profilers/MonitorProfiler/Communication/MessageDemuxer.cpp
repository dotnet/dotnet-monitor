// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "MessageDemuxer.h"

bool MessageDemuxer::TryRegister(unsigned short commandSet, ManagedMessageCallback callback)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    ManagedMessageCallback existingCallback;
    if (TryGetCallback(commandSet, existingCallback))
    {
        return false;
    }

    m_callbacks[commandSet] = callback;
    return true;
}

HRESULT MessageDemuxer::OnMessage(const IpcMessage& message)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    ManagedMessageCallback callback;
    if (!TryGetCallback(message.CommandSet, callback))
    {
        return E_FAIL;
    }

    return callback(message.Command, message.Payload.data(), message.Payload.size());
}

bool MessageDemuxer::TryGetCallback(unsigned short commandSet, ManagedMessageCallback& callback)
{
    auto const& it = m_callbacks.find(commandSet);
    if (it != m_callbacks.end())
    {
        callback = it->second;
        return true;
    }

    return false;
}

void MessageDemuxer::Unregister(unsigned short commandSet)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    m_callbacks.erase(commandSet);
}

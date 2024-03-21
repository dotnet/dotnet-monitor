// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ManagedMessageCallbackLookup.h"

void ManagedMessageCallbackLookup::Set(GUID id, ManagedMessageCallback callback)
{
    std::lock_guard<std::mutex> lock(m_mutex);
    m_callbacks[id] = callback;
}

bool ManagedMessageCallbackLookup::Invoke(GUID id, INT16 command, const BYTE* payload, UINT64 payloadSize, HRESULT& hr)
{
    std::lock_guard<std::mutex> lock(m_mutex);

    auto const& it = m_callbacks.find(id);
    if (it != m_callbacks.end())
    {
        hr = it->second(command, payload, payloadSize);
        return true;
    }
    return false;
}

void ManagedMessageCallbackLookup::Remove(GUID id)
{
    std::lock_guard<std::mutex> lock(m_mutex);
    m_callbacks.erase(id);
}

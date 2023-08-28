// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ThreadNameCache.h"

void ThreadNameCache::Set(ThreadID id, tstring&& name)
{
    std::lock_guard<std::mutex> lock(_mutex);
    _threadNames[id] = std::move(name);
}

void ThreadNameCache::Set(ThreadID id, const tstring& name)
{
    Set(id, tstring(name));
}

bool ThreadNameCache::Get(ThreadID id, tstring& name)
{
    std::lock_guard<std::mutex> lock(_mutex);

    auto const& it = _threadNames.find(id);
    if (it != _threadNames.end())
    {
        name = it->second;
        return true;
    }
    return false;
}

void ThreadNameCache::Remove(ThreadID id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    _threadNames.erase(id);
}

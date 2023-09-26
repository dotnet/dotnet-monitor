// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <unordered_map>
#include <string>
#include <mutex>
#include "cor.h"
#include "corprof.h"
#include "tstring.h"

class ThreadNameCache
{
    public:
        void Set(ThreadID id, tstring&& name);
        void Set(ThreadID id, const tstring& name);
        bool Get(ThreadID id, tstring& name);
        void Remove(ThreadID id);
    private:
        std::unordered_map<ThreadID, tstring> _threadNames;
        std::mutex _mutex;
};

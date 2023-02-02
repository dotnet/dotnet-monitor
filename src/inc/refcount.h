// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <atomic>

#define DEFINE_DELEGATED_METHOD(base_cls, result_type, method, derived_paren_args, base_paren_args) \
    STDMETHOD_(result_type, method) derived_paren_args override\
    { \
        return base_cls::method base_paren_args; \
    }

#define DEFINE_DELEGATED_REFCOUNT_ADDREF(cls) \
    DEFINE_DELEGATED_METHOD(RefCount, ULONG, AddRef, (void), ())
#define DEFINE_DELEGATED_REFCOUNT_RELEASE(cls) \
    DEFINE_DELEGATED_METHOD(RefCount, ULONG, Release, (void), ())

class RefCount
{
private:
    std::atomic<int> m_nRefCount;

public:
    RefCount() : m_nRefCount(0) {}

    virtual ~RefCount() {}
    
    ULONG STDMETHODCALLTYPE AddRef()
    {
        return std::atomic_fetch_add(&m_nRefCount, 1) + 1;
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        int count = std::atomic_fetch_sub(&m_nRefCount, 1) - 1;

        if (count <= 0)
        {
            delete this;
        }

        return count;
    }
};

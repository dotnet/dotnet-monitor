// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "tstring.h"
#include <vector>
#include <string>

/// <summary>
/// Helper class to convert types to COR_PRF_EVENTPIPE_PARAM_DESC representations
/// TODO: Need to map out remaining types, which are not used for the current data.
/// </summary>
template <typename T>
class EventTypeMapping
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        // If we got here, it means we do not know how to convert the type to a COR_PRF_EVENTPIPE_PARAM_DESC
        // We are not allowed to say static_assert(false), even if we never bind to this specialization.
        static_assert(sizeof(T) != sizeof(T), "Invalid type");
    }
};

template<>
class EventTypeMapping<UINT32>
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        descriptor.type = COR_PRF_EVENTPIPE_UINT32;
        descriptor.elementType = 0;
    }
};

template<>
class EventTypeMapping<UINT64>
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        descriptor.type = COR_PRF_EVENTPIPE_UINT64;
        descriptor.elementType = 0;
    }
};

template<>
class EventTypeMapping<tstring>
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        descriptor.type = COR_PRF_EVENTPIPE_STRING;
        descriptor.elementType = 0;
    }
};

template<>
class EventTypeMapping<GUID>
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        descriptor.type = COR_PRF_EVENTPIPE_GUID;
        descriptor.elementType = 0;
    }
};

template<>
class EventTypeMapping<std::vector<UINT64>>
{
public:
    void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
    {
        descriptor.type = COR_PRF_EVENTPIPE_ARRAY;
        descriptor.elementType = COR_PRF_EVENTPIPE_UINT64;
    }
};

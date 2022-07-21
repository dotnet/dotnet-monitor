#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include <vector>
#include <string>

/// <summary>
/// Helper class to convert types to COR_PRF_EVENTPIPE_PARAM_DESC representations
/// TODO: Need to map out remaining types, which are not used for the current data.
/// </summary>
class EventTypeMapping
{
public:
    template<typename T>
    static void GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor);

    template<>
    static void GetType<UINT32>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor);

    template<>
    static void GetType<UINT64>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor);

    template<>
    static void GetType<std::wstring>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor);

    template<>
    static void GetType<std::vector<UINT64>>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor);
};

template<typename T>
static void EventTypeMapping::GetType(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
{
    //If we got here, it means we do not know how to convert the type to a COR_PRF_EVENTPIPE_PARAM_DESC
    static_assert(false, "Invalid type.");
}

template<>
static void EventTypeMapping::GetType<UINT32>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
{
    descriptor.elementType = 0;
    descriptor.type = COR_PRF_EVENTPIPE_UINT32;
}

template<>
static void EventTypeMapping::GetType<UINT64>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
{
    descriptor.type = COR_PRF_EVENTPIPE_UINT64;
    descriptor.elementType = 0;
}

template<>
static void EventTypeMapping::GetType<std::wstring>(COR_PRF_EVENTPIPE_PARAM_DESC & descriptor)
{
    descriptor.type = COR_PRF_EVENTPIPE_STRING;
    descriptor.elementType = 0;
}

template<>
static void EventTypeMapping::GetType<std::vector<UINT64>>(COR_PRF_EVENTPIPE_PARAM_DESC& descriptor)
{
    descriptor.type = COR_PRF_EVENTPIPE_ARRAY;
    descriptor.elementType = COR_PRF_EVENTPIPE_UINT64;
}
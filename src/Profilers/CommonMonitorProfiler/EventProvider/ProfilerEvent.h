// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "EventTypeMapping.h"
#include <vector>
#include <string>
#include <memory>

/// <summary>
/// Helper class used to write EventSource data.
/// Initialize is used to declare the data types and fill the COR_PRF_EVENTPIPE_PARAM_DESC structure.
/// WritePayload is used to create COR_PRF_EVENT_DATA and write the actual data.
/// We rely on variadic templates and template specialization for both of the above.
///
/// IMPORTANT All data to WritePayload must be valid until the data is written by ICorProfiler12, not just assigned to COR_PRF_EVENT_DATA.
/// The profiler API uses memory addresses even for primitive types.
/// </summary>
template<typename... Args>
class ProfilerEvent
{
    friend class ProfilerEventProvider;
public:
    HRESULT WritePayload(const Args&... args);

private:
    ProfilerEvent(ICorProfilerInfo12* profilerInfo);

    template<size_t index, typename T, typename... TArgs>
    HRESULT Initialize(const WCHAR* (&names)[sizeof...(Args)]);

    template<size_t index>
    HRESULT Initialize(const WCHAR* (&names)[sizeof...(Args)]);

    template<size_t index, typename T, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const T& first, TArgs... rest);

    // In order to specialize with variadic templates, all the overloads have to declare their template parameters
    template<size_t index, typename T = tstring, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const tstring& first, TArgs... rest);

    template<size_t index, typename T = GUID, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const GUID& first, TArgs... rest);

    template<size_t index, typename T, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const std::vector<typename T::value_type>& first, TArgs... rest);

    template<typename T>
    static std::vector<BYTE> GetEventBuffer(const std::vector<T>& data);

    template<typename T>
    static void WriteToBuffer(BYTE* pBuffer, size_t* pOffset, const T& value);

    template<size_t index>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data);

private:

    //TODO We don't have a way of modeling data with no payload. 0 sized arrays are not allowed.
    COR_PRF_EVENTPIPE_PARAM_DESC _descriptor[sizeof...(Args)];

    // Note this field is set by ProfilerEventProvider.
    EVENTPIPE_EVENT _event;
    ComPtr<ICorProfilerInfo12> _profilerInfo;
};

template<typename... Args>
ProfilerEvent<Args...>::ProfilerEvent(ICorProfilerInfo12* profilerInfo) : _event(0), _profilerInfo(profilerInfo)
{
    memset(_descriptor, 0, sizeof(_descriptor));
}

template<typename... Args>
template<size_t index, typename T, typename... TArgs>
HRESULT ProfilerEvent<Args...>::Initialize(const WCHAR* (&names)[sizeof...(Args)])
{
    _descriptor[index].name = names[index];
    EventTypeMapping<T> typeMapper;
    typeMapper.GetType(_descriptor[index]);
    return Initialize<index + 1, TArgs...>(names);
}

template<typename... Args>
template<size_t index>
HRESULT ProfilerEvent<Args...>::Initialize(const WCHAR* (&names)[sizeof...(Args)])
{
    //CONSIDER An alternate design is to make each event call DefineEvent instead of leaving this responsibility on the provider.
    return S_OK;
}

template<typename... Args>
HRESULT ProfilerEvent<Args...>::WritePayload(const Args&... args)
{
    COR_PRF_EVENT_DATA data[sizeof...(Args)];
    return WritePayload<0, Args...>(data, args...);
}

template<typename... Args>
template<size_t index, typename T, typename... TArgs>
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const T& first, TArgs... rest)
{
    data[index].ptr = reinterpret_cast<UINT64>(&first);
    data[index].size = static_cast<UINT32>(sizeof(T));
    data[index].reserved = 0;
    return WritePayload<index + 1, TArgs...>(data, rest...);
}

template<typename... Args>
template<size_t index, typename T, typename... TArgs>
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const tstring& first, TArgs... rest)
{
    //Note this works for empty strings.
    data[index].ptr = reinterpret_cast<UINT64>(first.c_str());
    data[index].size = static_cast<UINT32>((first.size() + 1) * sizeof(WCHAR)); // + 1 for null terminator.
    data[index].reserved = 0;
    return WritePayload<index + 1, TArgs...>(data, rest...);
}

 template<typename... Args>
 template<size_t index, typename T, typename... TArgs>
 HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const std::vector<typename T::value_type>& first, TArgs... rest)
 {
     // This value must stay in scope during all the WritePayload functions.
     std::vector<BYTE> buffer(0);

     if (first.size() == 0)
     {
         data[index].ptr = 0;
         data[index].size = 0;
         data[index].reserved = 0;
     }
     else
     {
         buffer = std::move(GetEventBuffer(first));
         data[index].ptr = reinterpret_cast<UINT64>(buffer.data());
         data[index].size = static_cast<UINT32>(buffer.size());
         data[index].reserved = 0;
     }
     return WritePayload<index + 1, TArgs...>(data, rest...);
 }

template<typename... Args>
template<size_t index, typename T, typename... TArgs>
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const GUID& first, TArgs... rest)
{
    // Manually copy the GUID into a buffer and pass the buffer address.
    // We can't pass the GUID address directly (or use sizeof(GUID)) because the GUID may have padding between its different data segments.
    const int GUID_FLAT_SIZE = sizeof(INT32) + sizeof(INT16) + sizeof(INT16) + sizeof(INT64);
    static_assert(GUID_FLAT_SIZE == 128 / 8, "Incorrect flat GUID size.");

    BYTE buffer[GUID_FLAT_SIZE] = {0};
    int offset = 0;

    memcpy(&buffer[offset], &first.Data1, sizeof(INT32));
    offset += sizeof(INT32);
    memcpy(&buffer[offset], &first.Data2, sizeof(INT16));
    offset += sizeof(INT16);
    memcpy(&buffer[offset], &first.Data3, sizeof(INT16));
    offset += sizeof(INT16);
    memcpy(&buffer[offset], first.Data4, sizeof(INT64));

    data[index].ptr = reinterpret_cast<UINT64>(buffer);
    data[index].size = static_cast<UINT32>(GUID_FLAT_SIZE);
    data[index].reserved = 0;

    return WritePayload<index + 1, TArgs...>(data, rest...);
}

template<typename... Args>
template<typename T>
std::vector<BYTE> ProfilerEvent<Args...>::GetEventBuffer(const std::vector<T>& data)
{
    size_t offset = 0;
    //2 byte length prefix
    size_t bufferSize = sizeof(UINT16) + (data.size() * sizeof(T));
    std::vector<BYTE> buffer = std::vector<BYTE>(bufferSize);
    WriteToBuffer<UINT16>(buffer.data(), &offset, (UINT16)data.size());

    for (const T& element : data)
    {
        WriteToBuffer<T>(buffer.data(), &offset, element);
    }

    return buffer;
}

template<typename... Args>
template<typename T>
void ProfilerEvent<Args...>::WriteToBuffer(BYTE* pBuffer, size_t* pOffset, const T& value)
{
    *(T*)(pBuffer + *pOffset) = value;
    *pOffset += sizeof(T);
}

template<typename... Args>
template<size_t index>
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data)
{
    return _profilerInfo->EventPipeWriteEvent(_event, sizeof...(Args), data, nullptr, nullptr);
}

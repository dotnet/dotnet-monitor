// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    template<size_t index, typename T = std::wstring, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const std::wstring& first, TArgs... rest);

    template<size_t index, typename T = std::vector<typename T::value_type>, typename... TArgs>
    HRESULT WritePayload(COR_PRF_EVENT_DATA* data, const std::vector<typename T::value_type>& first, TArgs... rest);

    template<typename T>
    static std::vector<BYTE> GetEventBuffer(const std::vector<T>& data);

    template<typename T>
    static void WriteToBuffer(BYTE* pBuffer, size_t bufferLength, size_t* pOffset, const T& value);

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
    EventTypeMapping::GetType<T>(_descriptor[index]);
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
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const std::wstring& first, TArgs... rest)
{
    if (first.size() == 0)
    {
        data[index].ptr = 0;
        data[index].size = 0;
        data[index].reserved = 0;
    }
    else
    {
        data[index].ptr = reinterpret_cast<UINT64>(first.c_str());
        data[index].size = static_cast<UINT32>((first.size() + 1) * sizeof(WCHAR));
        data[index].reserved = 0;
    }
    return WritePayload<index + 1, TArgs...>(data, rest...);
}

template<typename... Args>
template<size_t index, typename T, typename... TArgs>
HRESULT ProfilerEvent<Args...>::WritePayload(COR_PRF_EVENT_DATA* data, const std::vector<typename T::value_type>& first, TArgs... rest)
{
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
template<typename T>
static std::vector<BYTE> ProfilerEvent<Args...>::GetEventBuffer(const std::vector<T>& data)
{
    size_t offset = 0;
    size_t bufferSize = 2 + (data.size() * sizeof(T));
    std::vector<BYTE> buffer = std::vector<BYTE>(bufferSize);
    WriteToBuffer<UINT16>(buffer.data(), bufferSize, &offset, (UINT16)data.size());

    for (const T& element : data)
    {
        WriteToBuffer<T>(buffer.data(), bufferSize, &offset, element);
    }

    return buffer;
}

template<typename... Args>
template<typename T>
static void ProfilerEvent<Args...>::WriteToBuffer(BYTE* pBuffer, size_t bufferLength, size_t* pOffset, const T& value)
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

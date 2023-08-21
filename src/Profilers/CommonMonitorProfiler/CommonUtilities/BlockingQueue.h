// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <queue>
#include <mutex>
#include <condition_variable>

template<typename T>
class BlockingQueue final
{
public:
    HRESULT Enqueue(const T& item)
    {
        {
            std::lock_guard<std::mutex> lock(_mutex);
            if (_complete)
            {
                return E_UNEXPECTED;
            }
            _queue.push(item);
        }
        _condition.notify_all();

        return S_OK;
    }

    HRESULT BlockingDequeue(T& item)
    {
        std::unique_lock<std::mutex> lock(_mutex);
        _condition.wait(lock, [this]() { return !_queue.empty() || _complete; });

        //We can't really tell if the caller wants to drain the remaining entries or simply abandon the queue
        if (!_queue.empty())
        {
            item = _queue.front();
            _queue.pop();
            return _complete ? S_FALSE : S_OK;
        }

        return E_FAIL;
    }

    void Complete()
    {
        {
            std::lock_guard<std::mutex> lock(_mutex);
            _complete = true;
        }
        _condition.notify_all();
    }

private:
    std::queue<T> _queue;
    std::mutex _mutex;
    std::condition_variable _condition;
    bool _complete = false;
};
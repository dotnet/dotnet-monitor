// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <vector>
#include "cor.h"
#include "corprof.h"

class Stack
{
public:
    const UINT64 GetThreadId() const { return _tid; }
    void SetThreadId(UINT64 threadid) { _tid = threadid; }
    const std::vector<UINT64>& GetFunctionIds() const { return _functionIds; }
    const std::vector<UINT64>& GetOffsets() const { return _offsets; }

    void AddFrame(FunctionID functionID, UINT_PTR offset)
    {
        _functionIds.push_back(functionID);
        _offsets.push_back(offset);
    }
private:
    UINT64 _tid = 0;
    //We model these as two parallel arrays instead of objects to simplify conversion to the EventSource format of std::vector<BYTE>
    std::vector<UINT64> _functionIds;
    std::vector<UINT64> _offsets;
};




// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class PayloadType : short
{
    Unknown,
    Int32,
    Utf8Json
};

enum class MessageType : short
{
    Unknown,
    Status,
    Callstack,
    CaptureParameters
};

struct IpcMessage
{
    PayloadType PayloadType = PayloadType::Unknown;
    MessageType MessageType = MessageType::Unknown;
    std::vector<BYTE> Payload;
};

struct SimpleIpcMessage
{
    PayloadType PayloadType = PayloadType::Int32;
    MessageType MessageType = MessageType::Unknown;
    int Parameters;
};

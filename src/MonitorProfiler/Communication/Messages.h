// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class MessageType : short
{
    Unknown,
    SimpleCommand,
    JsonCommand
};

enum class ProfilerCommand : short
{
    Unknown,
    OK,
    Error,
    Callstack,
    CaptureParameters
};

struct IpcMessage
{
    MessageType MessageType = MessageType::Unknown;
    ProfilerCommand ProfilerCommand = ProfilerCommand::Unknown;
    std::vector<BYTE> Payload;
};

struct SimpleIpcMessage
{
    MessageType MessageType = MessageType::SimpleCommand;
    ProfilerCommand ProfilerCommand = ProfilerCommand::Unknown;
    int Parameters;
};

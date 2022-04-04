// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <codecvt>
#include <locale>
#include <ostream>
#include "tstring.h"

std::ostream& operator<<(std::ostream& os, const tstring& str)
{
#ifdef HOST_UNIX
    std::wstring_convert<std::codecvt_utf8_utf16<WCHAR>, WCHAR> conv;
#else // HOST_UNIX
    std::wstring_convert<std::codecvt_utf8<WCHAR>, WCHAR> conv;
#endif // HOST_UNIX
    os << conv.to_bytes(str);
    return os;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <codecvt>
#include <string>

#if TARGET_UNIX

typedef std::u16string tstring;
#define _T(str) u##str

#else // TARGET_UNIX

typedef std::wstring tstring;
#define _T(str) L##str

#endif // TARGET_UNIX

static std::string to_string(const tstring& str)
{
#ifdef TARGET_UNIX
    std::wstring_convert<std::codecvt_utf8_utf16<WCHAR>, WCHAR> conv;
#else // TARGET_UNIX
    std::wstring_convert<std::codecvt_utf8<WCHAR>, WCHAR> conv;
#endif // TARGET_UNIX
    return conv.to_bytes(str);
}

static tstring to_tstring(const std::string& str)
{
#ifdef TARGET_UNIX
    std::wstring_convert<std::codecvt_utf8_utf16<WCHAR>, WCHAR> conv;
#else // TARGET_UNIX
    std::wstring_convert<std::codecvt_utf8<WCHAR>, WCHAR> conv;
#endif // TARGET_UNIX
    return conv.from_bytes(str);
}

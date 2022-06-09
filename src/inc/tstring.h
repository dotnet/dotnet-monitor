// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <codecvt>
#include <locale>
#include <string>
#if TARGET_WINDOWS
#include <winnt.h>
#elif TARGET_UNIX
#include <pal_mstypes.h>
#endif

#if TARGET_UNIX

typedef std::u16string tstring;
#define _T(str) u##str

typedef std::codecvt_utf8_utf16<WCHAR> codecvt_utf8_utf16_wchar;

#else // TARGET_UNIX

typedef std::wstring tstring;
#define _T(str) L##str

typedef std::codecvt_utf8<WCHAR> codecvt_utf8_utf16_wchar;

#endif // TARGET_UNIX

static std::string to_string(const tstring& str)
{
    std::wstring_convert<codecvt_utf8_utf16_wchar, WCHAR> conv;
    return conv.to_bytes(str);
}

static tstring to_tstring(const std::string& str)
{
    std::wstring_convert<codecvt_utf8_utf16_wchar, WCHAR> conv;
    return conv.from_bytes(str);
}

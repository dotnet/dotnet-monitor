// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <codecvt>
#include <locale>
#include <ostream>
#include "tstring.h"

std::ostream& operator<<(std::ostream& os, const tstring& tstr)
{
    os << to_string(tstr);
    return os;
}

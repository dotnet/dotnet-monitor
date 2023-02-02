// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <ostream>
#include "tstring.h"

std::ostream& operator<<(std::ostream& os, const tstring& tstr)
{
    os << to_string(tstr);
    return os;
}

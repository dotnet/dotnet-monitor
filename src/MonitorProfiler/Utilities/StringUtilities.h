// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string.h>

class StringUtilities
{
    public:
        template<size_t DestinationSize>
        static HRESULT Copy(char (&destination)[DestinationSize], const char* source)
        {
            //Note both strncpy_s and strlcpy stop early if the source has a \0 terminator

#if TARGET_WINDOWS
            int result = strncpy_s(destination, DestinationSize, source, DestinationSize - 1);
            if (result != 0)
            {
                return HRESULT_FROM_WIN32(result);
            }
#elif defined(TARGET_LINUX) && !defined(TARGET_ALPINE_LINUX)
            //TODO Glibc does not support the recommened string copy functions
            strncpy(destination, source, DestinationSize);
#else
            if (strlcpy(destination, source, DestinationSize) >= DestinationSize)
            {
                return E_INVALIDARG;
            }
#endif

            return S_OK;
        }
};
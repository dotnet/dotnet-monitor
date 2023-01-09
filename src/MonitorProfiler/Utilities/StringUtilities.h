// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string.h>

#if !defined(_IMPLEMENT_STRNCPY_S)
#if defined(TARGET_LINUX) && !defined(TARGET_ALPINE_LINUX) /* glibc */
#define _IMPLEMENT_STRNCPY_S 1
#else /* glibc */
#define _IMPLEMENT_STRNCPY_S 0
#endif /* glibc */
#endif /* !defined(_IMPLEMENT_STRNCPY_S) */

#if _IMPLEMENT_STRNCPY_S && !defined(STRUNCATE)
#define STRUNCATE       80
#endif

class StringUtilities
{
    public:
        template<size_t DestinationSize>
        static HRESULT Copy(char (&destination)[DestinationSize], const char* source)
        {
            //Note both strncpy_s and strlcpy stop early if the source has a \0 terminator

#if TARGET_WINDOWS || _IMPLEMENT_STRNCPY_S
            int result = strncpy_s(destination, DestinationSize, source, DestinationSize - 1);
            if (result != 0)
            {
                return HRESULT_FROM_WIN32(result);
            }
#else
            if (strlcpy(destination, source, DestinationSize) >= DestinationSize)
            {
                return E_INVALIDARG;
            }
#endif

            return S_OK;
        }

#if _IMPLEMENT_STRNCPY_S
    private:
        // Derived from https://github.com/dotnet/runtime/blob/6dd808ff7ae62512330d2f111eb1f60f1ae40125/src/coreclr/pal/src/safecrt/strncpy_s.cpp
        // The only functional difference is that in debug builds this modified version will not fill any remaining space in the dst buffer with 0xFE.
        static errno_t strncpy_s(char *dst, size_t size, const char *src, size_t count)
        {
            char *p;
            size_t available;

            if (count == 0 && dst == NULL && size == 0)
            {
                /* this case is allowed; nothing to do */
                return 0;
            }

            /* validation section */
            if (dst == NULL || size == 0)
            {
                errno = EINVAL;
                return EINVAL;
            }

            if (count == 0)
            {
                /* notice that the source string pointer can be NULL in this case */
                *dst = 0;
                return 0;
            }

            if (src == NULL)
            {
                *dst = 0;
                errno = EINVAL;
                return EINVAL;
            }

            p = dst;
            available = size;
            if (count == _TRUNCATE)
            {
                while ((*p++ = *src++) != 0 && --available > 0)
                {
                }
            }
            else
            {
                while ((*p++ = *src++) != 0 && --available > 0 && --count > 0)
                {
                }
                if (count == 0)
                {
                    *p = 0;
                }
            }

            if (available == 0)
            {
                if (count == _TRUNCATE)
                {
                    dst[size - 1] = 0;
                    return STRUNCATE;
                }
                *dst = 0;
                errno = ERANGE;
                return ERANGE;
            }

            return 0;
        }
#endif /* _IMPLEMENT_STRNCPY_S */
};
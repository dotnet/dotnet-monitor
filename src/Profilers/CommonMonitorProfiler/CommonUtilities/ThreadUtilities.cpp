// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ThreadUtilities.h"
#if TARGET_UNIX
#include <time.h>
#include <errno.h>
#else
#include <Windows.h>
#endif

void ThreadUtilities::Sleep(unsigned int milliseconds)
{
#if TARGET_WINDOWS
    ::Sleep(milliseconds);
#else
    // Mostly copied from Pal

    timespec req;
    timespec rem;
    int result;

    req.tv_sec = milliseconds / 1000;
    req.tv_nsec = (milliseconds % 1000) * 1000000;

    do
    {
        // Sleep for the requested time.
        result = nanosleep(&req, &rem);

        // Save the remaining time (used if the loop runs another iteration).
        req = rem;
    } while (result == -1 && errno == EINTR);

#endif
}

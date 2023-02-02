// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "corhlpr.h"
#include "tstring.h"

/// <summary>
/// Interface allowing getting and setting of environment variables.
/// </summary>
DECLARE_INTERFACE(IEnvironment)
{
    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    STDMETHOD(GetEnvironmentVariable)(const tstring name, tstring& value) PURE;

    /// <summary>
    /// Sets the value of the specified environment variable.
    /// </summary>
    STDMETHOD(SetEnvironmentVariable)(const tstring name, const tstring value) PURE;
};

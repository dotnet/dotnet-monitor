// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    /// <summary>
    /// Attribute used to denote that a property belongs to an experimental feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class ExperimentalAttribute : Attribute
    {
    }
}

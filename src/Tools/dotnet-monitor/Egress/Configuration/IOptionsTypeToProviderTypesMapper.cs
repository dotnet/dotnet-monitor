// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Interface for describing what provider types are use a given options type.
    /// </summary>
    internal interface IOptionsTypeToProviderTypesMapper
    {
        /// <summary>
        /// Gets the ProviderTypes for a given <paramref name="optionsType"/> reference.
        /// </summary>
        /// <returns><see cref="IEnumerable{string}"/> that has the set of ProviderTypes that can be retreived.</returns>
        IEnumerable<IConfigurationSection> GetOptions(IConfigurationSection egressSection, Type optionsType);
    }
}

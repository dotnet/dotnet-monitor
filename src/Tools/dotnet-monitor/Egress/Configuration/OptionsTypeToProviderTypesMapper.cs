// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal class OptionsTypeToProviderTypesMapper : IOptionsTypeToProviderTypesMapper
    {
        private readonly IConfigurationSection _egressSection;

        public OptionsTypeToProviderTypesMapper(IConfiguration configuration)
        {
            _egressSection = configuration.GetEgressSection();
        }

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetProviderSections(Type optionsType)
        {
            if (optionsType == typeof(ExtensionEgressProviderOptions))
            {
                // Not an efficient way to do this, but just experimenting
                foreach (IConfigurationSection providerTypeSection in _egressSection.GetChildren())
                {
                    if (providerTypeSection.Exists() && providerTypeSection.Key.Equals("Extensions")) // should only return the Extensions one
                    {
                        yield return providerTypeSection;
                    }
                }
                yield break;
                /*
                foreach (IConfigurationSection providerTypeSection in _egressSection.GetChildren())
                {
                    if (providerTypeSection.Exists() && !IsBuiltInSection(providerTypeSection))
                    {
                        yield return providerTypeSection;
                    }
                }
                yield break;*/
            }
            else if (optionsType == typeof(AzureBlobEgressProviderOptions))
            {
                IConfigurationSection azureBlobSection = _egressSection.GetSection(EgressProviderTypes.AzureBlobStorage);
                if (azureBlobSection.Exists())
                {
                    yield return azureBlobSection;
                }
                yield break;
            }
            else if (optionsType == typeof(FileSystemEgressProviderOptions))
            {
                IConfigurationSection fileSystemSection = _egressSection.GetSection(EgressProviderTypes.FileSystem);
                if (fileSystemSection.Exists())
                {
                    yield return fileSystemSection;
                }
                yield break;
            }

            throw new ArgumentException(
                String.Format(Strings.ErrorMessage_FieldNotAllowed,
                    nameof(optionsType),
                    optionsType.FullName), 
                nameof(optionsType));
        }

        /// <summary>
        /// Determines if the given section is a built in type property/field of <see cref="EgressOptions"/>.
        /// </summary>
        /// <param name="s">The <see cref="IConfigurationSection"/> to evaluate, this should be one of the children of the "Egress" section.</param>
        /// <returns><see langword="true"/> if and only if the given section is one of of the pre-defined</returns>
        private bool IsBuiltInSection(IConfigurationSection s)
        {
            Type egressType = typeof(EgressOptions);
            return egressType.GetProperty(s.Key) != null;
        }
    }
}

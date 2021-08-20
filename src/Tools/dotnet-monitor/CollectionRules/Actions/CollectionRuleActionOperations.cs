// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectionRuleActionOperations :
        ICollectionRuleActionOperations
    {
        private readonly IDictionary<string, ICollectionRuleActionDescriptor> _map =
            new Dictionary<string, ICollectionRuleActionDescriptor>(StringComparer.Ordinal);

        private readonly IServiceProvider _serviceProvider;

        public CollectionRuleActionOperations(
            IServiceProvider serviceProvider,
            ILogger<CollectionRuleActionOperations> logger,
            IEnumerable<ICollectionRuleActionDescriptor> descriptors)
        {
            _serviceProvider = serviceProvider;

            foreach (ICollectionRuleActionDescriptor descriptor in descriptors)
            {
                if (_map.ContainsKey(descriptor.ActionName))
                {
                    logger.DuplicateCollectionRuleActionIgnored(descriptor.ActionName);
                }
                else
                {
                    _map.Add(descriptor.ActionName, descriptor);
                }
            }
        }

        /// <inheritdoc/>
        public bool TryCreateAction(
            string actionName,
            out ICollectionRuleActionProxy action)
        {
            if (_map.TryGetValue(actionName, out ICollectionRuleActionDescriptor descriptor))
            {
                Type actionWrapperType = typeof(CollectionRuleActionProxy<,>).MakeGenericType(descriptor.ActionType, descriptor.OptionsType);

                action = (ICollectionRuleActionProxy)_serviceProvider.GetService(actionWrapperType);
                return true;
            }

            action = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryCreateOptions(
            string actionName,
            out object options)
        {
            if (_map.TryGetValue(actionName, out ICollectionRuleActionDescriptor descriptor))
            {
                options = Activator.CreateInstance(descriptor.OptionsType);
                return true;
            }

            options = null;
            return false;
        }

        /// <inheritdoc/>
        public void TryValidateOptions(
            string actionName,
            object options,
            ValidationContext validationContext,
            ICollection<ValidationResult> results)
        {
            if (_map.TryGetValue(actionName, out ICollectionRuleActionDescriptor descriptor))
            {
                ValidationHelper.TryValidateOptions(descriptor.OptionsType, options, validationContext, results);
            }
            else
            {
                results.Add(new ValidationResult(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_UnknownActionType, actionName)));
            }
        }
    }
}

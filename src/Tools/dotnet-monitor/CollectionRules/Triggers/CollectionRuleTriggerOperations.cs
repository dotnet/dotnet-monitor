// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    internal sealed class CollectionRuleTriggerOperations :
        ICollectionRuleTriggerOperations
    {
        private readonly IDictionary<string, ICollectionRuleTriggerDescriptor> _map =
            new Dictionary<string, ICollectionRuleTriggerDescriptor>(StringComparer.Ordinal);

        private readonly IServiceProvider _serviceProvider;

        public CollectionRuleTriggerOperations(
            IServiceProvider serviceProvider,
            ILogger<CollectionRuleTriggerOperations> logger,
            IEnumerable<ICollectionRuleTriggerDescriptor> descriptors)
        {
            _serviceProvider = serviceProvider;

            foreach (ICollectionRuleTriggerDescriptor descriptor in descriptors)
            {
                if (_map.ContainsKey(descriptor.TriggerName))
                {
                    logger.DuplicateCollectionRuleTriggerIgnored(descriptor.TriggerName);
                }
                else
                {
                    _map.Add(descriptor.TriggerName, descriptor);
                }
            }
        }

        /// <inheritdoc/>
        public bool TryCreateFactory(
            string triggerName,
            out ICollectionRuleTriggerFactoryProxy factory)
        {
            // Check that the trigger is registered
            if (_map.TryGetValue(triggerName, out ICollectionRuleTriggerDescriptor descriptor))
            {
                // Trigger options are optional; the descriptor will have a non-null options type
                // if the trigger was registered with options. Create the appropriate factory proxy
                // depending on if the trigger has options.
                Type factoryWrapperType;
                if (null == descriptor.OptionsType)
                {
                    factoryWrapperType = typeof(CollectionRuleTriggerFactoryProxy<>).MakeGenericType(descriptor.FactoryType);
                }
                else
                {
                    factoryWrapperType = typeof(CollectionRuleTriggerFactoryProxy<,>).MakeGenericType(descriptor.FactoryType, descriptor.OptionsType);
                }

                factory = (ICollectionRuleTriggerFactoryProxy)_serviceProvider.GetService(factoryWrapperType);
                return true;
            }

            factory = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryCreateOptions(
            string triggerName,
            out object options)
        {
            // Check that the trigger is registered and has options
            if (_map.TryGetValue(triggerName, out ICollectionRuleTriggerDescriptor descriptor) &&
                null != descriptor.OptionsType)
            {
                options = Activator.CreateInstance(descriptor.OptionsType);
                return true;
            }

            options = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryValidateOptions(
            string triggerName,
            object options,
            ValidationContext validationContext,
            ICollection<ValidationResult> results)
        {
            // Check that the trigger is registered
            if (_map.TryGetValue(triggerName, out ICollectionRuleTriggerDescriptor descriptor))
            {
                // If the trigger type does not have options, then skip validation of the options.
                if (null != descriptor.OptionsType)
                {
                    return ValidationHelper.TryValidateOptions(descriptor.OptionsType, options, validationContext, results);
                }
                return true;
            }
            else
            {
                results.Add(new ValidationResult(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_UnknownTriggerType, triggerName)));
                return false;
            }
        }
    }
}

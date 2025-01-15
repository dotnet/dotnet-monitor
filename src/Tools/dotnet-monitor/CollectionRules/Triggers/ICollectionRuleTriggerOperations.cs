// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Provides operations over collection rule triggers.
    /// </summary>
    internal interface ICollectionRuleTriggerOperations
    {
        /// <summary>
        /// Attempts to create a proxy for the trigger factory instance
        /// associated with the registered trigger name.
        /// </summary>
        bool TryCreateFactory(
            string triggerName,
            out ICollectionRuleTriggerFactoryProxy factory);

        /// <summary>
        /// Attempts to create an options instance of the options type
        /// associated with the registered trigger name.
        /// </summary>
        bool TryCreateOptions(
            string triggerName,
            out object options);

        /// <summary>
        /// Attempts to validate an options instance of the options type
        /// associated with the registered trigger name.
        /// </summary>
        bool TryValidateOptions(
            string triggerName,
            object? options,
            ValidationContext validationContext,
            ICollection<ValidationResult> results);
    }
}

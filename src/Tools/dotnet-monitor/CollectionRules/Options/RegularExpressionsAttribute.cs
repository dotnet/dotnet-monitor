// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class RegularExpressionsAttribute : RegularExpressionAttribute
    {
        public RegularExpressionsAttribute(string pattern) : base(pattern)
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is string[] values)
            {
                foreach (string valueToValidate in values)
                {
                    if (!base.IsValid(valueToValidate))
                    {
                        return false;
                    }
                }
                return true;
            }

            return base.IsValid(value);
        }
    }
}

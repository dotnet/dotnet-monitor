// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal readonly struct EnumBinding<TEnum> : IParsable<EnumBinding<TEnum>>
        where TEnum : struct, Enum
    {
        public EnumBinding(TEnum value)
        {
            Value = value;
        }

        public TEnum Value { get; }

        public static implicit operator TEnum(EnumBinding<TEnum> binding) => binding.Value;

        public static implicit operator EnumBinding<TEnum>(TEnum value) => new(value);

        public static EnumBinding<TEnum> Parse(string s, IFormatProvider? provider)
        {
            if (!TryParse(s, provider, out EnumBinding<TEnum> result))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Value '{0}' is not valid for enum type '{1}'.", s, typeof(TEnum).Name));
            }

            return result;
        }

        public static bool TryParse(string? s, IFormatProvider? provider, out EnumBinding<TEnum> result)
        {
            if (!string.IsNullOrEmpty(s) && Enum.TryParse<TEnum>(s, ignoreCase: true, out TEnum enumValue))
            {
                result = new EnumBinding<TEnum>(enumValue);
                return true;
            }

            result = default;
            return false;
        }

        public override string ToString() => Value.ToString();
    }
}

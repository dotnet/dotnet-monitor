// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    /// <summary>
    /// Utility class to create ExceptionsConfigurationSettings.
    /// </summary>
    internal static class ExceptionsSettingsFactory
    {
        public static ExceptionsConfigurationSettings ConvertExceptionsConfiguration(ExceptionsConfiguration configuration)
        {
            ExceptionsConfigurationSettings configurationSettings = new();

            foreach (var filter in configuration.Include)
            {
                configurationSettings.Include.Add(ConvertExceptionFilter(filter));
            }

            foreach (var filter in configuration.Exclude)
            {
                configurationSettings.Exclude.Add(ConvertExceptionFilter(filter));
            }

            return configurationSettings;
        }

        private static ExceptionFilterSettings ConvertExceptionFilter(ExceptionFilter filter)
        {
            return new ExceptionFilterSettings()
            {
                ClassName = filter.ClassName,
                ExceptionType = filter.ExceptionType,
                MethodName = filter.MethodName,
                ModuleName = filter.ModuleName
            };
        }
    }
}

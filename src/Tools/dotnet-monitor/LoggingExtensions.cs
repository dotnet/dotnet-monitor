// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _egressProviderAdded =
            LoggerMessage.Define<string>(
                eventId: new EventId(1, "EgressProviderAdded"),
                logLevel: LogLevel.Debug,
                formatString: "Provider '{providerName}': Added.");

        private static readonly Action<ILogger, string, Exception> _egressProviderInvalidOptions =
            LoggerMessage.Define<string>(
                eventId: new EventId(2, "EgressProviderInvalidOptions"),
                logLevel: LogLevel.Error,
                formatString: "Provider '{providerName}': Invalid options.");

        private static readonly Action<ILogger, string, string, Exception> _egressProviderInvalidType =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(3, "EgressProviderInvalidType"),
                logLevel: LogLevel.Error,
                formatString: "Provider '{providerName}': Type '{providerType}' is not supported.");

        private static readonly Action<ILogger, string, Exception> _egressProviderValidatingOptions =
            LoggerMessage.Define<string>(
                eventId: new EventId(4, "EgressProviderValidatingOptions"),
                logLevel: LogLevel.Debug,
                formatString: "Provider '{providerName}': Validating options.");

        private static readonly Action<ILogger, int, Exception> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: new EventId(5, "EgressCopyActionStreamToEgressStream"),
                logLevel: LogLevel.Debug,
                formatString: "Copying action stream to egress stream with buffer size {bufferSize}");

        private static readonly Action<ILogger, string, string, Exception> _egressProviderOptionsValidationWarning =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(6, "EgressProviderOptionsValidationWarning"),
                logLevel: LogLevel.Warning,
                formatString: "Provider '{providerName}': {validationWarning}");

        private static readonly Action<ILogger, string, string, string, Exception> _egressProviderOptionValue =
            LoggerMessage.Define<string, string, string>(
                eventId: new EventId(7, "EgressProviderOptionValue"),
                logLevel: LogLevel.Debug,
                formatString: "Provider {providerType}: Provider option {optionName} = {optionValue}");

        private static readonly Action<ILogger, string, string, string, Exception> _egressStreamOptionValue =
            LoggerMessage.Define<string, string, string>(
                eventId: new EventId(8, "EgressStreamOptionValue"),
                logLevel: LogLevel.Debug,
                formatString: "Provider {providerType}: Stream option {optionName} = {optionValue}");

        private static readonly Action<ILogger, string, string, Exception> _egressProviderFileName =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(9, "EgressProviderFileName"),
                logLevel: LogLevel.Debug,
                formatString: "Provider {providerType}: File name = {fileName}");

        private static readonly Action<ILogger, string, string, Exception> _egressProviderUnableToFindPropertyKey =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(10, "EgressProvideUnableToFindPropertyKey"),
                logLevel: LogLevel.Warning,
                formatString: "Provider {providerType}: Unable to find '{keyName}' key in egress properties");

        private static readonly Action<ILogger, string, Exception> _egressProviderInvokeStreamAction =
            LoggerMessage.Define<string>(
                eventId: new EventId(11, "EgressProviderInvokeStreamAction"),
                logLevel: LogLevel.Debug,
                formatString: "Provider {providerType}: Invoking stream action.");

        private static readonly Action<ILogger, string, string, Exception> _egressProviderSavedStream =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(12, "EgressProviderSavedStream"),
                logLevel: LogLevel.Debug,
                formatString: "Provider {providerType}: Saved stream to {path}");

        private static readonly Action<ILogger, Exception> _noAuthentication =
            LoggerMessage.Define(
                eventId: new EventId(13, "NoAuthentication"),
                logLevel: LogLevel.Warning,
                formatString: "WARNING: Authentication has been disabled. This can pose a security risk and is not intended for production environments.");

        private static readonly Action<ILogger, Exception> _insecureAuthenticationConfiguration =
            LoggerMessage.Define(
                eventId: new EventId(14, "InsecureAutheticationConfiguration"),
                logLevel: LogLevel.Warning,
                formatString: "WARNING: Authentication is enabled over insecure http transport. This can pose a security risk and is not intended for production environments.");

        private static readonly Action<ILogger, string, Exception> _unableToListenToAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(15, "UnableToListenToAddress"),
                logLevel: LogLevel.Error,
                formatString: "Unable to listen to {url}. Dotnet-monitor functionality will be limited.");

        private static readonly Action<ILogger, string, Exception> _boundDefaultAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(16, "BoundDefaultAddress"),
                logLevel: LogLevel.Debug,
                formatString: "Bound default address: {address}");

        private static readonly Action<ILogger, string, Exception> _boundMetricsAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(17, "BoundMetricsAddress"),
                logLevel: LogLevel.Debug,
                formatString: "Bound metrics address: {address}");

        private static readonly Action<ILogger, Exception> _metricUrlsUpdated =
            LoggerMessage.Define(
                eventId: new EventId(18, "MetricUrlsUpdated"),
                logLevel: LogLevel.Warning,
                formatString: $"Metric bindings changed. To host custom metrics over http set {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.AllowInsecureChannelForCustomMetrics))} to {true}");

        private static readonly Action<ILogger, string, string, Exception> _metricUrlUpdated =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(19, "MetricUrlUpdated"),
                logLevel: LogLevel.Warning,
                formatString: "Updated {originalUrl} to {newUrl} due to custom metrics.");

        private static readonly Action<ILogger, string, Exception> _optionsValidationFalure =
            LoggerMessage.Define<string>(
                eventId: new EventId(20, "OptionsValidationFailure"),
                logLevel: LogLevel.Critical,
                formatString: "{failure}");

        public static void EgressProviderAdded(this ILogger logger, string providerName)
        {
            _egressProviderAdded(logger, providerName, null);
        }

        public static void EgressProviderInvalidOptions(this ILogger logger, string providerName)
        {
            _egressProviderInvalidOptions(logger, providerName, null);
        }

        public static void EgressProviderInvalidType(this ILogger logger, string providerName, string providerType)
        {
            _egressProviderInvalidType(logger, providerName, providerType, null);
        }

        public static void EgressProviderValidatingOptions(this ILogger logger, string providerName)
        {
            _egressProviderValidatingOptions(logger, providerName, null);
        }

        public static void EgressCopyActionStreamToEgressStream(this ILogger logger, int bufferSize)
        {
            _egressCopyActionStreamToEgressStream(logger, bufferSize, null);
        }

        public static void EgressProviderOptionsValidationWarning(this ILogger logger, string providerName, string validationWarning)
        {
            _egressProviderOptionsValidationWarning(logger, providerName, validationWarning, null);
        }

        public static void EgressProviderOptionValue(this ILogger logger, string providerName, string optionName, Uri optionValue)
        {
            logger.EgressProviderOptionValue(providerName, optionName, optionValue?.ToString());
        }

        public static void EgressProviderOptionValue(this ILogger logger, string providerName, string optionName, string optionValue, bool redact = false)
        {
            if (redact)
            {
                optionValue = Redact(optionValue);
            }

            _egressProviderOptionValue(logger, providerName, optionName, optionValue, null);
        }

        public static void EgressStreamOptionValue(this ILogger logger, string providerName, string optionName, string optionValue, bool redact = false)
        {
            if (redact)
            {
                optionValue = Redact(optionValue);
            }

            _egressStreamOptionValue(logger, providerName, optionName, optionValue, null);
        }

        public static void EgressProviderFileName(this ILogger logger, string providerName, string fileName)
        {
            _egressProviderFileName(logger, providerName, fileName, null);
        }

        public static void EgressProviderUnableToFindPropertyKey(this ILogger logger, string providerName, string keyName)
        {
            _egressProviderUnableToFindPropertyKey(logger, providerName, keyName, null);
        }

        public static void EgressProviderInvokeStreamAction(this ILogger logger, string providerName)
        {
            _egressProviderInvokeStreamAction(logger, providerName, null);
        }

        public static void EgressProviderSavedStream(this ILogger logger, string providerName, string path)
        {
            _egressProviderSavedStream(logger, providerName, path, null);
        }

        public static void NoAuthentication(this ILogger logger)
        {
            _noAuthentication(logger, null);
        }

        public static void InsecureAuthenticationConfiguration(this ILogger logger)
        {
            _insecureAuthenticationConfiguration(logger, null);
        }

        public static void UnableToListenToAddress(this ILogger logger, string address, Exception ex)
        {
            _unableToListenToAddress(logger, address, ex);
        }

        public static void BoundDefaultAddress(this ILogger logger, string address)
        {
            _boundDefaultAddress(logger, address, null);
        }

        public static void BoundMetricsAddress(this ILogger logger, string address)
        {
            _boundMetricsAddress(logger, address, null);
        }

        public static void MetricUrlUpdated(this ILogger logger, string originalUrl, string newUrl)
        {
            _metricUrlUpdated(logger, originalUrl, newUrl, null);
        }

        public static void MetricUrlsUpdated(this ILogger logger)
        {
            _metricUrlsUpdated(logger, null);
        }

        public static void OptionsValidationFailure(this ILogger logger, OptionsValidationException exception)
        {
            foreach (string failure in exception.Failures)
                _optionsValidationFalure(logger, failure, null);
        }

        private static string Redact(string value)
        {
            return string.IsNullOrEmpty(value) ? value : "<REDACTED>";
        }
    }
}

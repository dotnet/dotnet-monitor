﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Diagnostics.Tools.Monitor {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Diagnostics.Tools.Monitor.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to API key authentication not configured..
        /// </summary>
        internal static string ErrorMessage_ApiKeyNotConfigured {
            get {
                return ResourceManager.GetString("ErrorMessage_ApiKeyNotConfigured", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In &apos;Listen&apos; mode, the diagnostic port endpoint name must be specified..
        /// </summary>
        internal static string ErrorMessage_DiagnosticPortMissingInListenMode {
            get {
                return ResourceManager.GetString("ErrorMessage_DiagnosticPortMissingInListenMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Azure blob egress failed: {0}.
        /// </summary>
        internal static string ErrorMessage_EgressAzureFailedDetailed {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressAzureFailedDetailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Azure blob egress failed..
        /// </summary>
        internal static string ErrorMessage_EgressAzureFailedGeneric {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressAzureFailedGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File system egress failed&quot; {0}.
        /// </summary>
        internal static string ErrorMessage_EgressFileFailedDetailed {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressFileFailedDetailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File system egress failed..
        /// </summary>
        internal static string ErrorMessage_EgressFileFailedGeneric {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressFileFailedGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SharedAccessSignature or AccountKey must be specified..
        /// </summary>
        internal static string ErrorMessage_EgressMissingSasOrKey {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressMissingSasOrKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Egress provider &apos;{0}&apos; does not exist..
        /// </summary>
        internal static string ErrorMessage_EgressProviderDoesNotExist {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressProviderDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Egress provider type &apos;{0}&apos; was not registered..
        /// </summary>
        internal static string ErrorMessage_EgressProviderTypeNotRegistered {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressProviderTypeNotRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to create unique intermediate file in &apos;{0}&apos; directory..
        /// </summary>
        internal static string ErrorMessage_EgressUnableToCreateIntermediateFile {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressUnableToCreateIntermediateFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value of field {0} must be less than the value of field {1}..
        /// </summary>
        internal static string ErrorMessage_FieldMustBeLessThanOtherField {
            get {
                return ResourceManager.GetString("ErrorMessage_FieldMustBeLessThanOtherField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} field value &apos;{1}&apos; is not allowed..
        /// </summary>
        internal static string ErrorMessage_FieldNotAllowed {
            get {
                return ResourceManager.GetString("ErrorMessage_FieldNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} field could not be decoded as hex string..
        /// </summary>
        internal static string ErrorMessage_FieldNotHex {
            get {
                return ResourceManager.GetString("ErrorMessage_FieldNotHex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} field value length must be an even number..
        /// </summary>
        internal static string ErrorMessage_FieldOddLengh {
            get {
                return ResourceManager.GetString("ErrorMessage_FieldOddLengh", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid API key..
        /// </summary>
        internal static string ErrorMessage_InvalidApiKey {
            get {
                return ResourceManager.GetString("ErrorMessage_InvalidApiKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid API key format..
        /// </summary>
        internal static string ErrorMessage_InvalidApiKeyFormat {
            get {
                return ResourceManager.GetString("ErrorMessage_InvalidApiKeyFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid authentication header..
        /// </summary>
        internal static string ErrorMessage_InvalidAuthHeader {
            get {
                return ResourceManager.GetString("ErrorMessage_InvalidAuthHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} parameter value &apos;{1}&apos; is not allowed..
        /// </summary>
        internal static string ErrorMessage_ParameterNotAllowed {
            get {
                return ResourceManager.GetString("ErrorMessage_ParameterNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} parameter value &apos;{1}&apos; is not allowed. Must be between {2} and {3} bytes long..
        /// </summary>
        internal static string ErrorMessage_ParameterNotAllowedByteRange {
            get {
                return ResourceManager.GetString("ErrorMessage_ParameterNotAllowedByteRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Both the {0} field and the {1} field cannot be specified..
        /// </summary>
        internal static string ErrorMessage_TwoFieldsCannotBeSpecified {
            get {
                return ResourceManager.GetString("ErrorMessage_TwoFieldsCannotBeSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} field or the {1} field is required..
        /// </summary>
        internal static string ErrorMessage_TwoFieldsMissing {
            get {
                return ResourceManager.GetString("ErrorMessage_TwoFieldsMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to bind any urls..
        /// </summary>
        internal static string ErrorMessage_UnableToBindUrls {
            get {
                return ResourceManager.GetString("ErrorMessage_UnableToBindUrls", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unhandled connection mode: {0}.
        /// </summary>
        internal static string ErrorMessage_UnhandledConnectionMode {
            get {
                return ResourceManager.GetString("ErrorMessage_UnhandledConnectionMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a known action type..
        /// </summary>
        internal static string ErrorMessage_UnknownActionType {
            get {
                return ResourceManager.GetString("ErrorMessage_UnknownActionType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a known trigger type..
        /// </summary>
        internal static string ErrorMessage_UnknownTriggerType {
            get {
                return ResourceManager.GetString("ErrorMessage_UnknownTriggerType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Monitor logs and metrics in a .NET application send the results to a chosen destination..
        /// </summary>
        internal static string HelpDescription_CommandCollect {
            get {
                return ResourceManager.GetString("HelpDescription_CommandCollect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configuration related commands for dotnet-monitor..
        /// </summary>
        internal static string HelpDescription_CommandConfig {
            get {
                return ResourceManager.GetString("HelpDescription_CommandConfig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generate api key and hash for authentication..
        /// </summary>
        internal static string HelpDescription_CommandGenerateKey {
            get {
                return ResourceManager.GetString("HelpDescription_CommandGenerateKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Shows configuration, as if dotnet-monitor collect was executed with these parameters..
        /// </summary>
        internal static string HelpDescription_CommandShow {
            get {
                return ResourceManager.GetString("HelpDescription_CommandShow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The string representing the hash algorithm used to compute ApiKeyHash store in configuration, typically SHA256..
        /// </summary>
        internal static string HelpDescription_HashAlgorithm {
            get {
                return ResourceManager.GetString("HelpDescription_HashAlgorithm", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The length of the MonitorApiKey in bytes..
        /// </summary>
        internal static string HelpDescription_KeyLength {
            get {
                return ResourceManager.GetString("HelpDescription_KeyLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The fully qualified path and filename of the diagnostic port to which runtime instances can connect..
        /// </summary>
        internal static string HelpDescription_OptionDiagnosticPort {
            get {
                return ResourceManager.GetString("HelpDescription_OptionDiagnosticPort", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configuration level. Unredacted configuration can show sensitive information..
        /// </summary>
        internal static string HelpDescription_OptionLevel {
            get {
                return ResourceManager.GetString("HelpDescription_OptionLevel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enable publishing of metrics.
        /// </summary>
        internal static string HelpDescription_OptionMetrics {
            get {
                return ResourceManager.GetString("HelpDescription_OptionMetrics", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bindings for metrics api..
        /// </summary>
        internal static string HelpDescription_OptionMetricsUrls {
            get {
                return ResourceManager.GetString("HelpDescription_OptionMetricsUrls", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Turn off authentication..
        /// </summary>
        internal static string HelpDescription_OptionNoAuth {
            get {
                return ResourceManager.GetString("HelpDescription_OptionNoAuth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Turn off HTTP response egress.
        /// </summary>
        internal static string HelpDescription_OptionNoHttpEgress {
            get {
                return ResourceManager.GetString("HelpDescription_OptionNoHttpEgress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generates a new MonitorApiKey for each launch of the process..
        /// </summary>
        internal static string HelpDescription_OptionTempApiKey {
            get {
                return ResourceManager.GetString("HelpDescription_OptionTempApiKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bindings for the REST api..
        /// </summary>
        internal static string HelpDescription_OptionUrls {
            get {
                return ResourceManager.GetString("HelpDescription_OptionUrls", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {apiAuthenticationConfigKey} settings have changed..
        /// </summary>
        internal static string LogFormatString_ApiKeyAuthenticationOptionsChanged {
            get {
                return ResourceManager.GetString("LogFormatString_ApiKeyAuthenticationOptionsChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {apiAuthenticationConfigKey} settings are invalid: {validationFailure}.
        /// </summary>
        internal static string LogFormatString_ApiKeyValidationFailure {
            get {
                return ResourceManager.GetString("LogFormatString_ApiKeyValidationFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bound default address: {address}.
        /// </summary>
        internal static string LogFormatString_BoundDefaultAddress {
            get {
                return ResourceManager.GetString("LogFormatString_BoundDefaultAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bound metrics address: {address}.
        /// </summary>
        internal static string LogFormatString_BoundMetricsAddress {
            get {
                return ResourceManager.GetString("LogFormatString_BoundMetricsAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Negotiate, Kerberos, and NTLM authentication are not enabled when running with elevated permissions..
        /// </summary>
        internal static string LogFormatString_DisabledNegotiateWhileElevated {
            get {
                return ResourceManager.GetString("LogFormatString_DisabledNegotiateWhileElevated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to New provider &apos;{providerName}&apos; under type &apos;{providerType}&apos; was already registered with type &apos;{existingProviderType}&apos; and will be ignored..
        /// </summary>
        internal static string LogFormatString_DuplicateEgressProviderIgnored {
            get {
                return ResourceManager.GetString("LogFormatString_DuplicateEgressProviderIgnored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copying action stream to egress stream with buffer size {bufferSize}.
        /// </summary>
        internal static string LogFormatString_EgressCopyActionStreamToEgressStream {
            get {
                return ResourceManager.GetString("LogFormatString_EgressCopyActionStreamToEgressStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider &apos;{providerName}&apos;: The options are invalid. The provider will not be available for use..
        /// </summary>
        internal static string LogFormatString_EgressProviderInvalidOptions {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderInvalidOptions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {providerType}: Invoking stream action..
        /// </summary>
        internal static string LogFormatString_EgressProviderInvokeStreamAction {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderInvokeStreamAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider &apos;{providerName}&apos;: {failureMessage}.
        /// </summary>
        internal static string LogFormatString_EgressProviderOptionsValidationError {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderOptionsValidationError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {providerType}: Saved stream to {path}.
        /// </summary>
        internal static string LogFormatString_EgressProviderSavedStream {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderSavedStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {providerType}: Unable to find &apos;{keyName}&apos; key in egress properties.
        /// </summary>
        internal static string LogFormatString_EgressProvideUnableToFindPropertyKey {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProvideUnableToFindPropertyKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WARNING: Authentication is enabled over insecure http transport. This can pose a security risk and is not intended for production environments..
        /// </summary>
        internal static string LogFormatString_InsecureAutheticationConfiguration {
            get {
                return ResourceManager.GetString("LogFormatString_InsecureAutheticationConfiguration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generated one-time-use ApiKey for dotnet-monitor; use the following header for authorization:{NewLine}{AuthHeaderName}: {AuthScheme} {MonitorApiKey}.
        /// </summary>
        internal static string LogFormatString_LogTempApiKey {
            get {
                return ResourceManager.GetString("LogFormatString_LogTempApiKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WARNING: Authentication has been disabled. This can pose a security risk and is not intended for production environments..
        /// </summary>
        internal static string LogFormatString_NoAuthentication {
            get {
                return ResourceManager.GetString("LogFormatString_NoAuthentication", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {failure}.
        /// </summary>
        internal static string LogFormatString_OptionsValidationFailure {
            get {
                return ResourceManager.GetString("LogFormatString_OptionsValidationFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The process was launched elevated and will have access to all processes on the system. Do not run elevated unless you need to monitor processes launched by another user (e.g., IIS worker processes).
        /// </summary>
        internal static string LogFormatString_RunningElevated {
            get {
                return ResourceManager.GetString("LogFormatString_RunningElevated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to listen to {url}. Dotnet-monitor functionality will be limited..
        /// </summary>
        internal static string LogFormatString_UnableToListenToAddress {
            get {
                return ResourceManager.GetString("LogFormatString_UnableToListenToAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to :NOT PRESENT:.
        /// </summary>
        internal static string Placeholder_NotPresent {
            get {
                return ResourceManager.GetString("Placeholder_NotPresent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to :REDACTED:.
        /// </summary>
        internal static string Placeholder_Redacted {
            get {
                return ResourceManager.GetString("Placeholder_Redacted", resourceCulture);
            }
        }
    }
}

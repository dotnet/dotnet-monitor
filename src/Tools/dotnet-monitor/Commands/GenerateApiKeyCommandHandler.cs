// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    /// <summary>
    /// Used to generate Api Key for authentication. The first output is
    /// part of the Authorization header, and is the Base64 encoded key.
    /// The second output is a hex encoded string of the hash of the secret.
    /// </summary>
    internal static class GenerateApiKeyCommandHandler
    {
        public static void Invoke(OutputFormat output, TimeSpan expiration, TextWriter outputWriter)
        {
            GeneratedJwtKey newJwt = GeneratedJwtKey.Create(expiration);

            RootOptions opts = new()
            {
                Authentication = new AuthenticationOptions()
                {
                    MonitorApiKey = new MonitorApiKeyOptions()
                    {
                        Subject = newJwt.Subject,
                        PublicKey = newJwt.PublicKey,
                    }
                }
            };

            StringBuilder outputBldr = new StringBuilder();

            if (output == OutputFormat.MachineJson)
            {
                // For MachineJson, we don't do any decorations and the entire output payload is a single json blob
                MachineOutputFormat result = new MachineOutputFormat()
                {
                    Authentication = opts.Authentication,
                    AuthorizationHeader = $"{AuthConstants.ApiKeySchema} {newJwt.Token}" // This is the actual format of the HTTP header and should not be localized
                };
                outputBldr.AppendLine(JsonSerializer.Serialize(result, result.GetType(), new JsonSerializerOptions() { WriteIndented = true }));
            }
            else
            {
                outputBldr.AppendLine(ExperienceSurvey.ExperienceSurveyMessage);
                outputBldr.AppendLine();
                outputBldr.AppendLine(Strings.Message_GenerateApiKey);
                outputBldr.AppendLine();
                outputBldr.AppendLine(string.Format(Strings.Message_GeneratedAuthorizationHeader, HeaderNames.Authorization, AuthConstants.ApiKeySchema, newJwt.Token));
                outputBldr.AppendLine();

                outputBldr.AppendFormat(CultureInfo.CurrentCulture, Strings.Message_SettingsDump, output);
                outputBldr.AppendLine();
                switch (output)
                {
                    case OutputFormat.Json:
                        {
                            // Create configuration from object model.
                            MemoryConfigurationSource source = new();
                            source.InitialData = (IDictionary<string, string?>)opts.ToConfigurationValues(); // Cast the values as nullable, since they are reference types we can safely do this.
                            ConfigurationBuilder builder = new();
                            builder.Add(source);
                            IConfigurationRoot configuration = builder.Build();

                            try
                            {
                                // Write configuration into stream
                                using MemoryStream stream = new();
                                using (ConfigurationJsonWriter writer = new(stream))
                                {
                                    writer.Write(configuration, full: true, skipNotPresent: true);
                                }

                                // Write stream content as test into builder.
                                stream.Position = 0;
                                using StreamReader reader = new(stream);
                                outputBldr.AppendLine(reader.ReadToEnd());
                            }
                            finally
                            {
                                DisposableHelper.Dispose(configuration);
                            }
                        }
                        break;
                    case OutputFormat.Text:
                        outputBldr.AppendLine(string.Format(Strings.Message_GeneratekeySubject, newJwt.Subject));
                        outputBldr.AppendLine(string.Format(Strings.Message_GeneratekeyPublicKey, newJwt.PublicKey));
                        break;
                    case OutputFormat.Cmd:
                    case OutputFormat.PowerShell:
                    case OutputFormat.Shell:
                        IDictionary<string, string> optList = opts.ToEnvironmentConfiguration();
                        foreach ((string name, string value) in optList)
                        {
                            outputBldr.AppendFormat(CultureInfo.InvariantCulture, GetFormatString(output), name, value);
                            outputBldr.AppendLine();
                        }
                        break;
                }

                outputBldr.AppendLine();
            }

            outputWriter.Write(outputBldr);
        }

        private static string GetFormatString(OutputFormat output)
        {
            switch (output)
            {
                case OutputFormat.Cmd:
                    return "set {0}={1}";
                case OutputFormat.PowerShell:
                    return "$env:{0}=\"{1}\"";
                case OutputFormat.Shell:
                    return "export {0}=\"{1}\"";
                default:
                    throw new InvalidOperationException(string.Format(Strings.ErrorMessage_UnknownFormat, output));
            }
        }

        /// <summary>
        /// Represents the output format for <see cref="OutputFormat.MachineJson" />.
        /// </summary>
        /// <remarks>
        /// This is the first copy of this class, the testing companion is
        /// Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners.MonitorGenerateKeyRunner.ExpectedMachineOutputFormat
        /// ExpectedMachineOutputFormat. Any breaking changes here will cause a test failure.
        /// If you find yourself here editing this, 
        /// be careful of any downstream dependencies that are depending on this remaining stable.
        /// </remarks>
        internal class MachineOutputFormat
        {
            public required AuthenticationOptions Authentication { get; set; }
            public required string AuthorizationHeader { get; set; }
        }
    }
}

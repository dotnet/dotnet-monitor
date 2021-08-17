// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Used to generate Api Key for authentication. The first output is
    /// part of the Authorization header, and is the Base64 encoded key.
    /// The second output is a hex encoded string of the hash of the secret.
    /// </summary>
    internal sealed class GenerateApiKeyCommandHandler
    {
        public Task<int> GenerateApiKey(CancellationToken token, OutputFormat output, IConsole console)
        {
            GeneratedJwtKey newJwt = GeneratedJwtKey.Create();

            StringBuilder outputBldr = new StringBuilder();

            outputBldr.AppendLine(Strings.Message_GenerateApiKey);
            outputBldr.AppendLine();
            outputBldr.AppendLine(string.Format(Strings.Message_GeneratedAuthorizationHeader, HeaderNames.Authorization, AuthConstants.ApiKeySchema, newJwt.Token));
            outputBldr.AppendLine();

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

            outputBldr.AppendFormat(CultureInfo.CurrentCulture, Strings.Message_SettingsDump, output);
            outputBldr.AppendLine();
            switch (output)
            {
                case OutputFormat.Json:
                    string optsJson = JsonSerializer.Serialize(opts, new JsonSerializerOptions() 
                    { 
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true 
                    });
                    outputBldr.AppendLine(optsJson);
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
            console.Out.Write(outputBldr.ToString());

            return Task.FromResult(0);
        }

        private string GetFormatString(OutputFormat output)
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
    }
}

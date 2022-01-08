// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class DacLocator
    {
        public static void LocateRuntimeComponents(IEndpointInfo endpointInfo, out string dac, out string dbi)
        {
            if (Environment.GetEnvironmentVariable("DOTNET_MONITOR_VERSION") != null)
            {
                //TODO This is not really a comprehensive way to detect that we are in a sidecar.
                // We cannot use CommandLine since we didn't start the process.
                throw new InvalidOperationException(Strings.ErrorMessage_UnsupportedInSidecar);
            }

            Process process = null;
            try
            {
                process = Process.GetProcessById(endpointInfo.ProcessId);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(e.Message, e);
            }

            try
            {
                string runtimeDir = null;
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName == "libcoreclr.so")
                    {
                        runtimeDir = Path.GetDirectoryName(module.FileName);
                        break;
                    }
                }

                if (runtimeDir == null)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_NoRuntimeFolder);
                }

                dac = Path.Combine(runtimeDir, "libmscordaccore.so");
                if (!File.Exists(dac))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_MissingRuntimeComponent, dac));
                }
                dbi = Path.Combine(runtimeDir, "libmscordbi.so");
                if (!File.Exists(dbi))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_MissingRuntimeComponent, dbi));
                }
            }
            catch (NotSupportedException e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }
    }
}

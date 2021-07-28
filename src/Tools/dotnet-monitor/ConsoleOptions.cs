// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
#if !UNITTEST
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
#endif
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal sealed class ConsoleOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleOptions_FormatterName))]
        public string FormatterName { get; set; } = "simple";

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleOptions_LogToStandardErrorThreshold))]
        public logLevel LogToStandardErrorThreshold { get; set; }
    }
}

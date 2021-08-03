using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    class JsonConsoleFormatterOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_LoggingOptions_LogLevel))]
        public IDictionary<string, string> Foo { get; set; }
    }
}

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal sealed class ExecuteActionValidateOptions : IValidateOptions<ExecuteOptions>
    {
        public ValidateOptionsResult Validate(string name, ExecuteOptions options)
        {
            if (!string.IsNullOrEmpty(options.Path))
            {
                if (File.Exists(options.Path))
                {
                    return ValidateOptionsResult.Success;
                }

                return ValidateOptionsResult.Fail("An invalid path was provided.");
            }

            return ValidateOptionsResult.Fail("No path was provided.") ;
        }
    }
}

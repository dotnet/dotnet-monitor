using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Options;
using System.IO;

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

                return ValidateOptionsResult.Fail(Strings.ErrorMessage_ExecuteActionInvalidPath);
            }

            return ValidateOptionsResult.Fail(Strings.ErrorMessage_ExecuteActionNoProvidedPath) ;
        }
    }
}

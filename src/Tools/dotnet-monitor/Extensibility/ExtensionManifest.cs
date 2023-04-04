// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionManifest : IValidatableObject
    {
        public const string DefaultFileName = "extension.json";

        /// <summary>
        /// This is the name that users specify in configuration to refer to the extension.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// If specified, the executable file (without extension) to be launched when executing the extension.
        /// </summary>
        /// <remarks>
        /// Either <see cref="ExecutableFileName"/> or <see cref="AssemblyFileName"/> must be specified.
        /// </remarks>
        public string ExecutableFileName { get; set; }

        /// <summary>
        /// If specified, executes the extension using the shared .NET host (e.g. dotnet.exe) with the specified entry point assembly (without extension). 
        /// </summary>
        /// <remarks>
        /// Either <see cref="ExecutableFileName"/> or <see cref="AssemblyFileName"/> must be specified.
        /// </remarks>
        public string AssemblyFileName { get; set; }

        public static ExtensionManifest FromPath(string path)
        {
            if (!File.Exists(path))
            {
                ExtensionException.ThrowManifestNotFound(path);
            }

            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                return JsonSerializer.Deserialize<ExtensionManifest>(stream);
            }
            catch (JsonException ex)
            {
                ExtensionException.ThrowInvalidManifest(ex);

                throw;
            }
        }

        public void Validate()
        {
            List<ValidationResult> results = new();
            if (!Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true) &&
                results.Count > 0)
            {
                ExtensionException.ThrowInvalidManifest(results.First().ErrorMessage);
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            bool hasAssemblyFileName = !string.IsNullOrEmpty(AssemblyFileName);
            bool hasExecutableFileName = !string.IsNullOrEmpty(ExecutableFileName);

            if (hasAssemblyFileName && hasExecutableFileName)
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                            nameof(AssemblyFileName),
                            nameof(ExecutableFileName))));
            }

            if (!hasAssemblyFileName && !hasExecutableFileName)
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_TwoFieldsMissing,
                            nameof(AssemblyFileName),
                            nameof(ExecutableFileName))));
            }

            return results;
        }
    }
}

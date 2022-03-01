// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    // Need to rename this since we'll have two things in here
    internal interface ResponseCounts
    {
        // Need to centralize these (shouldn't be in two places)
        private const string StatusCodeRegex = "[1-5][0-9]{2}";
        private const string StatusCodesRegex = StatusCodeRegex + "(-" + StatusCodeRegex + ")?";

        public int? ResponseCount { get; set; }


        // Might yank this for simplicity
        [MinLength(1)]
        [RegularExpressions(StatusCodesRegex)]
        public string[] StatusCodes { get; set; }
    }
}

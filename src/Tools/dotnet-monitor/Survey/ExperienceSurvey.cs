// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ExperienceSurvey
    {
        public const string ExperienceSurveyLink = "https://aka.ms/dotnet-monitor-survey";

        public static string ExperienceSurveyMessage = string.Format(
            CultureInfo.InvariantCulture,
            Strings.Message_ExperienceSurvey,
            ExperienceSurveyLink);
    }
}

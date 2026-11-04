// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public static class Utilities
    {
        public const string ArtifactType_Dump = "dump";
        public const string ArtifactType_GCDump = "gcdump";
        public const string ArtifactType_Logs = "logs";
        public const string ArtifactType_Trace = "trace";
        public const string ArtifactType_Metrics = "livemetrics";
        public const string ArtifactType_Stacks = "stacks";
        public const string ArtifactType_Exceptions = "exceptions";
        public const string ArtifactType_Parameters = "parameters";

        public static TimeSpan ConvertSecondsToTimeSpan(int durationSeconds)
        {
            return durationSeconds < 0 ?
                Timeout.InfiniteTimeSpan :
                TimeSpan.FromSeconds(durationSeconds);
        }

        public static string GetFileNameTimeStampUtcNow()
        {
            // spell-checker:disable-next
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        }

        public static KeyValueLogScope CreateArtifactScope(string artifactType, IEndpointInfo endpointInfo)
        {
            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(artifactType);
            scope.AddArtifactEndpointInfo(endpointInfo);
            return scope;
        }

        public static ProcessKey? GetProcessKey(int? pid, Guid? uid, string? name)
        {
            return (!pid.HasValue && !uid.HasValue && string.IsNullOrEmpty(name)) ? null : new ProcessKey(pid, uid, name);
        }

        /// <summary>
        /// Validates that a user-supplied artifact name is a plain file name that cannot
        /// escape the egress destination (e.g. via path separators, drive or stream specifiers).
        /// Validation is platform-agnostic so that behavior is consistent regardless of host OS.
        /// </summary>
        public static void ValidateArtifactName(string? artifactName)
        {
            if (artifactName == null)
            {
                return;
            }

            if (!IsValidArtifactName(artifactName))
            {
                throw new ValidationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_InvalidArtifactName, artifactName));
            }
        }

        private static bool IsValidArtifactName(string artifactName)
        {
            if (string.IsNullOrWhiteSpace(artifactName) || artifactName.Length > MaxArtifactNameLength)
            {
                return false;
            }

            // Reject relative path segments and names that Windows silently trims.
            if (artifactName == "." || artifactName == ".." || artifactName.EndsWith('.') || artifactName.EndsWith(' '))
            {
                return false;
            }

            foreach (char c in artifactName)
            {
                if (char.IsControl(c) || Array.IndexOf(s_invalidArtifactNameChars, c) >= 0)
                {
                    return false;
                }
            }

            return Path.GetFileName(artifactName) == artifactName;
        }

        private const int MaxArtifactNameLength = 255;

        // Union of invalid file name characters across Windows and Unix, plus ':' to block drive and alternate data stream specifiers.
        private static readonly char[] s_invalidArtifactNameChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
            .Distinct()
            .ToArray();

        public static ISet<string> SplitTags(string? tags)
        {
            if (string.IsNullOrEmpty(tags))
            {
                return new HashSet<string>();
            }

            return tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        }
    }
}

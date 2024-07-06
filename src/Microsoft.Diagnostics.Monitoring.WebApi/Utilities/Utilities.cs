// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

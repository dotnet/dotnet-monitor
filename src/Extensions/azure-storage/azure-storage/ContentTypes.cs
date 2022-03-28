using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
        public const string ApplicationJsonSequence = "application/json-seq";
        public const string ApplicationNdJson = "application/x-ndjson";
        public const string ApplicationOctetStream = "application/octet-stream";
        public const string ApplicationProblemJson = "application/problem+json";
        public const string TextPlain = "text/plain";
        public const string TextPlain_v0_0_4 = TextPlain + "; version=0.0.4";
    }
}

using System;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    class HashAlgorithmChecker
    {
        private static readonly string[] DisallowedHashAlgorithms = new string[]
        {
            // ------------------   SHA1    ------------------
            "SHA",
            "SHA1",
            "System.Security.Cryptography.SHA1",
            "System.Security.Cryptography.SHA1Cng",
            "System.Security.Cryptography.HashAlgorithm",
            "http://www.w3.org/2000/09/xmldsig#sha1",
            // These give a KeyedHashAlgorith based on SHA1
            "System.Security.Cryptography.HMAC",
            "System.Security.Cryptography.KeyedHashAlgorithm",
            "HMACSHA1",
            "System.Security.Cryptography.HMACSHA1",
            "http://www.w3.org/2000/09/xmldsig#hmac-sha1",
            
            // ------------------    MD5    ------------------
            "MD5",
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.MD5Cng",
            "http://www.w3.org/2001/04/xmldsig-more#md5",
            // These give a KeyedHashAlgorith based on MD5
            "HMACMD5",
            "System.Security.Cryptography.HMACMD5",
            "http://www.w3.org/2001/04/xmldsig-more#hmac-md5",
        };

        public static bool IsAllowedAlgorithm(string hashAlgorithmName)
        {
            return !DisallowedHashAlgorithms.Contains(hashAlgorithmName, StringComparer.OrdinalIgnoreCase);
        }
    }
}

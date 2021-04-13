using System.IO;

namespace DiagnosticsReleaseTool.Util
{
    public static class DiagnosticsRepoHelpers
    {
        public const string ProductName = "dotnet-monitor";
        public const string RepositoryName = "https://github.com/dotnet/dotnet-monitor";
        internal static bool IsDockerUtilityFile(FileInfo arg) => arg.FullName.EndsWith(".nupkg.sha512") || arg.FullName.EndsWith(".nupkg.version");
    }
}
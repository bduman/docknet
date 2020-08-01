using System.IO;

namespace Docknet.Extensions
{
    public static class PathExtensions
    {
        public static string GetDirectoryNameFromFile(this string filePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(filePath));
        }
    }
}
namespace Docknet.Extensions
{
    public static class DigestExtensions
    {
        public static string GetHashPart(this string digest)
        {
            return digest.Replace("sha256:", "");
        }
    }
}
using System.Security.Cryptography;
using System.Text;

namespace Docknet.Extensions
{
    public static class HashAlgorithmExtensions
    {
        public static string ComputeHash(this HashAlgorithm hasher, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = hasher.ComputeHash(bytes);

            return hashBytes.ToHex();
        }
    }
}
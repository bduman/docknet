using System.Linq;

namespace Docknet.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            return string.Join("", bytes.Select(x => string.Format("{0:x2}", x)));
        }
    }
}
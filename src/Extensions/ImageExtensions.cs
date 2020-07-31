using System.IO;
using Docknet.Models;

namespace Docknet.Extensions
{
    public static class ImageExtensions
    {
        public static string CreateTempDirectory(this Image image)
        {
            var tempDirectory = string.Format("tmp_{0}_{1}", image.Name, image.Tag.Replace(":", "@"));
            return Directory.CreateDirectory(tempDirectory).FullName;
        }
    }
}
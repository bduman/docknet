using System.Collections.Generic;
using System.Globalization;

namespace Docknet.Models
{
    public class ManifestContent
    {
        public string Config { get; set; }
        public List<string> RepoTags { get; set; }
        public List<string> Layers { get; set; }

        public ManifestContent(Image image)
        {
            this.RepoTags = new List<string>();
            this.Layers = new List<string>();

            this.RepoTags.Add(image.RepositoryTag.Replace(":SHA256", string.Empty, true, CultureInfo.InvariantCulture));
        }
    }
}
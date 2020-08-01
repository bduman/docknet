using System.Collections.Generic;

namespace Docknet.Models
{
    public class Manifest
    {
        public int SchemaVersion { get; set; }
        public string MediaType { get; set; }
        public ConfigType Config { get; set; }
        public List<LayerType> Layers { get; set; }

        public class ConfigType
        {
            public string MediaType { get; set; }
            public int Size { get; set; }
            public string Digest { get; set; }
        }

        public class LayerType
        {
            public string MediaType { get; set; }
            public int Size { get; set; }
            public string Digest { get; set; }
            public List<string> Urls { get; set; }
        }
    }

}
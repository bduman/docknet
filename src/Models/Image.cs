namespace Docknet.Models
{
    public class Image
    {
        public string Registry { get; set; } = "registry-1.docker.io";
        public string Repository { get; set; } = "library";
        public string Name { get; set; }
        public string Tag { get; set; } = "latest";

        public string RegistryService { get; set; } = "registry.docker.io";
        public string AuthUrl { get; set; } = "https://auth.docker.io/token";

        public string RepositoryTag { get; set; }
    }
}
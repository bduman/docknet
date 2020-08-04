using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Docknet.Abstractions;
using Docknet.Enums;
using Docknet.Extensions;
using Docknet.Models;
using McMaster.Extensions.CommandLineUtils;
using static Docknet.Models.Manifest;

namespace Docknet.Services
{
    public class ImagePuller : IImagePuller
    {
        private const string EmptyJson = "{\"id\":\"{{id}}\", \"parent\":\"{{parent}}\", \"created\":\"1970-01-01T00:00:00Z\",\"container_config\":{\"Hostname\":\"\",\"Domainname\":\"\",\"User\":\"\",\"AttachStdin\":false, \"AttachStdout\":false,\"AttachStderr\":false,\"Tty\":false,\"OpenStdin\":false, \"StdinOnce\":false,\"Env\":null,\"Cmd\":null,\"Image\":\"\",\"Volumes\":null,\"WorkingDir\":\"\",\"Entrypoint\":null,\"OnBuild\":null,\"Labels\":null}}";
        private readonly IAuthTokenFactory _authTokenFactory;
        private readonly HttpClient _client;
        private readonly IConsole _console;
        private readonly SHA256Managed _hasher;
        private readonly ITarManager _tarManager;

        public ImagePuller(IConsole console, IAuthTokenFactory authTokenFactory,
            IHttpClientFactory clientFactory, ITarManager tarManager)
        {
            this._authTokenFactory = authTokenFactory;
            this._client = clientFactory.CreateClient();
            this._console = console;
            this._hasher = new SHA256Managed();
            this._tarManager = tarManager;
        }

        public async Task PullAsync(Image image)
        {
            var tempImageDir = image.CreateTempDirectory();

            var authToken = await this._authTokenFactory.GetAuthToken(image, AuthType.PULL);
            var manifest = await this.GetManifestAsync(image, authToken);

            var rootDigest = manifest.Config.Digest;
            var rootManifestJson = await this.GetRootManifestAsync(image, tempImageDir, authToken, rootDigest);

            var pullLayerTasks = manifest.Layers.Select(l => this.PullLayerAsync(l, image, authToken, tempImageDir));
            var layerFilePaths = await Task.WhenAll(pullLayerTasks);

            var tasks = new List<Task>();

            var saveManifestFileTask = this.SaveManifestFileAsync(image, tempImageDir, rootDigest, layerFilePaths);
            tasks.Add(saveManifestFileTask);

            for (int i = 0; i < layerFilePaths.Length; i++)
            {
                var layerDirectory = Path.GetDirectoryName(layerFilePaths[i]);
                var layerId = Path.GetFileName(layerDirectory);
                var parentLayerId = (i == 0) ? "" : layerFilePaths[i - 1].GetDirectoryNameFromFile();

                string json = EmptyJson;

                if (i == layerFilePaths.Length - 1)
                {
                    json = rootManifestJson;
                    json = json.Replace("\"history\":", "\"history_REMOVED\":")
                            .Replace("\"rootfs\":", "\"rootfs_REMOVED\":", true, CultureInfo.InvariantCulture);

                    json = "{\"id\": \"{{id}}\", \"parent\": \"{{parent}}\"," + json.Remove(0, 1);
                }

                json = json.Replace("{{id}}", layerId).Replace("{{parent}}", parentLayerId);

                tasks.Add(File.WriteAllTextAsync(string.Format("{0}/json", layerDirectory), json));
                tasks.Add(File.WriteAllTextAsync(string.Format("{0}/VERSION", layerDirectory), "1.0"));
            }

            var saveRepositoriesTask = this.SaveRepositoriesFileAsync(image, tempImageDir, layerFilePaths.Last().GetDirectoryNameFromFile());
            tasks.Add(saveRepositoriesTask);

            await Task.WhenAll(tasks);

            var tarFileName = image.Repository.Replace("/", "_") + "_" + image.Name + ".tar";
            this._tarManager.CreateTar(tarFileName, tempImageDir + "\\");

            this._console.WriteLine("Docker image pulled: " + tarFileName);
            Directory.Delete(tempImageDir, true);
        }

        private async Task<string> PullLayerAsync(LayerType layer, Image image, string authToken, string imageDir)
        {
            var digest = layer.Digest;

            var tempLayerId = this._hasher.ComputeHash(digest);
            var tempLayerDir = string.Format("{0}/{1}", imageDir, tempLayerId);
            Directory.CreateDirectory(tempLayerDir);

            var response = await this.GetStreamAsync(image, authToken, digest);

            if (!response.IsSuccessStatusCode) // When the layer is located at a custom URL
            {
                var request = this.PrepareRegistryRequest(layer.Urls[0], authToken, "application/octet-stream");
                response = await this._client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = string.Format("ERROR: Cannot download layer {0}", digest.GetHashPart());
                    this._console.WriteLine(errorMessage);
                    throw new Exception(errorMessage);
                }
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var tempLayerFilePath = string.Format("{0}/layer.tar", tempLayerDir);

            string realLayerId;

            using (var layerFile = File.Create(tempLayerFilePath))
            using (var decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                layerFile.Seek(0, SeekOrigin.Begin);
                await decompressedStream.CopyToAsync(layerFile);
            }

            using (var fstream = File.OpenRead(tempLayerFilePath))
            {
                realLayerId = this._hasher.ComputeHash(fstream).ToHex();
            }

            var realLayerDir = tempLayerDir.Replace(tempLayerId, realLayerId);
            Directory.Move(tempLayerDir, realLayerDir);

            var realLayerFilePath = string.Format("{0}/layer.tar", realLayerDir);

            this._console.WriteLine("{0}: Pull complete [{1}]", digest.GetHashPart(), response.Content.Headers.ContentLength);

            return realLayerFilePath;
        }

        private async Task<HttpResponseMessage> GetStreamAsync(Image image, string authToken, string digest)
        {
            var url = string.Format("https://{0}/v2/{1}/{2}/blobs/{3}", image.Registry, image.Repository, image.Name, digest);
            var request = this.PrepareRegistryRequest(url, authToken, "application/octet-stream");
            return await this._client.SendAsync(request);
        }

        private HttpRequestMessage PrepareRegistryRequest(string url, string authToken, string acceptValue)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Headers.Add("Accept", acceptValue);

            return request;
        }

        private async Task<Manifest> GetManifestAsync(Image image, string authToken)
        {
            var url = string.Format("https://{0}/v2/{1}/{2}/manifests/{3}", image.Registry, image.Repository, image.Name, image.Tag);

            var request = this.PrepareRegistryRequest(url, authToken, "application/vnd.docker.distribution.manifest.v2+json");
            var response = await this._client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                this._console.WriteLine(string.Format("[-] Cannot fetch manifest for {0} [HTTP {1}]", image.Repository, response.StatusCode));

                request = this.PrepareRegistryRequest(url, authToken, "application/vnd.docker.distribution.manifest.list.v2+json");
                response = await this._client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed");
                }

                this._console.WriteLine("[+] Manifests found for this tag (use the @digest format to pull the corresponding image):");

                var foundedManifests = await response.Content.ReadAsStringAsync();
                this._console.WriteLine(foundedManifests); // print founded digest list

                throw new Exception("Manifests not found for this tag");
            }

            var jsonStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Manifest>(jsonStream);
        }

        private async Task<string> GetRootManifestAsync(Image image, string tempImageDir, string authToken, string rootDigest)
        {
            var rootManifestResponse = await this.GetStreamAsync(image, authToken, rootDigest);

            var rootManifestJson = await rootManifestResponse.Content.ReadAsStringAsync();
            var manifestFile = string.Format("{0}/{1}.json", tempImageDir, rootDigest.GetHashPart());

            await File.WriteAllTextAsync(manifestFile, rootManifestJson);

            return rootManifestJson;
        }

        private async Task SaveManifestFileAsync(Image image, string tempImageDir, string rootDigest, string[] layerFilePaths)
        {
            var manifestContent = new ManifestContent(image);
            manifestContent.Config = rootDigest.GetHashPart() + ".json";
            manifestContent.Layers.AddRange(layerFilePaths.Select(p => p.GetDirectoryNameFromFile() + "/layer.tar"));

            var manifestJson = string.Format("[{0}]", JsonSerializer.Serialize(manifestContent));
            await File.WriteAllTextAsync(string.Format("{0}/manifest.json", tempImageDir), manifestJson);
        }

        private async Task SaveRepositoriesFileAsync(Image image, string tempImageDir, string lastLayerId)
        {
            var repositoriesJson = "{\"{{image}}\": {\"{{tag}}\": \"{{lastLayerId}}\"}}"
                                    .Replace("{{image}}", image.RepositoryTag.Replace(":" + image.Tag, string.Empty))
                                    .Replace("{{tag}}", image.Tag)
                                    .Replace("{{lastLayerId}}", lastLayerId);

            await File.WriteAllTextAsync(string.Format("{0}/repositories", tempImageDir), repositoriesJson);
        }
    }
}
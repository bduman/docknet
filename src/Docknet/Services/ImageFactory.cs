using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Docknet.Abstractions;
using Docknet.Models;

namespace Docknet.Services
{
    public class ImageFactory : IImageFactory
    {
        private readonly HttpClient _client;

        public ImageFactory(IHttpClientFactory clientFactory)
        {
            this._client = clientFactory.CreateClient();
        }

        public async Task<Image> CreateFromExpressionAsync(string expression)
        {
            var image = new Image();
            var expressionParts = expression.Split("/");

            if (expression.Contains("@"))
            {
                var subParts = expressionParts.Last().Split("@");
                image.Name = subParts[0];
                image.Tag = subParts[1];
            }
            else if (expression.Contains(":"))
            {
                var subParts = expressionParts.Last().Split(":");
                image.Name = subParts[0];
                image.Tag = subParts[1];
            }
            else
            {
                image.Name = expressionParts.Last();
            }

            if (expressionParts.Length > 1)
            {
                if (expressionParts[0].Contains(".") || expressionParts[0].Contains(":"))
                {
                    image.Registry = expressionParts[0];
                    image.Repository = string.Join("/", expressionParts.Skip(1).SkipLast(1));
                }
                else
                {
                    image.Repository = string.Join("/", expressionParts.SkipLast(1));
                }
            }

            image.RepositoryTag = image.Repository != "library"
                   ? string.Format("{0}/{1}:{2}", string.Join("/", expressionParts.SkipLast(1)), image.Name, image.Tag)
                   : string.Format("{0}:{1}", image.Name, image.Tag);

            await this.ResolveAuthAsync(image);

            return image;
        }

        private async Task ResolveAuthAsync(Image image)
        {
            var response = await this._client.GetAsync(string.Format("https://{0}/v2/", image.Registry));

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var authenticateValues = response.Headers.GetValues("WWW-Authenticate").First().Split(",");

                if (authenticateValues.Length > 0)
                {
                    image.AuthUrl = authenticateValues[0].Split("\"")[1];
                }

                if (authenticateValues.Length > 1)
                {
                    image.RegistryService = authenticateValues[1].Split("\"")[1];
                }
                else
                {
                    image.RegistryService = "";
                }
            }
        }
    }
}
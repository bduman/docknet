using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Docknet.Abstractions;
using Docknet.Enums;
using Docknet.Models;

namespace Docknet.Services
{
    public class AuthTokenFactory : IAuthTokenFactory
    {
        private const string AUTH_URL_FORMAT = "{0}?service={1}&scope=repository:{2}/{3}:{4}";
        private readonly HttpClient _client;

        public AuthTokenFactory(IHttpClientFactory clientFactory)
        {
            this._client = clientFactory.CreateClient();
        }

        public async Task<string> GetAuthToken(Image image, AuthType type)
        {
            var url = string.Format(AUTH_URL_FORMAT, image.AuthUrl,
             image.RegistryService, image.Repository, image.Name, type.ToString().ToLower());

            var response = await this._client.GetStringAsync(url);
            var accessToken = JsonSerializer.Deserialize<Dictionary<string, object>>(response);

            return accessToken["token"].ToString();
        }
    }
}
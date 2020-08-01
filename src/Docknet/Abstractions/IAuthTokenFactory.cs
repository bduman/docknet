using System.Threading.Tasks;
using Docknet.Enums;
using Docknet.Models;

namespace Docknet.Abstractions
{
    public interface IAuthTokenFactory
    {
        Task<string> GetAuthToken(Image image, AuthType type);
    }
}
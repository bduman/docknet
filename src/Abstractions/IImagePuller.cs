using System.Threading.Tasks;
using Docknet.Models;

namespace Docknet.Abstractions
{
    public interface IImagePuller
    {
        Task PullAsync(Image image);
    }
}
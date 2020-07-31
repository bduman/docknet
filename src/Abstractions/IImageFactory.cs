using System.Threading.Tasks;
using Docknet.Models;

namespace Docknet.Abstractions
{
    public interface IImageFactory
    {
        Task<Image> CreateFromExpressionAsync(string expression);
    }
}
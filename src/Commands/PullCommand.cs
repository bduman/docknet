using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Docknet.Abstractions;
using McMaster.Extensions.CommandLineUtils;

namespace Docknet.Commands
{
    [Command("pull", Description = "Pull an image or a repository from a registry")]
    class PullCommand
    {
        [Required(ErrorMessage = "You must specify the image name")]
        [Argument(0, Description = "Image name")]
        private string Name { get; }

        private async Task OnExecute(CommandLineApplication app, IImagePuller imagePuller, IImageFactory imageFactory)
        {
            var image = await imageFactory.CreateFromExpressionAsync(this.Name);

            await imagePuller.PullAsync(image);
        }
    }
}
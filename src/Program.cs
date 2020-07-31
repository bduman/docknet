using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using Docknet.Abstractions;
using Docknet.Commands;
using Docknet.Services;
using System.Text.Json;

namespace Docknet
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            DefaultJsonSerializerOptions();

            var services = new ServiceCollection()
                        .AddHttpClient()
                        .AddTransient<IImagePuller, ImagePuller>()
                        .AddTransient<IAuthTokenFactory, AuthTokenFactory>()
                        .AddSingleton<ITarManager, TarManager>()
                        .AddSingleton<IImageFactory, ImageFactory>()
                        .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                        .BuildServiceProvider();

            var app = new CommandLineApplication<DocknetCommand>();

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            return app.ExecuteAsync(args);
        }

        static void DefaultJsonSerializerOptions()
        {
            ((JsonSerializerOptions)typeof(JsonSerializerOptions)
                .GetField("s_defaultOptions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null))
                .PropertyNameCaseInsensitive = true;
        }
    }
}

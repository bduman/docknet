using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace Docknet.Commands
{
    [Subcommand(
        typeof(PullCommand)
    )]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    class DocknetCommand
    {
        private void OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
        }

        private static string GetVersion()
            => typeof(DocknetCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
using System.IO;
using Docknet.Abstractions;
using ICSharpCode.SharpZipLib.Tar;
using McMaster.Extensions.CommandLineUtils;

namespace Docknet.Services
{
    public class TarManager : ITarManager
    {
        private IConsole _console;

        public TarManager(IConsole console)
        {
            this._console = console;
        }

        public void CreateTar(string outputTarFilename, string sourceDirectory)
        {
            this._console.WriteLine("Creating archive...");

            using (var fs = File.Create(outputTarFilename))
            using (var tarArchive = TarArchive.CreateOutputTarArchive(fs))
            {
                tarArchive.RootPath = Path.GetFileName(Path.GetDirectoryName(sourceDirectory));

                var tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
                tarArchive.WriteEntry(tarEntry, true);
            }

            this._console.WriteLine("Archive created: " + outputTarFilename);
        }
    }
}
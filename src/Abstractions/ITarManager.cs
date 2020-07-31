namespace Docknet.Abstractions
{
    public interface ITarManager
    {
        void CreateTar(string outputTarFilename, string sourceDirectory);
    }
}
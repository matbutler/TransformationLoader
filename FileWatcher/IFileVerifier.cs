namespace FileProcessing.Watcher
{
    public interface IFileVerifier
    {
        bool RequiresProcessing(string fileName, out int fileConfigId);
    }
}
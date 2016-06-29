namespace FileProcessing.Watcher
{
    public interface IFileProcessQueue
    {
        void Enqueue(string filepath, int fileConfigId);
    }
}
namespace FileWatcher
{
    public interface IFileProcessQueue
    {
        void Enqueue(string filepath, int fileConfigId);
    }
}
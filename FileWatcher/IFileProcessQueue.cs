using FileProcessing.Core.Enums;

namespace FileProcessing.Watcher
{
    public interface IFileProcessQueue
    {
        FileAction Enqueue(string filepath, int fileConfigId);
    }
}
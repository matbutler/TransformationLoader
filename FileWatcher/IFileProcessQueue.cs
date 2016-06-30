using FileProcessing.Core.Enums;

namespace FileProcessing.Watcher
{
    public interface IFileProcessQueue
    {
        ProcessStatus Enqueue(string filepath, int fileConfigId);
    }
}
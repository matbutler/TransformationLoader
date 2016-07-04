using FileProcessing.Core.Enums;

namespace FileProcessing.Watcher
{
    public interface IFileWatchAudit
    {
        void Log(string filepath, FileAction fileAction);
    }
}
using FileProcessing.Core;
using System.Configuration;
using System.IO;

namespace FileProcessing.Watcher
{
    public class FileWatcherService : IService
    {
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly IFileProcessQueue _fileProcessQueue;
        private readonly IFileVerifier _fileVerifier;
        private readonly IFileWatchAudit _fileWatchAudit;

        public FileWatcherService()
        {
            _fileSystemWatcher=new FileSystemWatcher();
            _fileSystemWatcher.Changed += new FileSystemEventHandler(OnChanged);
            _fileSystemWatcher.Created += new FileSystemEventHandler(OnChanged);
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName;
            _fileSystemWatcher.Filter = "*.*";
            _fileSystemWatcher.Path = ConfigurationManager.AppSettings["WatchPath"];

            _fileProcessQueue = new FileProcessQueue(ConfigurationManager.ConnectionStrings["FileWatcher"].ConnectionString);
            _fileVerifier = new FileVerifier(ConfigurationManager.ConnectionStrings["FileWatcher"].ConnectionString);
            _fileWatchAudit = new FileWatchAudit(ConfigurationManager.ConnectionStrings["FileWatcher"].ConnectionString);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            int fileConfigId = 0;
            if (_fileVerifier.RequiresProcessing(e.FullPath, out fileConfigId))
            {
                _fileWatchAudit.Log(e.FullPath);
                _fileProcessQueue.Enqueue(e.FullPath, fileConfigId);
            }
        }

        public string Name
        {
            get
            {
                return "File Watcher";
            }
        }

        public bool Start()
        {
            _fileSystemWatcher.EnableRaisingEvents = true;
            return true;
        }

        public bool Stop()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            return true;
        }
    }
}

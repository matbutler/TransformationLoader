using FileProcessing.Core;
using Logging;
using System;
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
        private readonly ILogger _logger;

        public FileWatcherService()
        {
            _fileSystemWatcher=new FileSystemWatcher();
            _fileSystemWatcher.Changed += new FileSystemEventHandler(OnChanged);
            _fileSystemWatcher.Created += new FileSystemEventHandler(OnChanged);
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName;
            _fileSystemWatcher.Filter = "*.*";
            _fileSystemWatcher.Path = ConfigurationManager.AppSettings["WatchPath"];

            var connectionstring = ConfigurationManager.ConnectionStrings["FileWatcher"].ConnectionString;

            _fileProcessQueue = new FileProcessQueue(connectionstring);
            _fileVerifier = new FileVerifier(connectionstring);
            _fileWatchAudit = new FileWatchAudit(connectionstring);
            _logger = new Log4NetLogger(typeof(FileWatcherService));
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            int fileConfigId = 0;
            try
            {
                if (_fileVerifier.RequiresProcessing(e.FullPath, out fileConfigId))
                {
                    var fileAction = _fileProcessQueue.Enqueue(e.FullPath, fileConfigId);
                    _fileWatchAudit.Log(e.FullPath, fileAction);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Error Adding File to Queue", ex);
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

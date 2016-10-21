using System;
using FileProcessing.Core;
using Transformation.Loader;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using FileProcessing.Loader.Models;
using Logging;
using System.Xml.Linq;

namespace FileProcessing.Loader
{
    public class FileLoaderService : IService
    {
        private readonly int _pollingTime;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _loadProcessLoopTask;

        public FileLoaderService()
        {
            _logger = new Log4NetLogger(typeof(FileLoaderService));
            _pollingTime = int.Parse(ConfigurationManager.AppSettings["PollingTimeSeconds"]) * 1000;
        }

        public string Name
        {
            get
            {
                return "File Loader";
            }
        }

        public bool Start()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                _loadProcessLoopTask = StartFileProcessLoop();
            }
            catch (Exception ex)
            {
                _logger.Error("File Load Service Error", ex);
                throw;
            }

            return true;
        }

        private Task StartFileProcessLoop()
        {
            var fileSelector = new FileSelector("");

            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ProcessFile fileToProcess = null;

                    while ((fileToProcess = fileSelector.GetFileToProcess()) != null)
                    {
                        var dbRowLogger = new DBRowLogger("");
                        var loadProcess = new LoadProcess();
                        loadProcess.Initialise(fileToProcess.Config, _cancellationTokenSource, _logger, dbRowLogger);

                        var processInfo = new XElement("processinfo");
                        //fileToProcess.FilePath
                        loadProcess.Process(processInfo);

                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    await Task.Delay(_pollingTime);
                }
            });
        }

        public bool Stop()
        {
            _cancellationTokenSource?.Cancel();

            _loadProcessLoopTask.Wait();

            return true;
        }
    }
}

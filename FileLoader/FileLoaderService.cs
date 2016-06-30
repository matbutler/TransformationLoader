using System;
using FileProcessing.Core;
using Transformation.Loader;
using System.Threading.Tasks;
using FileProcessing.Loader.Helper;
using System.Configuration;
using System.Threading;
using FileProcessing.Loader.Models;

namespace FileProcessing.Loader
{
    public class FileLoaderService : IService
    {
        private readonly TransformationCore.Interfaces.ILogger _logger;
        private readonly int _pollingTime;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _loadProcessLoopTask;

        public FileLoaderService()
        {
            _logger = new TransformationLogger(new Logging.Log4NetLogger(typeof(FileLoaderService)));
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
            _cancellationTokenSource = new CancellationTokenSource();

            _loadProcessLoopTask = StartFileProcessLoop();

            return true;
        }

        private Task StartFileProcessLoop()
        {
            var fileSelector = new FileSelector();

            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ProcessFile fileToProcess = null;

                    while ((fileToProcess = fileSelector.GetFileToProcess()) != null)
                    {
                        var loadProcess = new LoadProcess(fileToProcess.Config, _cancellationTokenSource);

                        loadProcess.Start(fileToProcess.FilePath, _logger);

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

using System;
using FileProcessing.Core;
using Transformation.Loader;
using System.Threading.Tasks;
using FileProcessing.Loader.Helper;
using System.Configuration;

namespace FileProcessing.Loader
{
    public class FileLoaderService : IService
    {
        private readonly TransformationCore.Interfaces.ILogger _logger;
        private readonly int _pollingTime;
        private LoadProcess _runner;

        public FileLoaderService()
        {
            _logger = new TransformationLogger(new Logging.Log4NetLogger(typeof(FileLoaderService)));
            _pollingTime = int.Parse(ConfigurationManager.AppSettings["PollingTime"]);
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
            var fileSelector = new FileSelector();

            Task.Run(async () =>
            {
                while (true)
                {
                    var fileToProcess = fileSelector.GetFileToProcess();

                    if (fileToProcess != null)
                    {
                        _runner = new LoadProcess(fileToProcess.Config);

                        _runner.Start(fileToProcess.FilePath, _logger);
                    }
                    await Task.Delay(_pollingTime);
                }
            });

            return true;
        }

        public bool Stop()
        {
            _runner.Cancel();

            return true;
        }
    }
}

using System;
using FileProcessing.Core;
using Transformation.Loader;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using FileProcessing.Loader.Models;
using Logging;
using System.Xml.Linq;
using FileProcessing.Loader.Models.Exceptions;

namespace FileProcessing.Loader
{
    public class FileLoaderService : IService
    {
        private readonly int _pollingTime;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _loadProcessLoopTask;
        private string _connectionString;

        public FileLoaderService()
        {
            _logger = new Log4NetLogger(typeof(FileLoaderService));
            _connectionString = ConfigurationManager.ConnectionStrings["FileLoader"]?.ConnectionString;

            int pollingSeconds = 0;
            if (string.IsNullOrWhiteSpace(_connectionString) || !int.TryParse(ConfigurationManager.AppSettings["PollingTimeSeconds"], out pollingSeconds))
            {
                throw new Exception("Missing Fileloader connection string");
            }

            _pollingTime = pollingSeconds * 1000;
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
            _logger.Info("File Loader Started");

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
            var fileSelector = new FileSelector(_connectionString);

            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {    
                    ProcessFile fileToProcess = null;
                    try
                    {
                        while ((fileToProcess = fileSelector.GetFileToProcess()) != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.Debug(string.Format("Begin loading {0}", fileToProcess.FilePath));

                            var loadProcess = new LoadProcess(fileToProcess.Config, _cancellationTokenSource, _logger, new DBRowLogger(_connectionString, fileToProcess.Id));

                            await loadProcess.Run(new XElement("processinfo", new XAttribute("id", fileToProcess.Id.ToString()), new XElement("filename", fileToProcess.FilePath)));

                            //TODO POST PROCESS UPDATE
                        }
                    }
                    catch(ConfigException ex)
                    {
                        _logger.Error("Invalid Configuration", ex);
                    }

                    try
                    {
                        await Task.Delay(_pollingTime, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            });
        }

        public bool Stop()
        {
            _cancellationTokenSource?.Cancel();

            _loadProcessLoopTask.Wait();

            _logger.Info("File Loader Stopped");

            return true;
        }
    }
}

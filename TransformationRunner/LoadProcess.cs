using Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Interfaces;

namespace Transformation.Loader
{
    public class LoadProcess
    {
        private Stopwatch _sw = new Stopwatch();

        private Timer _LogTimer;
        private ILogger _logger;
        private XElement _config;
        private CancellationTokenSource _cancellationTokenSource;
        private BlockingCollection<Dictionary<string, object>> _inputQueue;
        private bool _running = false;
        private int _pipeCount;
        private int _rowprocessedCount;
        private int _activePipeCount;
        private int _rowErrorCount;

        public LoadProcess(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Missing Config");
            }

            _config = config;
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;


            _pipeCount = Convert.ToInt32(_config.Element("pipe")?.Attribute("pipes")?.Value);

            int maxQueue = 50000;
            if (_config.Element("pipe")?.Attribute("queuesize") != null)
            {
                maxQueue = Convert.ToInt32(_config.Element("pipe")?.Attribute("queuesize").Value);
            }

            _inputQueue = new BlockingCollection<Dictionary<string, object>>(maxQueue);
        }


        private void FinishProcess(bool success, string errorMsg)
        {
            _sw.Stop();
            _LogTimer.Dispose();

            _logger.Info(string.Format("Process : Finished ({1:#,##0} rows in {0:#,##0.00} Secs / {2:#,##0} RPS)", _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));

            //Sleep to give log a chance to update before completion
            Thread.Sleep(1000);

            _running = false;
        }

        private void LogTimerTick(object state)
        {
            _logger.Info(string.Format("Process : {0} Active Pipes ({2:#,##0} rows  in {1:#,##0.00} Secs / {3:#,##0} RPS)", _activePipeCount, _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));
        }

        public async void Start(string fileName)
        {
            //Create Logging Timer
            _LogTimer = new Timer(LogTimerTick, null, 10000, 10000);


            if (_running)
            {
                _logger.Fatal("Process : The process is already running");
                return;
            }

            Task[] ETLtasks = new Task[_pipeCount + 1];

            _logger.Info(string.Format("Process : Started (Pipes = {0})", _pipeCount));

            try
            {
                var readerConfig = _config.Element("reader");

                var readerFactory = new ReaderFactory();
                var reader = readerFactory.GetReader(readerConfig);
                reader.Initialise(fileName, readerConfig, _logger);

                var processId = Guid.NewGuid();

                var globalDictionaryBuilder = new GlobalDictionaryBuilder();
                var globalData = globalDictionaryBuilder.Build(_config);

                _sw.Start();

                var rowlogger = new RowLogger(globalData["connection"].ToString(),processId);

                var token = _cancellationTokenSource.Token;

                StartReader(token, ETLtasks, reader, rowlogger);

                StartPipes(globalData ,token, ETLtasks, rowlogger);

                bool success = false;
                string errorMsg = string.Empty;

                try
                {
                    await Task.WhenAll(ETLtasks);

                    success = true;
                    rowlogger.Complete();

                    rowlogger = null;
                }
                catch (AggregateException ex)
                {
                    errorMsg = LogException(errorMsg, ex);
                }

                _logger.Debug(string.Format("Process : Loaded {0:0.0} Seconds", _sw.Elapsed.TotalSeconds));
                FinishProcess(success, errorMsg);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);

                FinishProcess(false, ex.Message);
            }
        }

        private string LogException(string errorMsg, AggregateException ex)
        {
            foreach (var v in ex.Flatten().InnerExceptions)
            {
                if (!(v is TaskCanceledException))
                {
                    errorMsg = "Process : " + v.Message;
                    if (v.InnerException != null)
                    {
                        errorMsg = errorMsg + ":" + v.InnerException.Message;
                    }

                    _logger.Fatal(errorMsg);
                }
            }

            return errorMsg;
        }

        private void StartPipes(ReadOnlyDictionary<string, object> globalData,CancellationToken token, Task[] ETLtasks, RowLogger rowlogger)
        {
            for (var i = 1; i <= _pipeCount; i++)
            {
                var pipeno = i;

                var pipeBuilder = new PipeBuilder(_config, globalData, _logger);

                ETLtasks[i] = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var pipe = pipeBuilder.Build(pipeno);
                        var tpipe = new PipeRunner(pipeno, pipe, 1, _logger);
                        try
                        {
                           tpipe.Load(_inputQueue, ref _rowErrorCount, ref _activePipeCount, ref _rowprocessedCount, token, rowlogger.LogRow);
                        }
                        finally
                        {
                            tpipe.Dispose();
                            tpipe = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _cancellationTokenSource.Cancel();

                        throw new Exception(string.Format("Pipe {0} : {1}", pipeno, ex.Message));
                    }
                }, TaskCreationOptions.LongRunning);

            }
        }

        private void StartReader(CancellationToken token, Task[] ETLtasks, IReader reader, RowLogger rowlogger)
        {
            ETLtasks[0] = Task.Factory.StartNew(() =>
            {
                try
                {
                    reader.Load(_inputQueue, ref _rowErrorCount, token, _logger, rowlogger.LogRow);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Loader : {0}", ex.Message));
                }
                finally
                {
                    _inputQueue.CompleteAdding();
                }
            }, TaskCreationOptions.LongRunning);
        }

    }
}
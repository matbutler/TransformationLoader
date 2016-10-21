﻿using Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Interfaces;

namespace Transformation.Loader
{
    public class LoadProcess : IProcessStep
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
        private List<IRowLogger> _rowloggers;

        public void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Missing Config");
            }

            _config = config;
            _logger = logger;
            _rowloggers = new List<IRowLogger>()
            {
                new RowLogger()
            };

            if (rowlogger != null)
            {
                _rowloggers.Add(rowlogger);
            }

            _cancellationTokenSource = cancellationTokenSource;

            _pipeCount = Convert.ToInt32(_config.Element("pipe")?.Attribute("pipes")?.Value);

            int maxQueue = 50000;
            if (_config.Element("pipe")?.Attribute("queuesize") != null)
            {
                maxQueue = Convert.ToInt32(_config.Element("pipe")?.Attribute("queuesize").Value);
            }

            _inputQueue = new BlockingCollection<Dictionary<string, object>>(maxQueue);
        }

        public async Task<bool> Process(XElement processInfo, bool previousStepSucceeded = true)
        {
            var filename = processInfo.Element("filename")?.Value;

            if (string.IsNullOrWhiteSpace(filename))
            {
                _logger.Error("Missing Filename");
                return false;
            }

            _LogTimer = new Timer(LogTimerTick, null, 10000, 10000);

            var result = await Start(filename);

            _sw.Stop();
            _LogTimer.Dispose();

            _logger.Info(string.Format("Finished ({1:#,##0} rows in {0:#,##0.00} Secs / {2:#,##0} RPS)", _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));

            return result;
        }

        private void LogTimerTick(object state)
        {
            _logger.Info(string.Format("{0} Active Pipes ({2:#,##0} rows  in {1:#,##0.00} Secs / {3:#,##0} RPS)", _activePipeCount, _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));
        }

        private async Task<bool> Start(string fileName)
        {
            Task[] ETLtasks = new Task[_pipeCount + 1];

            _logger.Info(string.Format("Started (Pipes = {0})", _pipeCount));

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

                var token = _cancellationTokenSource.Token;

                _rowloggers.ForEach(x => x.Initialise(processId));

                StartReader(token, ETLtasks, reader);

                StartPipes(globalData ,token, ETLtasks);

                try
                {
                    await Task.WhenAll(ETLtasks);

                    _rowloggers.ForEach(x => x.Complete());

                    return true;
                }
                catch (AggregateException ex)
                {
                    LogException(ex);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
            }

            return false;
        }

        private void LogException(AggregateException ex)
        {
            string errorMsg = string.Empty;
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
        }

        private void StartPipes(ReadOnlyDictionary<string, object> globalData,CancellationToken token, Task[] ETLtasks)
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
                           tpipe.Load(_inputQueue, ref _rowErrorCount, ref _activePipeCount, ref _rowprocessedCount, token, LogRow);
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

        private void StartReader(CancellationToken token, Task[] ETLtasks, IReader reader)
        {
            ETLtasks[0] = Task.Factory.StartNew(() =>
            {
                try
                {
                    reader.Load(_inputQueue, ref _rowErrorCount, token, _logger, LogRow);
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

        public void LogRow(bool rowSucess, bool rowDropped, long rowNumber, string rowError)
        {
            _rowloggers.ForEach(x => x.LogRow(rowSucess, rowDropped, rowNumber, rowError));
        }
    }
}
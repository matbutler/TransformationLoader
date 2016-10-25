using Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace Transformation.Loader
{
    [Export(typeof(IProcessStep))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "Transformation")]
    [ExportMetadata("Version", "1.0.0")]
    public class TransformationProcess : IProcessStep
    {
        private Stopwatch _sw = new Stopwatch();

        private Timer _LogTimer;
        private ILogger _logger;
        private XElement _config;
        private CancellationTokenSource _cancellationTokenSource;
        private BlockingCollection<Dictionary<string, object>> _inputQueue;
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

        public async Task<bool> Process(XElement processInfo, GlobalData globalData, bool previousStepSucceeded = true)
        {
            _LogTimer = new Timer(LogTimerTick, null, 10000, 10000);

            _sw.Start();

            var result = await Start(processInfo, globalData);

            _sw.Stop();
            _LogTimer.Dispose();

            _logger.Info(string.Format("Finished ({1:#,##0} rows in {0:#,##0.00} Secs / {2:#,##0} RPS)", _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));

            return result;
        }

        private void LogTimerTick(object state)
        {
            _logger.Info(string.Format("{0} Active Pipes ({2:#,##0} rows  in {1:#,##0.00} Secs / {3:#,##0} RPS)", _activePipeCount, _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowprocessedCount / _sw.Elapsed.TotalSeconds));
        }

        private async Task<bool> Start(XElement processInfo, GlobalData globalData)
        {
            var processId = processInfo.Attribute("id")?.Value;

            if (string.IsNullOrWhiteSpace(processId))
            {
                throw new Exception("Invalid/Missing Process Id");
            }

            Task[] ETLtasks = new Task[_pipeCount + 1];

            _logger.Info(string.Format("Started (Pipes = {0})", _pipeCount));

            try
            {
                var readerConfig = _config.Element("reader");

                var readerFactory = new ReaderFactory();
                var reader = readerFactory.GetReader(readerConfig);
                reader.Initialise(processInfo, readerConfig, 1, _logger);

                var token = _cancellationTokenSource.Token;

                _rowloggers.ForEach(x => x.Initialise(processId));

                StartReader(token, ETLtasks, reader);

                StartPipes(globalData, ETLtasks);

                try
                {
                    await Task.WhenAll(ETLtasks);

                    return true;
                }
                catch (AggregateException ex)
                {
                    LogException(ex);
                }
                finally
                {
                    _rowloggers.ForEach(x => x.Complete());
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

        private void StartPipes(GlobalData globalData, Task[] ETLtasks)
        {
            for (var i = 1; i <= _pipeCount; i++)
            {
                var pipeno = i;

                var catalog = new AggregateCatalog();

                catalog.Catalogs.Add(new AssemblyCatalog(typeof(LoadProcess).Assembly));
                catalog.Catalogs.Add(new DirectoryCatalog("Engine"));

                var pipeBuilder = new PipeBuilder(_config, globalData, _logger, new CompositionContainer(catalog));

                ETLtasks[i] = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var pipe = pipeBuilder.Build(pipeno);
                        var tpipe = new PipeRunner(pipeno, pipe, 1, _logger, LogRow);
                        try
                        {
                            tpipe.Load(_inputQueue, ref _rowErrorCount, ref _activePipeCount, ref _rowprocessedCount, _cancellationTokenSource.Token);
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
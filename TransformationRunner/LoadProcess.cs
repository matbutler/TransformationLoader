using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Enums;
using TransformationCore.Helpers;
using TransformationCore.Interfaces;

namespace Transformation.Loader
{
    public class LoadProcess
    {
        #region "Public Events"
        public event StartedEventHandler Started;
        public delegate void StartedEventHandler();
        public event FinishedEventHandler Finished;
        public delegate void FinishedEventHandler(bool success);
        #endregion

        #region "Private Variables"
        private Stopwatch _sw = new Stopwatch();

        private System.Threading.Timer _LogTimer;
        private ILogger _logger;
        private CancellationTokenSource _tokenSource;
        private BlockingCollection<Dictionary<string, object>> _inputQueue;
        #endregion

        #region "Public Properties"
        private bool _running = false;
        public bool Running
        {
            get { return _running; }
        }

        private int _pipeCount;
        public int PipeCount
        {
            get { return _pipeCount; }
        }

        private int _activePipeCount;
        public int ActivePipeCount
        {
            get { return _activePipeCount; }
        }

        private int _rowprocessedCount;
        public int RowprocessedCount
        {
            get { return _rowprocessedCount; }
        }

        private int _rowSkippedCount;
        public int RowSkippedCount
        {
            get { return _rowSkippedCount; }
        }

        private int _rowErrorCount;
        public int RowErrorCount
        {
            get { return _rowErrorCount; }
        }

        private Dictionary<string, object> _globalData;
        public Dictionary<string, object> GlobalData
        {
            get { return _globalData; }
        }

        private XElement _config;
        public XElement Config
        {
            get { return _config; }
        }

        #endregion

        public LoadProcess(XElement config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Missing Config");
            }

            _config = config;
        }

        #region "Private Functions"

        private void FinishProcess(bool success, string errorMsg)
        {
            _sw.Stop();
            _LogTimer.Dispose();

            _logger.Log(string.Format("Process : Finished ({1:#,##0} rows ({2:#,##0} skipped) in {0:#,##0.00} Secs / {3:#,##0} RPS)", _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowSkippedCount, (_rowprocessedCount + _rowSkippedCount) / _sw.Elapsed.TotalSeconds), MessageLevel.Action);

            //Sleep to give log a chance to update before completion
            Thread.Sleep(1000);

            _running = false;
            if (Finished != null)
            {
                Finished(success);
            }
        }

        private void LogTimerTick(object state)
        {
            _logger.Log(string.Format("Process : {0} Active Pipes ({2:#,##0} rows ({3:#,##0} skipped) in {1:#,##0.00} Secs / {4:#,##0} RPS)", _activePipeCount, _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowSkippedCount, (_rowprocessedCount + _rowSkippedCount) / _sw.Elapsed.TotalSeconds), MessageLevel.Info);
        }

        private void SetupGlobalVar()
        {
            foreach (var globalEl in _config.Elements("globalvar"))
            {
                if (globalEl.Attribute("name") != null && globalEl.Attribute("value") != null && globalEl.Attribute("valuetype") != null)
                {
                    try
                    {
                        var converter = TypeConverter.GetConverter(globalEl.Attribute("valuetype")?.Value, globalEl.Attribute("dateformat")?.Value);
                        var val = converter(globalEl.Attribute("value")?.Value);
                        _globalData.Add(globalEl.Attribute("name").Value, val);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error setting up global var {0} - {1}", globalEl.Attribute("name").ToString(), ex.Message));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Error setting up global var {0} missing (name, value, valuetype)", globalEl.ToString()));
                }
            }
        }
        #endregion

        #region "Public Functions"

        public async void Start(string fileName,ILogger logger)
        {
            //Create Logging Timer
            _LogTimer = new Timer(LogTimerTick, null, 10000, 10000);

            _logger = logger;

            if (_running)
            {
                _logger.Log("Process : The process is already running", MessageLevel.Critical);
                if (Finished != null)
                {
                    Finished(false);
                }
                return;
            }

            Initialise();

            CancellationToken token = _tokenSource.Token;
            Task[] ETLtasks = new Task[PipeCount + 1];

            _logger.Log(string.Format("Process : Started (Pipes = {0})", _pipeCount), MessageLevel.Action);

            try
            {
                SetupGlobalVar();

                var catalog = new DirectoryCatalog("Engine");
                
                var reader = GetReader(catalog);

                var processId = Guid.NewGuid();

                reader.Initialise(fileName, Config.Element("reader"), _logger);

                _sw.Start();

                var rowlogger = new RowLogger(GlobalData["connection"].ToString(),processId);

                StartReader(token, ETLtasks, reader, rowlogger);

                StartPipes(token, ETLtasks, rowlogger, catalog);

                Started?.Invoke();

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

                _logger.Log(string.Format("Process : Loaded {0:0.0} Seconds", _sw.Elapsed.TotalSeconds), MessageLevel.Info);
                FinishProcess(success, errorMsg);
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message, MessageLevel.Critical);

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

                    _logger.Log(errorMsg, MessageLevel.Critical);
                }
            }

            return errorMsg;
        }

        private void StartPipes(CancellationToken token, Task[] ETLtasks, RowLogger rowlogger, DirectoryCatalog catalog)
        {
            for (var i = 1; i <= PipeCount; i++)
            {
                var pipeno = i;

                var pipeBuilder = new PipeBuilder(_config, GlobalData, _logger, catalog);

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
                        _tokenSource.Cancel();

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

        private IReader GetReader(DirectoryCatalog catalog)
        {
            string readerName = string.Empty;
            string readerVersion = string.Empty;

            try
            {
                var readerConfig = Config.Element("reader");

                readerName = readerConfig?.Attribute("name")?.Value;

                readerVersion = readerConfig?.Attribute("version")?.Value ?? "";

                if (string.IsNullOrWhiteSpace(readerName))
                {
                    throw new Exception("Reader name missing from Config.");
                }

                var container = new CompositionContainer(catalog);

                ReaderFactory LdrFactory = new ReaderFactory();
                container.ComposeParts(LdrFactory);

                return LdrFactory.CreateReader(readerName, readerVersion);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to create Loader {0} : {1}", readerName, ex.Message));
            }
        }

        private void Initialise()
        {
            _running = true;
            _tokenSource = new CancellationTokenSource();
            _rowErrorCount = 0;
            _rowprocessedCount = 0;
            _rowSkippedCount = 0;
            _sw.Reset();
            _globalData = new Dictionary<string, object>();
            _inputQueue = null;

            var pipeElement = _config.Element("pipe");
            _pipeCount = Convert.ToInt32(pipeElement.Attribute("pipes").Value);

            int maxQueue = 50000;
            if (pipeElement.Attribute("queuesize") != null)
            {
                maxQueue = Convert.ToInt32(pipeElement.Attribute("queuesize").Value);
            }

            _inputQueue = new BlockingCollection<Dictionary<string, object>>(maxQueue);
        }

        public void Cancel()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
            }
        }
        #endregion
    }
}

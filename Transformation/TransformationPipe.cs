using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Data.SqlClient;
using TransformationCore.Exceptions;
using TransformationCore.Enums;
using TransformationCore.Interfaces;

namespace TransformationCore
{
    public class TransformationPipe : IDisposable
    {

        #region "Private Variables"
        private Dictionary<string, ITransformation> _transformationPipe = new Dictionary<string, ITransformation>();
        private int _errorsallowed = -1;
        private int _pipeNumber = -1;

        private ILogger _logger;
        private bool disposed = false;

        #endregion

        #region "Public Functions"
        /// <summary> 
        /// Defines the Transformations to run and sets the config properties
        /// </summary> 
        /// <param name="config">The config XML</param> 
        /// <param name="globalDS">The global dataset used to store common lookup tables</param> 
        /// <param name="logger">The UI Logging Object</param> 
        /// <param name="pipeNumber">The number of the pipe(thread) this transformation pipe is in</param> 
        public TransformationPipe(XElement config, Dictionary<string, object> globalData, ILogger logger, int pipeNumber, CompositionContainer container)
        {
            _logger = logger;
            _pipeNumber = pipeNumber;

            var pipeElement = config.Element("pipe");
            if (pipeElement == null)
            {
                throw new TransformationPipeException("Missing Pipe Config");
            }
            else if (pipeElement.Elements("transformation").Count() == 0)
            {
                throw new TransformationPipeException("Config has no Transformations");
            }

            var mgslvl = _pipeNumber > 1 ? MessageLevel.Debug : MessageLevel.Info;

            SetErrorsAllowed(pipeElement, mgslvl);

            CreateTransformations(globalData, pipeElement, container, mgslvl);
        }

        private void CreateTransformations(Dictionary<string, object> globalData, XElement pipeElement, CompositionContainer container, MessageLevel mgslvl)
        {

            int count = 0;
            foreach (var tranConfig in pipeElement.Elements("transformation"))
            {
                count++;

                if (tranConfig.Attribute("name") == null)
                {
                    throw new TransformationPipeException(string.Format("Incorrect Transformation Config : Missing Name : {0}", tranConfig));
                }

                var tranName = tranConfig.Attribute("name").Value;

                string tranVersion = string.Empty;

                if (tranConfig.Attribute("version") != null)
                {
                    tranVersion = tranConfig.Attribute("version").Value;
                }

                try
                {
                    TransformationFactory _transformationFactory = new TransformationFactory();
                    container.ComposeParts(_transformationFactory);

                    var tran = _transformationFactory.CreateTransformation(tranName, tranVersion);

                    tran.Initialise(tranConfig, globalData, _logger);

                    _transformationPipe.Add(string.Format("{0} - {1}", tranName, count), tran);
                    _logger.Log(string.Format("Pipe {0}: Transformation {1} = {2}", _pipeNumber, _transformationPipe.Count, tranName), mgslvl);
                }
                catch (Exception ex)
                {
                    _logger.Log(string.Format("Pipe {0}: Unable to Create Transformation {1} : {2} ({3})", _pipeNumber, _transformationPipe.Count, tranName, ex.Message), MessageLevel.Critical);
                    throw new TransformationPipeException(string.Format("Unable to Create Transformation {0} : {1}", tranName, ex.Message));
                }
            }
        }

        private void SetErrorsAllowed(XElement pipeElement, MessageLevel mgslvl)
        {
            if (pipeElement.Attribute("errorsallowed") != null)
            {
                _errorsallowed = Convert.ToInt32(pipeElement.Attribute("errorsallowed").Value);
                if (_errorsallowed == -1)
                {
                    _logger.Log(string.Format("Pipe {0}: Errors Allowed = Any", _pipeNumber), mgslvl);
                }
                else
                {
                    _logger.Log(string.Format("Pipe {0}: Errors Allowed = {1}", _pipeNumber, _errorsallowed), mgslvl);
                }
            }
        }

        /// <summary> 
        /// Defines the Transformations to run and sets the config properties
        /// </summary> 
        /// <param name="inputQueue">The thread safe Queue</param> 
        /// <param name="errorCount">The total number of errors accross the pipes(threads)</param> 
        /// <param name="pipeCount">The number of the Pipe</param> 
        /// <param name="rowprocessedCount">The variable to update number of rows processed</param> 
        /// <param name="ct">The cancellation object to cancel cross pipes(threads)</param> 
        /// <param name="rowLogAction">The row logging function (success,dropped,number,message)</param> 
        public void Load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, ref int pipeCount, ref int rowprocessedCount, CancellationToken ct, Action<bool, bool, int, string> rowLogAction)
        {
            _logger.Log(string.Format("Pipe {0}: Started", _pipeNumber), MessageLevel.Debug);
            Interlocked.Increment(ref pipeCount);

            try
            {
                foreach (var row in inputQueue.GetConsumingEnumerable())
                {
                    if (ct.IsCancellationRequested == true)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    Load(row, ref errorCount, rowLogAction);
                    Interlocked.Increment(ref rowprocessedCount);
                }

                _logger.Log(string.Format("Pipe {0}: Finished", _pipeNumber), MessageLevel.Debug);
            }
            catch (OperationCanceledException)
            {
                throw new TransformationPipeException("Pipe Canceled Prior to Loader");
            }
            finally
            {
                Interlocked.Decrement(ref pipeCount);
            }
        }

        /// <summary> 
        /// Performs the Transformations against the row
        /// </summary> 
        /// <param name="row">The row to process</param> 
        /// <param name="errorCount">The total number of errors accross the pipes(threads)</param>
        /// <param name="rowLogAction">The row logging function (success,dropped,number,message)</param>  
        public void Load(Dictionary<string, object> row, ref int errorCount, Action<bool, bool, int, string> rowLogAction)
        {
            string curTransformation = "";
            int rowNo = -1;

            if (row != null && row["#no"] != null)
            {
                rowNo = (int)row["#no"];
            }

            try
            {
                foreach (var tranKV in _transformationPipe)
                {
                    var tran = tranKV.Value;
                    curTransformation = tranKV.Key;
                    tran.Transform(row);

                    if (row.ContainsKey("#DROP") && (bool)row["#DROP"] == true)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("Error Transforming Row {0} in {2} : {1}", rowNo, ex.Message, curTransformation);

                _logger.Log(string.Format("Pipe {0}: {1}", _pipeNumber, errMsg), MessageLevel.Critical);

                Interlocked.Increment(ref errorCount);
                if (_errorsallowed != -1 && errorCount >= _errorsallowed)
                {
                    throw (new TransformationPipeException(string.Format("Number of Errors has Exceeded the limit {0}", _errorsallowed)));
                }
            }
        }
        #endregion

        #region "IDisposable Support"
        // To detect redundant calls
        private bool disposedValue;

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    //dispose managed state (managed objects).
                    foreach (var tranKV in _transformationPipe)
                    {
                        var tran = tranKV.Value;
                        if (tran != null && tran is IDisposable)
                        {
                            ((IDisposable)tran).Dispose();
                        }
                    }
                }
            }
            this.disposedValue = true;
        }

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

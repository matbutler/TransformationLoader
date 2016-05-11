using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using log4net;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Data.SqlClient;

namespace TransformationCore
{
    public class TransformationPipe : IDisposable
    {

        #region "Private Variables"
        private Dictionary<string, ITransformation> _transformationPipe = new Dictionary<string, ITransformation>();
        private bool _anySQLTran = false;

        private bool _anyReqTransaction = false;
        private string _connstr = "";
        private int _errorsallowed = -1;
        private int _pipeNumber = -1;

        private ILog _logger;
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
        public TransformationPipe(XElement config, Dictionary<string, object> globalData, ILog logger, int pipeNumber)
        {
            _logger = logger;
            _pipeNumber = pipeNumber;

            //If the pipe number is greater than 1 only show messages in Debug
            MessageLevel mgslvl = MessageLevel.Info;
            if (_pipeNumber > 1)
            {
                mgslvl = MessageLevel.Debug;
            }

            //Check Config XML
            if (config.Element("pipe") == null)
            {
                throw new TransformationPipeException("Missing Pipe Config");
            }
            else if (config.Element("pipe").Elements("transformation").Count() == 0)
            {
                throw new TransformationPipeException("Config has no Transformations");
            }

            if (config.Element("connection") != null && config.Element("connection").Attribute("value") != null)
            {
                _connstr = config.Element("connection").Attribute("value").Value;
                _logger.Log(string.Format("Pipe {0}: Connection String = {1}", _pipeNumber, _connstr), mgslvl);
            }

            if (config.Element("pipe").Attribute("errorsallowed") != null)
            {
                _errorsallowed = Convert.ToInt32(config.Element("pipe").Attribute("errorsallowed").Value);

                if (_errorsallowed == -1)
                {
                    _logger.Log(string.Format("Pipe {0}: Errors Allowed = Any", _pipeNumber), mgslvl);
                }
                else
                {
                    _logger.Log(string.Format("Pipe {0}: Errors Allowed = {1}", _pipeNumber, _errorsallowed), mgslvl);
                }
            }

            var catalog = new DirectoryCatalog("Engine");
            var container = new CompositionContainer(catalog);

            int count = 0;
            //Load Each Transformation from XML
            foreach (var tranConfig in config.Element("pipe").Elements("transformation"))
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
                    if (_pipeNumber == 1)
                    {
                        mgslvl = MessageLevel.Action;
                    }
                    else
                    {
                        mgslvl = MessageLevel.Debug;
                    }

                    TransformationFactory _transformationFactory = new TransformationFactory();
                    container.ComposeParts(_transformationFactory);

                    var tran = _transformationFactory.CreateTransformation(tranName, tranVersion);

                    if (tran is ISQLTransformation)
                    {
                        _anySQLTran = true;
                        if (((ISQLTransformation)tran).RequireSQLTran)
                        {
                            _anyReqTransaction = true;
                        }
                    }


                    tran.initialise(tranConfig, globalDS, _connstr, _logger, fileDate, transformationOutput);

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

        /// <summary> 
        /// Defines the Transformations to run and sets the config properties
        /// </summary> 
        /// <param name="inputQueue">The thread safe Queue</param> 
        /// <param name="errorCount">The total number of errors accross the pipes(threads)</param> 
        /// <param name="pipeCount">The number of the Pipe</param> 
        /// <param name="rowprocessedCount">The variable to update number of rows processed</param> 
        /// <param name="ct">The cancellation object to cancel cross pipes(threads)</param> 
        /// <param name="rowLogAction">The row logging function (success,dropped,number,message)</param> 
        public void Load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, ref int pipeCount, ref int rowprocessedCount, CancellationToken ct, Action<bool, bool, int, string, SqlConnection> rowLogAction)
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
        public void Load(Hashtable row, ref int errorCount, Action<bool, bool, int, string, SqlConnection> rowLogAction)
        {
            string curTransformation = "";
            int rowNo = -1;

            if (row != null && row["#no"] != null)
            {
                rowNo = (int)row["#no"];
            }

            try
            {
                if (_anySQLTran)
                {
                    //If pipe contains a SQL Transformation apply a transaction
                    SqlTransaction trSqlTran = null;

                    using (var trSqlConn = new SqlConnection(_connstr))
                    {
                        trSqlConn.Open();

                        try
                        {
                            if (_anyReqTransaction)
                            {
                                trSqlTran = trSqlConn.BeginTransaction();
                            }

                            foreach (var tranKV in _transformationPipe)
                            {
                                var tran = tranKV.Value;
                                curTransformation = tranKV.Key;

                                row = tran.Transform(row, trSqlConn, trSqlTran);

                                if (row.ContainsKey("#DROP") && (bool)row["#DROP"] == true)
                                {
                                    string errorMsg = null;

                                    if (row.ContainsKey("#DROPREASON"))
                                    {
                                        errorMsg = string.Format("Row Dropped: {0}", row["#DROPREASON"]);
                                    }
                                    else
                                    {
                                        errorMsg = "Row Dropped";
                                    }

                                    rowLogAction(false, true, rowNo, errorMsg, trSqlConn);
                                    //If Transformation Fails then rollback the transaction
                                    if (_anyReqTransaction)
                                    {
                                        try
                                        {
                                            trSqlTran.Rollback();
                                        }
                                        catch (SqlException sqlex)
                                        {
                                            if (trSqlTran.Connection != null)
                                            {
                                                throw new TransformationPipeException(string.Format("{0}, Unable to Rollback {1}.", sqlex.Message, sqlex.GetType()));
                                            }
                                        }
                                    }

                                    return;
                                }
                            }


                            if (rowLogAction != null)
                            {
                                rowLogAction(true, false, rowNo, null, trSqlConn);
                            }

                            if (_anyReqTransaction)
                            {
                                trSqlTran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            var rowDropped = (ex is TransformationDropRowException);

                            if (rowLogAction != null)
                            {
                                rowLogAction(false, rowDropped, rowNo, ex.Message, trSqlConn);
                            }

                            //If Transformation Fails then rollback the transaction
                            if (_anyReqTransaction)
                            {
                                try
                                {
                                    trSqlTran.Rollback();
                                }
                                catch (SqlException sqlex)
                                {
                                    if (trSqlTran.Connection != null)
                                    {
                                        throw new TransformationPipeException(string.Format("{0}, Unable to Rollback {1}.", sqlex.Message, sqlex.GetType()));
                                    }
                                }
                            }

                            //Don't Rethrow when just dropping row
                            if (!rowDropped)
                            {
                                throw;
                            }
                        }


                        trSqlConn.Close();
                    }

                }
                else
                {
                    try
                    {
                        foreach (var tranKV in _transformationPipe)
                        {
                            var tran = tranKV.Value;
                            curTransformation = tranKV.Key;
                            row = tran.Transform(row);

                            if (row.ContainsKey("#DROP") && (bool)row["#DROP"] == true)
                            {
                                return;
                            }
                        }
                    }
                    catch (TransformationDropRowException)
                    {
                        //Don't Rethrow when just dropping row
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = null;

                if (ex is TransformationException)
                {
                    errMsg = string.Format("Error Transforming Row {0} in {2} : {1}", rowNo, ex.Message, curTransformation);
                }
                else
                {
                    errMsg = string.Format("Error on Row {0} in {2} : {1}", rowNo, ex.Message, curTransformation);
                }


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

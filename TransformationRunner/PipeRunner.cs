using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TransformationCore.Exceptions;
using TransformationCore.Enums;
using TransformationCore.Interfaces;
using TransformationCore;

namespace Transformation.Loader
{
    public class PipeRunner : IDisposable
    {
        private Dictionary<string, ITransformation> _transformationPipe;
        private int _pipeNumber;
        private int _errorsAllowed;

        private ILogger _logger;

        public PipeRunner(int pipeNumber, Dictionary<string, ITransformation> transformationPipe, int errorsAllowed, ILogger logger)
        {
            _transformationPipe = transformationPipe;
            _pipeNumber = pipeNumber;
            _errorsAllowed = errorsAllowed;
            _logger = logger;
        }

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

                    LoadRow(row, ref errorCount, rowLogAction);
                    Interlocked.Increment(ref rowprocessedCount);
                }

                ClosePipe();

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

        private void ClosePipe()
        {
            foreach (var tranKV in _transformationPipe)
            {
                tranKV.Value.Close();
            }
        }

        private void LoadRow(Dictionary<string, object> row, ref int errorCount, Action<bool, bool, int, string> rowLogAction)
        {
            string curTransformation = "";

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
                long rowNo = -1;

                if (row != null && row["#row"] != null)
                {
                    rowNo = (long)row["#row"];
                }

                string errMsg = string.Format("Error Transforming Row {0} in {2} : {1}", rowNo, ex.Message, curTransformation);

                _logger.Log(string.Format("Pipe {0}: {1}", _pipeNumber, errMsg), MessageLevel.Critical);

                Interlocked.Increment(ref errorCount);
                if (_errorsAllowed != -1 && errorCount >= _errorsAllowed)
                {
                    throw (new TransformationPipeException(string.Format("Number of Errors has Exceeded the limit {0}", _errorsAllowed)));
                }
            }
        }

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

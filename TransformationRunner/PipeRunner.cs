﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TransformationCore.Exceptions;
using TransformationCore.Enums;
using TransformationCore.Interfaces;
using TransformationCore;
using Logging;

namespace Transformation.Loader
{
    public class PipeRunner : IDisposable
    {
        private Dictionary<string, ITransformation> _transformationPipe;
        private int _pipeNumber;
        private int _errorsAllowed;
        private RowLogAction _rowLogAction;

        private ILogger _logger;

        public PipeRunner(int pipeNumber, Dictionary<string, ITransformation> transformationPipe, int errorsAllowed, ILogger logger, RowLogAction rowLogAction)
        {
            _transformationPipe = transformationPipe;
            _pipeNumber = pipeNumber;
            _errorsAllowed = errorsAllowed;
            _rowLogAction = rowLogAction;
            _logger = logger;
        }

        public void Load(BlockingCollection<Dictionary<string, object>> inputQueue, ref int errorCount, ref int pipeCount, ref int rowprocessedCount, CancellationToken ct)
        {
            _logger.Debug(string.Format("Pipe {0}: Started", _pipeNumber));
            Interlocked.Increment(ref pipeCount);

            try
            {
                foreach (var row in inputQueue.GetConsumingEnumerable())
                {
                    if (ct.IsCancellationRequested == true)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    LoadRow(row, ref errorCount);
                    Interlocked.Increment(ref rowprocessedCount);
                }

                ClosePipe();

                _logger.Debug(string.Format("Pipe {0}: Finished", _pipeNumber));
            }
            catch (OperationCanceledException)
            {
                throw new TransformationPipeException("Pipe Cancelled");
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

        private void LoadRow(Dictionary<string, object> row, ref int errorCount)
        {
            string curTransformation = "";

            long rowNo = -1;

            if (row != null && row["#row"] != null)
            {
                rowNo = (long)row["#row"];
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
                        _rowLogAction?.Invoke(false, true, rowNo, null);
                        return;
                    }
                }
                _rowLogAction?.Invoke(true, false, rowNo, null);
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("Error Transforming Row {0} in {2} : {1}", rowNo, ex.Message, curTransformation);

                _rowLogAction?.Invoke(false, false, rowNo, errMsg);

                Interlocked.Increment(ref errorCount);
                if (_errorsAllowed != -1 && errorCount >= _errorsAllowed)
                {
                    _logger.Fatal(string.Format("Pipe {0}: {1}", _pipeNumber, errMsg));
                    throw (new TransformationPipeException(string.Format("Number of Errors has Exceeded the limit {0}", _errorsAllowed)));
                }
                else
                {
                    _logger.Warn(string.Format("Pipe {0}: {1}", _pipeNumber, errMsg));
                }
            }
        }

        #region "IDisposable Support"
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

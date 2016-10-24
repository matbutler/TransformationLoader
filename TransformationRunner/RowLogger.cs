using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransformationCore;

namespace Transformation.Loader
{
    public class RowLogger : IRowLogger, IDisposable
    {
        private string _filepath;
        private static object locker = new Object();
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static StreamWriter _stream = null;

        public void Initialise(string processId)
        {
            _filepath = string.Format("LOADING_{0}.csv", processId);
            _stream = new StreamWriter(_filepath, true, Encoding.UTF8, 65536);
        }

        public void Complete()
        {
            if(_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
            }

            if (File.Exists(_filepath))
            {
                File.Delete(_filepath);
            }
        }

        public void LogRow(bool rowSucess, bool rowDropped, long rowNumber, string rowError)
        {
            if(_stream == null)
            {
                throw new Exception("Rowlogger file not set");
            }

            _readWriteLock.EnterWriteLock();
            try
            {
                _stream.WriteLine(string.Format("{0},{1},{2},{3}", rowNumber, rowSucess, rowDropped, rowError));
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _stream?.Dispose();
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
    }
}

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
    public class RowLogger : IRowLogger
    {
        private string _filepath;
        private static SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public void Initialise(Guid processId)
        {
            _filepath = string.Format("LOADING_{0}.csv", processId);
        }

        public void Complete()
        {
            if (File.Exists(_filepath))
            {
                File.Delete(_filepath);
            }
        }

        private async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            await _fileLock.WaitAsync();

            FileStream sourceStream = null;
            try 
            {
                sourceStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
            finally
            {
                _fileLock.Release();
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                }
            }
        }

        public void LogRow(bool rowSucess, bool rowDropped, long rowNumber, string rowError)
        {
            WriteTextAsync(_filepath, string.Format("{0},{1},{2},{3}" + Environment.NewLine, rowNumber, rowSucess, rowDropped, rowError)).Wait();
        }
    }
}

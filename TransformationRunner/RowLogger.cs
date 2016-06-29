using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Transformation.Loader
{
    public class RowLogger
    {
        private string _connStr;
        private string _tablename;
        private string _filepath;
        private DataTable _loggingTable;
        private int _rows;

        public RowLogger(string connStr, Guid processId)
        {
            _connStr = connStr;
            _rows = 0;

            _tablename = string.Format("LOADING_{0}", processId);
            _filepath = _tablename + ".csv";

            _loggingTable = new DataTable();

            _loggingTable.Columns.Add(new DataColumn { DataType = typeof(int), ColumnName = "RowNumber" });
            _loggingTable.Columns.Add(new DataColumn { DataType = typeof(bool), ColumnName = "Success" });
            _loggingTable.Columns.Add(new DataColumn { DataType = typeof(bool), ColumnName = "Dropped" });
            _loggingTable.Columns.Add(new DataColumn { DataType = typeof(string), ColumnName = "ErrorMsg" });
        }

        public void Complete()
        {
            //Final bulk copy
            BulkCopy();

            if (File.Exists(_filepath))
            {
                File.Delete(_filepath);
            }
        }

        private async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        private void BulkCopy()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                try
                {
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null))
                    {
                        bulkCopy.DestinationTableName = _tablename;
                        bulkCopy.BatchSize = 5000;

                        bulkCopy.ColumnMappings.Add("RowNumber", "RowNumber");
                        bulkCopy.ColumnMappings.Add("Success", "Success");
                        bulkCopy.ColumnMappings.Add("Dropped", "Dropped");
                        bulkCopy.ColumnMappings.Add("ErrorMsg", "ErrorMsg");

                        bulkCopy.WriteToServer(_loggingTable);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    _loggingTable.Clear();
                }
            }
        }

        public async void LogRow(bool rowSucess, bool rowDropped, int rowNumber, string rowError)
        {
            await WriteTextAsync(_filepath, string.Format("{0},{1},{2},{3}/r/n", rowNumber, rowSucess, rowDropped, rowError));

            lock (_loggingTable)
            {
                DataRow newRow = _loggingTable.NewRow();

                newRow["RowNumber"] = rowNumber;
                newRow["Success"] = rowSucess;
                newRow["Dropped"] = rowDropped;
                newRow["ErrorMsg"] = rowError;

                _loggingTable.Rows.Add(newRow);
                _rows++;

                if (_rows >= 5000)
                {
                    BulkCopy();
                    _rows = 0;
                }
            }
        }
    }
}

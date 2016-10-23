using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransformationCore;

namespace FileProcessing.Loader
{
    public class DBRowLogger : IRowLogger
    {
        private string _connStr;
        private string _tablename;
        private DataTable _loggingTable;
        private int _rows;
        private static SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public DBRowLogger(string connStr)
        {
            _connStr = connStr;
        }

        public void Initialise(string processId)
        {
            _rows = 0;

            _tablename = string.Format("LOADING_{0}", processId);

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

        public void LogRow(bool rowSucess, bool rowDropped, long rowNumber, string rowError)
        {
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

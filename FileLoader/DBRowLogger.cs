using System.Data;
using System.Data.SqlClient;
using System.Threading;
using TransformationCore;

namespace FileProcessing.Loader
{
    public class DBRowLogger : IRowLogger
    {
        private string _connStr;
        private int _id;
        private DataTable _loggingTable;
        private int _rows;
        private static SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public DBRowLogger(string connStr, int id)
        {
            _connStr = connStr;
            _id = id;
        }

        public void Initialise(string processId)
        {
            _rows = 0;

            _loggingTable = new DataTable();

            _loggingTable.Columns.Add(new DataColumn { DataType = typeof(int), ColumnName = "FileProcessQueue_Id" });
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
                        bulkCopy.DestinationTableName = "FileProcessRowAudit";
                        bulkCopy.BatchSize = 5000;

                        bulkCopy.ColumnMappings.Add("FileProcessQueue_Id", "FileProcessQueue_Id");
                        bulkCopy.ColumnMappings.Add("RowNumber", "RowNumber");
                        bulkCopy.ColumnMappings.Add("Success", "Success");
                        bulkCopy.ColumnMappings.Add("Dropped", "Dropped");
                        bulkCopy.ColumnMappings.Add("ErrorMsg", "ErrorMsg");

                        bulkCopy.WriteToServer(_loggingTable);
                    }
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

                newRow["FileProcessQueue_Id"] = _id;
                newRow["RowNumber"] = rowNumber;
                newRow["Success"] = rowSucess;
                newRow["Dropped"] = rowDropped;
                newRow["ErrorMsg"] = string.IsNullOrWhiteSpace(rowError) ? "" : rowError;

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

using System.Data.SqlClient;

namespace FileProcessing.Watcher
{
    public class FileWatchAudit : IFileWatchAudit
    {
        private readonly string _connectionString;
        public FileWatchAudit(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Log(string filepath)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("FileProcessAudit", connection))
            {
                connection.Open();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Filepath", filepath);

                command.ExecuteNonQuery();
            }
        }
    }
}

using FileProcessing.Loader.Models;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace FileProcessing.Loader
{
    public class FileSelector : IFileSelector
    {
        private readonly string _connectionString;
        public FileSelector(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ProcessFile GetFileToProcess()
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("FileProcessAuditLog", connection))
            {
                connection.Open();
                command.CommandType = System.Data.CommandType.StoredProcedure;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var config = XElement.Parse(reader["FileProcessConfig"].ToString());

                        return new ProcessFile
                        {
                             FilePath = reader["Filepath"].ToString(),
                             Config = config,
                        };
                    }
                }
            }
            return null;
        }
    }
}

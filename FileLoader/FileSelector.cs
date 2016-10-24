using FileProcessing.Loader.Models;
using System;
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
            using (var command = new SqlCommand("GetNextFileToProcess", connection))
            {
                connection.Open();
                command.CommandType = System.Data.CommandType.StoredProcedure;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var config = reader["ProcessConfig"]?.ToString();
                        var filepath = reader["Filepath"].ToString();

                        if (string.IsNullOrWhiteSpace(config))
                        {
                            throw new Exception(string.Format("Missing file configuration for file {0}", filepath));
                        }

                        var configXML = XElement.Parse(config);

                        return new ProcessFile
                        {
                            FilePath = filepath,
                            Id = (int)reader["Id"],
                            Config = configXML,
                        };
                    }
                }
            }
            return null;
        }
    }
}

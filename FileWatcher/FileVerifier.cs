using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileWatcher
{
    public class FileVerifier : IFileVerifier
    {
        private readonly Dictionary<int, Regex> _fileConfigurations;

        public FileVerifier(string connectionString)
        {
            _fileConfigurations = GetFileConfigurations(connectionString);
        }

        private static Dictionary<int, Regex> GetFileConfigurations(string connectionString)
        {
            var patterns = new Dictionary<int, Regex>();

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("GetFilePatterns", connection))
            {
                connection.Open();
                command.CommandType = System.Data.CommandType.StoredProcedure;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var regex = new Regex(reader["Filepattern"].ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        patterns.Add(reader.GetInt32(reader.GetOrdinal("Id")), regex);
                    }
                }
            }

            return patterns;
        }

        public bool RequiresProcessing(string fileName, out int fileConfigId)
        {
            fileConfigId = 0;
            var fileConfiguration = _fileConfigurations.Where(x => x.Value.IsMatch(fileName)).Select(x=>(int?)x.Key).FirstOrDefault();

            if (!fileConfiguration.HasValue)
            {

            }

            fileConfigId = fileConfiguration.Value;

            return true;
        }
    }
}

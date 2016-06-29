using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace FileWatcher.Setup
{
    public static class DbSetup
    {
        /// <summary>
        /// Run all embedded-resource SQL scripts ending in ".dbsetup.sql" on the target database,
        /// then run the embedded-resource script "[environment].dbseed.sql" if it exists.
        /// </summary>
        /// <param name="connectionStringName">The name of the ADO connection string in web.config to use.</param>
        /// <param name="environment">The name of the current environment.</param>
        public static void Run(string connectionStringName)
        {
            var scriptFiles = typeof(DbSetup).Assembly.GetManifestResourceNames().Where(r => r.ToLower().EndsWith(".dbsetup.sql"));
            foreach (var scriptFile in scriptFiles)
            {
                RunDbScript(connectionStringName, scriptFile);
            }
        }

        private static void RunDbScript(string connectionStringName, string scriptFile)
        {
            string script = ReadScript(scriptFile);

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString))
            using (var command = new SqlCommand(script, connection))
            {
                connection.Open();
                var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    command.Transaction = transaction;

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private static string ReadScript(string scriptFile)
        {
            string script;
            using (var stream = typeof(DbSetup).Assembly.GetManifestResourceStream(scriptFile))
            {
                if (stream == null)
                {
                    throw new Exception($"Unable to open a stream for the embedded resource `{scriptFile}`.");
                }

                using (var reader = new StreamReader(stream))
                {
                    script = reader.ReadToEnd();
                }
            }

            return script;
        }
    }

}

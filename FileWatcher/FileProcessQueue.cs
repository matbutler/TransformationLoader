using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace FileWatcher
{
    public class FileProcessQueue : IFileProcessQueue
    {
        private readonly string _connectionString;
        public FileProcessQueue(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Enqueue(string filepath, int fileConfigId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("FileProcessEnqueue", connection))
            {
                connection.Open();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@FileConfigId", fileConfigId);
                command.Parameters.AddWithValue("@Filepath", filepath);

                command.ExecuteNonQuery();
            }
        }

        //public static EventWaitHandle CreateSharedEventHandle()
        //{
        //    var eventWaitHandleSecurity = new EventWaitHandleSecurity();

        //    var securityIdentifier = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        //    eventWaitHandleSecurity.AddAccessRule(new EventWaitHandleAccessRule(securityIdentifier, EventWaitHandleRights.FullControl, AccessControlType.Allow));

        //    bool created;
        //    return new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\" + _signalKey, out created, eventWaitHandleSecurity);
        //}
    }
}

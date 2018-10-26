using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace RunSqlScript
{
    public sealed class RunSqlScriptJob : Job
    {
        private readonly string _connectionString;
        private readonly string[] _files;

        public RunSqlScriptJob(string connectionString, string[] files)
        {
            _connectionString = connectionString;
            _files = files;
        }

        protected override double GetTotalTasks()
        {
            return _files.Length + 2;
        }

        protected override void ExecuteTasks()
        {
            RaiseStateChanged("Connecting to sql server");
            var sqlConnection = new SqlConnection(_connectionString);
            var server = new Server(new ServerConnection(sqlConnection));
            if (IsCancelled)
            {
                return;
            }
            foreach (var file in _files)
            {
                if (IsCancelled) {
                    Cancelled();
                    return;
                }
                RaiseStateChanged("Executing " + Path.GetFileName(file));
                var script = File.ReadAllText(file);
                server.ConnectionContext.ExecuteNonQuery(script);
            }
            Completed();
        }
    }
}
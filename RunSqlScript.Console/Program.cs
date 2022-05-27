using Dapper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunSqlScript.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = @"";
            var file = @"";
            var endless = true;
            var cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyy-MM-ddTHH:mm:ss} {Level:w4}] {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            while (true)
            {
                Log.Information("Supported commands: start, stop, exit");
                var line = System.Console.ReadLine().Trim().ToLower();
                switch (line)
                {
                    case "start":
                        Log.Information($"Starting...");
                        cancellationToken = new CancellationTokenSource();
                        token = cancellationToken.Token;
                        Task.Factory.StartNew(() =>
                        {
                            ExecuteSql(connectionString, file, endless, token);
                        }, token);
                        Log.Information($"Started");
                        break;
                    case "stop":
                        Log.Information($"Stopping...");
                        cancellationToken.Cancel();
                        Log.Information($"Stopped");
                        break;
                    case "exit":
                        return;
                    default:
                        Log.Error("Supported commands: start, stop, exit");
                        break;
                }

            }

        }

        private static void ExecuteSql(string connectionString, string file, bool endless, CancellationToken token)
        {
            //var dataSource = new SqlConnectionStringBuilder(connectionString).DataSource;
            //var server = new Server(new ServerConnection(dataSource));
            var sqlConnection = new SqlConnection(connectionString);
            if (token.IsCancellationRequested)
            {
                Log.Information("Cancelled");
                return;
            }

            var stopWatch = new Stopwatch();
            var oldScript = default(string);
            while (!token.IsCancellationRequested)
            {
                stopWatch.Start();
                if (token.IsCancellationRequested)
                {
                    Log.Information("Cancelled");
                    return;
                }
                var script = File.ReadAllText(file);
                if (oldScript != script)
                {
                    if (string.IsNullOrEmpty(oldScript))
                    {
                        Log.Information($"Executing...{Environment.NewLine}{script}");
                    }
                    else
                    {
                        Log.Information($"File updated, executing...{Environment.NewLine}{script}");
                    }
                    oldScript = script;
                }
                try
                {
                    sqlConnection.Execute(script);
                    //server.ConnectionContext.ExecuteNonQuery(script);
                }
                catch (Exception e)
                {
                    var inner = e.InnerException != null ? $"{e.InnerException.GetType().Name}: {e.InnerException.Message}" : "";
                    Log.Error($"{e.GetType().Name}: {e.Message}" + (string.IsNullOrEmpty(inner) ? "" : inner));
                    return;
                }

                if (!endless)
                {
                    return;
                }

                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds < 50)
                {
                    Thread.Sleep(50 - (int)stopWatch.ElapsedMilliseconds);
                }
                stopWatch.Restart();

                //Log.Information("Completed");
            }
        }
    }
}

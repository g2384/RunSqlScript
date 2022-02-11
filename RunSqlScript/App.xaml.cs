using NLog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RunSqlScript
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException(e.ExceptionObject as Exception, "Domain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "UnobservedTaskException");

            var argumentProvider = new CommandLineArgumentProvider();
            var sqlFiles = argumentProvider.GetSqlFiles();
            var connectionString = argumentProvider.GetConnectionString();
            if (sqlFiles.Any() && !string.IsNullOrWhiteSpace(connectionString)) {
                var task = new RunSqlScriptJob(connectionString, sqlFiles);
                task.Execute();
                Shutdown(0);
            }
        }

        private static void LogUnhandledException(Exception exception, string source)
        {
            var message = $"Unhandled exception ({source})";
            try
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                message = $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}";
            }
            catch (Exception ex)
            {
                LogError(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                LogError(exception, message);
            }
        }

        private static void LogError(Exception exception, string message)
        {
            Logger.Error(exception, message);
        }
    }
}

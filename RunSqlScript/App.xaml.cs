using MahApps.Metro;
using NLog;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

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
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.AddAccent("Theme", new Uri("pack://application:,,,/RunSqlScript;component/Theme.xaml"));

            // get the current app style (theme and accent) from the application
            var theme = ThemeManager.DetectAppStyle(Current);

            // now change app style to the custom accent and current theme
            ThemeManager.ChangeAppStyle(Current,
                ThemeManager.GetAccent("Theme"),
                theme.Item1);

            base.OnStartup(e);
        }

        private void LogUnhandledException(Exception exception, string source)
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

        private void LogError(Exception exception, string message)
        {
            Logger.Error(exception, message);
        }
    }
}

using System;
using System.IO;
using System.Windows;

namespace SteamDebloat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Configurar manejo global de errores
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogError("Error during OnStartup", ex);
                MessageBox.Show($"Error during application startup: {ex.Message}\n\nDetails: {ex}", 
                              "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher Unhandled Exception", e.Exception);
            
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nDetails: {e.Exception}", 
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
            this.Shutdown(1);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("AppDomain Unhandled Exception", ex);
                
                MessageBox.Show($"A critical error occurred: {ex.Message}\n\nDetails: {ex}", 
                              "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogError(string context, Exception ex)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "SteamDebloat_Error.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                                $"Exception: {ex}\n" +
                                $"StackTrace: {ex.StackTrace}\n" +
                                $"----------------------------------------\n";
                
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }
    }
}
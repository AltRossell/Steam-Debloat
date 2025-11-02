using System;
using System.Threading.Tasks;
using System.Windows;

namespace SteamDebloat
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize theme system first
            ThemeManager.Initialize();

            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Run bootstrapper
            bool initSuccess = await Bootstrapper.InitializeAsync();
            
            if (!initSuccess)
            {
                Shutdown();
                return;
            }

            // Log startup
            Bootstrapper.LogStartup();

            // Continue with normal startup - MainWindow will show after Millennium warning
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"An unhandled error occurred:\n\n{exception?.Message}\n\nThe application will now close.",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"An error occurred:\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}
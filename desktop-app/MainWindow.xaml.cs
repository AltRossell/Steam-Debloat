using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SteamDebloat
{
    public partial class MainWindow : Window
    {
        private readonly SteamDebloatService _steamService;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isProcessing = false;
        private bool _steamInstalled = false; // Add this missing field

        public MainWindow()
        {
            InitializeComponent();
            _steamService = new SteamDebloatService();
            _steamService.ProgressChanged += OnProgressChanged;
            _steamService.SteamDetectionChanged += OnSteamDetectionChanged; // Subscribe to Steam detection events
            
            InitializeUI();
        }

        private async void InitializeUI()
        {
            CheckAdminPrivileges();
            ModeComboBox.SelectedIndex = 0;
            await LoadSystemInfo();
        }

        private void OnSteamDetectionChanged(bool steamInstalled)
        {
            Dispatcher.Invoke(() =>
            {
                _steamInstalled = steamInstalled;
            });
        }

        private void CheckAdminPrivileges()
        {
            bool isAdmin = _steamService.IsRunningAsAdmin();
            
            if (isAdmin)
            {
                AdminStatusText.Text = "Administrator privileges: OK";
                AdminStatusText.Foreground = new SolidColorBrush(Colors.Green);
                StatusIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Shield;
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                AdminStatusText.Text = "Administrator privileges required";
                AdminStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                StatusIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ShieldAlert;
                StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
            }
        }

        private async Task LoadSystemInfo()
        {
            try
            {
                var systemInfo = await _steamService.GetSystemInfoAsync();
                
                OSVersionText.Text = systemInfo.OSVersion;
                OSArchText.Text = systemInfo.Architecture;
                SteamPathText.Text = systemInfo.SteamFound ? systemInfo.SteamPath : "Steam not found";
                SteamConfigText.Text = systemInfo.ConfigExists ? "Steam.cfg found" : "No previous configuration";
                
                // Update the steam installation status
                _steamInstalled = systemInfo.SteamFound;
                
                UpdateModeSpecificInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading system information: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateModeSpecificInfo();
        }

        private void UpdateModeSpecificInfo()
        {
            if (ModeComboBox.SelectedItem is ComboBoxItem selected && selected.Tag != null)
            {
                string mode = selected.Tag.ToString() ?? "";
                
                if (mode == "NormalBoth2022-2025")
                {
                    SteamV2Label.Visibility = Visibility.Visible;
                    SteamV2PathText.Visibility = Visibility.Visible;
                    SteamV2PathText.Text = "Will be installed at: " + @"C:\Program Files (x86)\Steamv2";
                    UpdateSteam.Content = "Automatic update (both versions)";
                    UpdateSteam.IsEnabled = false;
                    UpdateSteam.IsChecked = true;
                }
                else
                {
                    SteamV2Label.Visibility = Visibility.Collapsed;
                    SteamV2PathText.Visibility = Visibility.Collapsed;
                    
                    // Enable UpdateSteam checkbox for ALL individual modes
                    UpdateSteam.IsEnabled = true;
                    UpdateSteam.Content = "Update Steam automatically";
                    UpdateSteam.IsChecked = true; // Default to checked for all modes
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            // Check if Steam is installed first
            if (!_steamInstalled)
            {
                MessageBox.Show(
                    "Steam is not installed on this system.\n\n" +
                    "Please install Steam from https://store.steampowered.com/about/ and try again.\n\n" +
                    "The application will automatically detect when Steam is installed.",
                    "Steam Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!_steamService.IsRunningAsAdmin())
            {
                var result = MessageBox.Show(
                    "This application requires administrator privileges to function correctly.\n\nDo you want to restart it as administrator?",
                    "Administrator Privileges",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    RestartAsAdmin();
                }
                return;
            }

            await StartOptimization();
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            var result = MessageBox.Show(
                "Are you sure you want to uninstall Steam Debloat?\n\n" +
                "This will remove optimization configurations and restore Steam to its original state.\n" +
                "Your games and personal settings will be preserved.",
                "Confirm Uninstallation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            if (!_steamService.IsRunningAsAdmin())
            {
                var adminResult = MessageBox.Show(
                    "Administrator privileges are required for uninstallation.\n\nDo you want to restart as administrator?",
                    "Administrator Privileges",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (adminResult == MessageBoxResult.Yes)
                {
                    RestartAsAdmin();
                }
                return;
            }

            await StartUninstall();
        }

        private async Task StartOptimization()
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                ShowProgressOverlay(true);
                StartButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;

                var config = GetOptimizationConfig();
                
                var result = await _steamService.OptimizeSteamAsync(config, _cancellationTokenSource.Token);

                if (result.Success)
                {
                    MessageBox.Show($"Optimization completed successfully in {result.Duration:mm\\:ss}", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Error during optimization:\n{result.ErrorMessage}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled by user.", 
                              "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                ShowProgressOverlay(false);
                StartButton.IsEnabled = true;
                UninstallButton.IsEnabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                // Reload system info after operation
                await LoadSystemInfo();
            }
        }

        private async Task StartUninstall()
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                ShowProgressOverlay(true);
                StartButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;

                var result = await _steamService.UninstallAsync(_cancellationTokenSource.Token);

                if (result.Success)
                {
                    MessageBox.Show("Uninstallation completed successfully.", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Error during uninstallation:\n{result.ErrorMessage}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled by user.", 
                              "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                ShowProgressOverlay(false);
                StartButton.IsEnabled = true;
                UninstallButton.IsEnabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                // Reload system info after operation
                await LoadSystemInfo();
            }
        }

        private OptimizationConfig GetOptimizationConfig()
        {
            var selectedItem = (ComboBoxItem)ModeComboBox.SelectedItem;
            
            return new OptimizationConfig
            {
                Mode = selectedItem.Tag?.ToString() ?? "Normal2025July",
                CreateDesktopShortcut = CreateDesktopShortcut.IsChecked == true,
                CreateStartMenuShortcut = CreateStartMenuShortcut.IsChecked == true,
                RemoveFromStartup = RemoveFromStartup.IsChecked == true,
                UpdateSteam = UpdateSteam.IsChecked == true && UpdateSteam.IsEnabled
            };
        }

        private void ShowProgressOverlay(bool show)
        {
            ProgressOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private void OnProgressChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = status;
            });
        }

        private void RestartAsAdmin()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "",
                    Verb = "runas"
                };

                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not restart as administrator: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _steamService?.Dispose();
            base.OnClosed(e);
        }
    }
}
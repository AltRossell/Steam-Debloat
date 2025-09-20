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
        private SteamDebloatService _steamService;
        private UninstallService _uninstallService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isProcessing = false;
        private string _customSteamPath = string.Empty;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Initialize basic components first
                InitializeBasicUI();
                
                // Then initialize services
                Task.Run(async () => await InitializeServicesAsync());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing MainWindow: {ex.Message}\n\nDetails: {ex}", 
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeBasicUI()
        {
            try
            {
                // Basic configurations that don't require external services
                if (ModeComboBox != null)
                    ModeComboBox.SelectedIndex = 0;
                
                CheckAdminPrivileges();
                SetDefaultValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InitializeBasicUI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        _steamService = new SteamDebloatService();
                        _steamService.ProgressChanged += OnProgressChanged;
                        _steamService.SteamDetectionChanged += OnSteamDetectionChanged;

                        _uninstallService = new UninstallService();
                        _uninstallService.ProgressChanged += OnProgressChanged;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating services: {ex.Message}", "Service Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                });

                // Load system information asynchronously
                await LoadSystemInfoSafe();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateUninstallStatus();
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error in InitializeServicesAsync: {ex.Message}", "Service Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private void SetDefaultValues()
        {
            try
            {
                // Set default values safely for both tabs
                if (OSVersionText != null)
                    OSVersionText.Text = "Loading...";
                
                if (OSArchText != null)
                    OSArchText.Text = "Loading...";
                
                if (SteamPathText != null)
                    SteamPathText.Text = "Searching...";
                
                if (SteamConfigText != null)
                    SteamConfigText.Text = "Searching...";
                
                // Uninstaller tab system info
                if (OSVersionText2 != null)
                    OSVersionText2.Text = "Loading...";
                
                if (OSArchText2 != null)
                    OSArchText2.Text = "Loading...";
                
                if (SteamPathText2 != null)
                    SteamPathText2.Text = "Searching...";
                
                if (SteamConfigText2 != null)
                    SteamConfigText2.Text = "Searching...";
                
                if (SteamDirectoryTextBox != null)
                    SteamDirectoryTextBox.Text = "";
            }
            catch (Exception ex)
            {
                // Log but don't show error for non-critical operations
                Debug.WriteLine($"Error in SetDefaultValues: {ex.Message}");
            }
        }

        private void OnSteamDetectionChanged(bool steamInstalled)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Force a complete system info reload instead of just path update
                    Task.Run(async () => await LoadSystemInfoSafe());
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSteamDetectionChanged: {ex.Message}");
            }
        }

        private void CheckAdminPrivileges()
        {
            try
            {
                bool isAdmin = false;
                
                // Safe verification of administrator privileges
                try
                {
                    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
                catch
                {
                    isAdmin = false;
                }
                
                // Update both admin status elements
                UpdateAdminStatusElements(isAdmin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckAdminPrivileges: {ex.Message}");
                UpdateAdminStatusElements(false, "Unable to check privileges");
            }
        }

        private void UpdateAdminStatusElements(bool isAdmin, string customMessage = null)
        {
            try
            {
                string statusText = customMessage ?? (isAdmin ? "Administrator" : "Standard user");
                var textColor = isAdmin ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Orange);
                var iconKind = isAdmin ? MaterialDesignThemes.Wpf.PackIconKind.Shield : MaterialDesignThemes.Wpf.PackIconKind.ShieldAlert;
                var iconColor = isAdmin ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Orange);

                // Update first admin status (Installer tab)
                if (AdminStatusText != null && StatusIcon != null)
                {
                    AdminStatusText.Text = statusText;
                    AdminStatusText.Foreground = textColor;
                    StatusIcon.Kind = iconKind;
                    StatusIcon.Foreground = iconColor;
                }

                // Update second admin status (Uninstaller tab)
                if (AdminStatusText2 != null && StatusIcon2 != null)
                {
                    AdminStatusText2.Text = statusText;
                    AdminStatusText2.Foreground = textColor;
                    StatusIcon2.Kind = iconKind;
                    StatusIcon2.Foreground = iconColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateAdminStatusElements: {ex.Message}");
            }
        }

        private string GetFriendlyOSVersion()
        {
            try
            {
                // Try to get Windows version from registry first (more reliable)
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            var productName = key.GetValue("ProductName")?.ToString();
                            if (!string.IsNullOrEmpty(productName))
                            {
                                // Clean up the product name
                                if (productName.Contains("Windows 11"))
                                    return "Windows 11";
                                if (productName.Contains("Windows 10"))
                                    return "Windows 10";
                                if (productName.Contains("Windows 8.1"))
                                    return "Windows 8.1";
                                if (productName.Contains("Windows 8"))
                                    return "Windows 8";
                                if (productName.Contains("Windows 7"))
                                    return "Windows 7";
                                    
                                return productName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Registry detection failed: {ex.Message}");
                }

                // Fallback to version detection
                var version = Environment.OSVersion.Version;
                var platform = Environment.OSVersion.Platform;

                if (platform == PlatformID.Win32NT)
                {
                    // More accurate Windows 11 detection
                    if (version.Major == 10 && version.Build >= 22000)
                    {
                        return "Windows 11";
                    }
                    // Windows 10 (including all builds from 10240 onwards)
                    else if (version.Major == 10 && version.Build >= 10240)
                    {
                        return "Windows 10";
                    }
                    // Windows 10 initial releases
                    else if (version.Major == 10)
                    {
                        return "Windows 10";
                    }
                    // Windows 8.1
                    else if (version.Major == 6 && version.Minor == 3)
                    {
                        return "Windows 8.1";
                    }
                    // Windows 8
                    else if (version.Major == 6 && version.Minor == 2)
                    {
                        return "Windows 8";
                    }
                    // Windows 7
                    else if (version.Major == 6 && version.Minor == 1)
                    {
                        return "Windows 7";
                    }
                    // Windows Vista
                    else if (version.Major == 6 && version.Minor == 0)
                    {
                        return "Windows Vista";
                    }
                    // Fallback for other versions
                    else
                    {
                        return $"Windows {version.Major}.{version.Minor} (Build {version.Build})";
                    }
                }
                
                return Environment.OSVersion.VersionString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OS detection error: {ex.Message}");
                return Environment.OSVersion.VersionString;
            }
        }

        private async Task LoadSystemInfoSafe()
        {
            try
            {
                if (_steamService == null)
                {
                    // Basic information without service
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateSystemInfoElements("Service not available", 
                                                GetFriendlyOSVersion(),
                                                Environment.Is64BitOperatingSystem ? "x64" : "x86",
                                                "Service not available");
                    });
                    return;
                }

                var systemInfo = await _steamService.GetSystemInfoAsync().ConfigureAwait(false);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    string configStatus = systemInfo.ConfigExists ? 
                        "Optimization config found" : "No optimization config";
                    
                    UpdateSystemInfoElements(systemInfo.SteamPath, 
                                           GetFriendlyOSVersion(), 
                                           systemInfo.Architecture, 
                                           configStatus);
                    
                    UpdateSteamPathDisplay(systemInfo);
                    UpdateModeSpecificInfo();
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Debug.WriteLine($"Error in LoadSystemInfoSafe: {ex.Message}");
                    UpdateSystemInfoElements("Error loading system info", 
                                           "Error loading", 
                                           "Error loading", 
                                           "Error loading config status");
                });
            }
        }

        private void UpdateSystemInfoElements(string steamPath, string osVersion, string architecture, string configStatus)
        {
            try
            {
                // Update Installer tab system info
                if (OSVersionText != null)
                    OSVersionText.Text = osVersion;
                
                if (OSArchText != null)
                    OSArchText.Text = architecture;
                
                if (SteamPathText != null)
                    SteamPathText.Text = steamPath;
                
                if (SteamConfigText != null)
                    SteamConfigText.Text = configStatus;

                // Update Uninstaller tab system info (duplicate elements)
                if (OSVersionText2 != null)
                    OSVersionText2.Text = osVersion;
                
                if (OSArchText2 != null)
                    OSArchText2.Text = architecture;
                
                if (SteamPathText2 != null)
                    SteamPathText2.Text = steamPath;
                
                if (SteamConfigText2 != null)
                    SteamConfigText2.Text = configStatus;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateSystemInfoElements: {ex.Message}");
            }
        }

        private void UpdateSteamPathDisplay(SystemInfo systemInfo = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(_customSteamPath))
                {
                    if (SteamPathText != null)
                        SteamPathText.Text = $"Custom: {_customSteamPath}";
                    if (SteamPathText2 != null)
                        SteamPathText2.Text = $"Custom: {_customSteamPath}";
                    if (SteamDirectoryTextBox != null)
                        SteamDirectoryTextBox.Text = _customSteamPath;
                    return;
                }

                if (systemInfo?.SteamFound == true)
                {
                    if (SteamPathText != null)
                        SteamPathText.Text = systemInfo.SteamPath;
                    if (SteamPathText2 != null)
                        SteamPathText2.Text = systemInfo.SteamPath;
                    if (SteamDirectoryTextBox != null)
                        SteamDirectoryTextBox.Text = systemInfo.SteamPath;
                }
                else
                {
                    if (SteamPathText != null)
                        SteamPathText.Text = "Steam not found - Please browse manually";
                    if (SteamPathText2 != null)
                        SteamPathText2.Text = "Steam not found - Please browse manually";
                    if (SteamDirectoryTextBox != null)
                        SteamDirectoryTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateSteamPathDisplay: {ex.Message}");
            }
        }

        private void UpdateUninstallStatus()
        {
            try
            {
                if (_uninstallService == null)
                {
                    if (UninstallSteamConfigText != null)
                        UninstallSteamConfigText.Text = "Service not available";
                    if (UninstallSteamV2StatusText != null)
                        UninstallSteamV2StatusText.Text = "Service not available";
                    if (UninstallShortcutsStatusText != null)
                        UninstallShortcutsStatusText.Text = "Service not available";
                    return;
                }

                var status = _uninstallService.GetUninstallStatus();

                if (UninstallSteamConfigText != null)
                {
                    UninstallSteamConfigText.Text = status.SteamConfigStatus;
                    UninstallSteamConfigText.Foreground = status.HasOptimizationConfig ? 
                        new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Gray);
                }

                if (UninstallSteamV2StatusText != null)
                {
                    UninstallSteamV2StatusText.Text = status.SteamV2Status;
                    UninstallSteamV2StatusText.Foreground = status.HasSteamV2Directory ? 
                        new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Gray);
                }

                if (UninstallShortcutsStatusText != null)
                {
                    UninstallShortcutsStatusText.Text = status.ShortcutsStatus;
                    UninstallShortcutsStatusText.Foreground = status.HasShortcuts ? 
                        new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateUninstallStatus: {ex.Message}");
                if (UninstallSteamConfigText != null)
                    UninstallSteamConfigText.Text = "Unable to check status";
                if (UninstallSteamV2StatusText != null)
                    UninstallSteamV2StatusText.Text = "Unable to check status";
                if (UninstallShortcutsStatusText != null)
                    UninstallShortcutsStatusText.Text = "Unable to check status";
            }
        }

        private void BrowseSteamDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderDialog.Description = "Select Steam installation directory";
                folderDialog.ShowNewFolderButton = false;
                
                // Set initial directory to common Steam locations
                string[] commonPaths = { 
                    @"C:\Program Files (x86)\Steam", 
                    @"C:\Steam", 
                    @"D:\Steam" 
                };
                
                foreach (string path in commonPaths)
                {
                    if (Directory.Exists(path))
                    {
                        folderDialog.SelectedPath = path;
                        break;
                    }
                }

                var result = folderDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    
                    // Validate that this is a Steam directory
                    if (IsValidSteamDirectory(selectedPath))
                    {
                        _customSteamPath = selectedPath;
                        if (SteamDirectoryTextBox != null)
                            SteamDirectoryTextBox.Text = selectedPath;
                        if (SteamPathText != null)
                            SteamPathText.Text = $"Custom: {selectedPath}";
                        if (SteamPathText2 != null)
                            SteamPathText2.Text = $"Custom: {selectedPath}";
                        
                        // Update config status
                        bool configExists = File.Exists(Path.Combine(selectedPath, "steam.cfg"));
                        if (SteamConfigText != null && SteamConfigText2 != null)
                        {
                            string configStatus = "No configuration file";
                            if (configExists)
                            {
                                try
                                {
                                    var configContent = File.ReadAllText(Path.Combine(selectedPath, "steam.cfg"));
                                    bool isOptimizationConfig = configContent.Contains("BootStrapperInhibitAll=enable") || 
                                                              configContent.Contains("Mode:");
                                    configStatus = isOptimizationConfig ? "Optimization config found" : "Standard config found";
                                }
                                catch
                                {
                                    configStatus = "Config file found (unable to read)";
                                }
                            }
                            SteamConfigText.Text = configStatus;
                            SteamConfigText2.Text = configStatus;
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "The selected directory does not appear to be a valid Steam installation.\n\n" +
                            "Please make sure you select the folder containing 'steam.exe'.",
                            "Invalid Steam Directory",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error browsing for Steam directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidSteamDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    return false;
                    
                return File.Exists(Path.Combine(path, "steam.exe"));
            }
            catch
            {
                return false;
            }
        }

        private string GetValidSteamPath()
        {
            try
            {
                // Prioritize custom path if set and valid
                if (!string.IsNullOrEmpty(_customSteamPath) && IsValidSteamDirectory(_customSteamPath))
                    return _customSteamPath;
                
                // Try textbox path
                string textboxPath = SteamDirectoryTextBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(textboxPath) && IsValidSteamDirectory(textboxPath))
                    return textboxPath;
                
                // Try service detection
                if (_steamService != null)
                    return _steamService.GetSteamPath();
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateModeSpecificInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ModeComboBox_SelectionChanged: {ex.Message}");
            }
        }

        private void UpdateModeSpecificInfo()
        {
            try
            {
                if (ModeComboBox?.SelectedItem is ComboBoxItem selected && selected.Tag != null)
                {
                    string mode = selected.Tag.ToString() ?? "";
                    
                    if (mode == "NormalBoth2022-2025")
                    {
                        // Show Steam V2 info in both tabs
                        if (SteamV2Label != null)
                            SteamV2Label.Visibility = Visibility.Visible;
                        if (SteamV2PathText != null)
                        {
                            SteamV2PathText.Visibility = Visibility.Visible;
                            SteamV2PathText.Text = "Will be installed at: " + @"C:\Program Files (x86)\Steamv2";
                        }
                        
                        if (SteamV2Label2 != null)
                            SteamV2Label2.Visibility = Visibility.Visible;
                        if (SteamV2PathText2 != null)
                        {
                            SteamV2PathText2.Visibility = Visibility.Visible;
                            SteamV2PathText2.Text = "Will be installed at: " + @"C:\Program Files (x86)\Steamv2";
                        }
                        
                        if (UpdateSteam != null)
                        {
                            UpdateSteam.Content = "Automatic update (both versions)";
                            UpdateSteam.IsEnabled = false;
                            UpdateSteam.IsChecked = true;
                        }
                    }
                    else
                    {
                        // Hide Steam V2 info in both tabs
                        if (SteamV2Label != null)
                            SteamV2Label.Visibility = Visibility.Collapsed;
                        if (SteamV2PathText != null)
                            SteamV2PathText.Visibility = Visibility.Collapsed;
                        
                        if (SteamV2Label2 != null)
                            SteamV2Label2.Visibility = Visibility.Collapsed;
                        if (SteamV2PathText2 != null)
                            SteamV2PathText2.Visibility = Visibility.Collapsed;
                        
                        if (UpdateSteam != null)
                        {
                            UpdateSteam.IsEnabled = true;
                            UpdateSteam.Content = "Update Steam automatically";
                            UpdateSteam.IsChecked = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateModeSpecificInfo: {ex.Message}");
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                // Check if we have a valid Steam path
                string steamPath = GetValidSteamPath();
                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    MessageBox.Show(
                        "Steam installation not found or invalid.\n\n" +
                        "Please browse for your Steam directory manually or install Steam from:\n" +
                        "https://store.steampowered.com/about/\n\n" +
                        "Make sure to select the folder containing 'steam.exe'.",
                        "Steam Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (_steamService?.IsRunningAsAdmin() != true)
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

                await StartOptimization(steamPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting optimization: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to uninstall Steam Debloat?\n\n" +
                    "This will remove optimization configurations and restore Steam to its original state.\n" +
                    "Your games and personal settings will be preserved.",
                    "Confirm Uninstallation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                if (_uninstallService?.IsRunningAsAdmin() != true)
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

                await StartUninstall().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting uninstallation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartOptimization(string steamPath)
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                Dispatcher.Invoke(() =>
                {
                    ShowProgressOverlay(true);
                    if (StartButton != null)
                        StartButton.IsEnabled = false;
                    if (UninstallButton != null)
                        UninstallButton.IsEnabled = false;
                });

                var config = GetOptimizationConfig(steamPath);
                
                var result = await _steamService.OptimizeSteamAsync(config, _cancellationTokenSource.Token).ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
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
                });
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Operation cancelled by user.", 
                                  "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
                Dispatcher.Invoke(() =>
                {
                    ShowProgressOverlay(false);
                    if (StartButton != null)
                        StartButton.IsEnabled = true;
                    if (UninstallButton != null)
                        UninstallButton.IsEnabled = true;
                });
                
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                
                // Reload system info and uninstall status after operation
                await LoadSystemInfoSafe().ConfigureAwait(false);
                Dispatcher.Invoke(() => UpdateUninstallStatus());
            }
        }

        private async Task StartUninstall()
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                Dispatcher.Invoke(() =>
                {
                    ShowProgressOverlay(true);
                    if (StartButton != null)
                        StartButton.IsEnabled = false;
                    if (UninstallButton != null)
                        UninstallButton.IsEnabled = false;
                });

                var result = await _uninstallService.UninstallAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
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
                });
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Operation cancelled by user.", 
                                  "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
                Dispatcher.Invoke(() =>
                {
                    ShowProgressOverlay(false);
                    if (StartButton != null)
                        StartButton.IsEnabled = true;
                    if (UninstallButton != null)
                        UninstallButton.IsEnabled = true;
                });
                
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                
                // Reload system info and uninstall status after operation
                await LoadSystemInfoSafe().ConfigureAwait(false);
                Dispatcher.Invoke(() => UpdateUninstallStatus());
            }
        }

        private OptimizationConfig GetOptimizationConfig(string steamPath)
        {
            try
            {
                var selectedItem = (ComboBoxItem)ModeComboBox?.SelectedItem;
                
                return new OptimizationConfig
                {
                    Mode = selectedItem?.Tag?.ToString() ?? "Normal2025July",
                    CreateDesktopShortcut = CreateDesktopShortcut?.IsChecked == true,
                    CreateStartMenuShortcut = CreateStartMenuShortcut?.IsChecked == true,
                    RemoveFromStartup = RemoveFromStartup?.IsChecked == true,
                    UpdateSteam = UpdateSteam?.IsChecked == true && UpdateSteam?.IsEnabled == true,
                    SteamPath = steamPath
                };
            }
            catch
            {
                return new OptimizationConfig { SteamPath = steamPath };
            }
        }

        private void ShowProgressOverlay(bool show)
        {
            try
            {
                if (ProgressOverlay != null)
                    ProgressOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowProgressOverlay: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CancelButton_Click: {ex.Message}");
            }
        }

        private void OnProgressChanged(string status)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ProgressText != null)
                        ProgressText.Text = status;
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnProgressChanged: {ex.Message}");
            }
        }

        private void RestartAsAdmin()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var mainModule = currentProcess.MainModule;
                var fileName = mainModule?.FileName ?? "";
                
                if (string.IsNullOrEmpty(fileName))
                {
                    MessageBox.Show("Could not determine application path for restart.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = fileName,
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
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                
                if (_steamService != null)
                {
                    _steamService.Dispose();
                }

                if (_uninstallService != null)
                {
                    _uninstallService.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnClosed: {ex.Message}");
            }
            
            base.OnClosed(e);
        }
    }
}
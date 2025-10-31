using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace SteamDebloat
{
    public partial class MainWindow : Window
    {
        private SteamDebloatService _steamService;
        private UninstallService _uninstallService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isProcessing = false;
        private string _customSteamPath = string.Empty;
        private string _selectedMode = "Normal2023June";
        private string _selectedModeDisplay = "Normal 2023 June (Recommended)";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show Millennium warning
            await Task.Delay(300); // Small pause for window rendering
            
            var warningWindow = new MillenniumWarningWindow
            {
                Owner = this
            };
            
            bool? result = warningWindow.ShowDialog();
            
            if (result != true)
            {
                // User cancelled or closed without accepting
                Application.Current.Shutdown();
                return;
            }
            
            // User accepted, continue with initialization
            InitializeBasicUI();
            InitializeModeButtons();
            await InitializeServicesAsync();
        }

        private void InitializeModeButtons()
        {
            try
            {
                StableVersionsPanel.Children.Clear();
                ExperimentalVersionsPanel.Children.Clear();

                var stableVersions = new[]
                {
                    new { Display = "Normal 2023 June\n(Recommended)", Tag = "Normal2023June", Recommended = true },
                    new { Display = "Normal 2025 October", Tag = "Normal2025October", Recommended = false },
                    new { Display = "Normal 2022 Dec", Tag = "Normal2022dec", Recommended = false },
                    new { Display = "Lite 2022 Dec", Tag = "Lite2022dec", Recommended = false }
                };

                foreach (var version in stableVersions)
                {
                    var button = CreateVersionButton(version.Display, version.Tag, version.Recommended);
                    StableVersionsPanel.Children.Add(button);
                }

                var experimentalVersions = new[]
                {
                    new { Date = "29Apr2023", Wayback = "20230429120402", Mode = "Experimental_Normal2023April" },
                    new { Date = "31May2023", Wayback = "20230531113527", Mode = "Experimental_Normal2023May" },
                    new { Date = "11Jul2023", Wayback = "20230711162631", Mode = "Experimental_Normal2023July" },
                    new { Date = "01Aug2023", Wayback = "20230801221717", Mode = "Experimental_Normal2023August" },
                    new { Date = "30Sep2023", Wayback = "20230930002005", Mode = "Experimental_Normal2023September" },
                    new { Date = "31Oct2023", Wayback = "20231031200154", Mode = "Experimental_Normal2023October" },
                    new { Date = "30Nov2023", Wayback = "20231130095245", Mode = "Experimental_Normal2023November" },
                    new { Date = "12Dec2023", Wayback = "20231212190321", Mode = "Experimental_Normal2023December" },
                    new { Date = "13Jan2024", Wayback = "20240113112425", Mode = "Experimental_Normal2024January" },
                    new { Date = "29Feb2024", Wayback = "20240229082406", Mode = "Experimental_Normal2024February" },
                    new { Date = "08Mar2024", Wayback = "20240308104109", Mode = "Experimental_Normal2024March" },
                    new { Date = "21May2024", Wayback = "20240521073345", Mode = "Experimental_Normal2024May" },
                    new { Date = "21Jun2024", Wayback = "20240621083816", Mode = "Experimental_Normal2024June" },
                    new { Date = "17Jul2024", Wayback = "20240717082107", Mode = "Experimental_Normal2024July" },
                    new { Date = "18Sep2024", Wayback = "20240918104445", Mode = "Experimental_Normal2024September" },
                    new { Date = "13Nov2024", Wayback = "20241113093224", Mode = "Experimental_Normal2024November" },
                    new { Date = "04Dec2024", Wayback = "20241204072114", Mode = "Experimental_Normal2024December" },
                    new { Date = "29Jan2025", Wayback = "20250129125321", Mode = "Experimental_Normal2025January" },
                    new { Date = "11Mar2025", Wayback = "20250311093241", Mode = "Experimental_Normal2025March" },
                    new { Date = "29Apr2025", Wayback = "20250429101123", Mode = "Experimental_Normal2025April" },
                    new { Date = "21May2025", Wayback = "20250521085614", Mode = "Experimental_Normal2025May" },
                    new { Date = "01Jul2025", Wayback = "20250701090002", Mode = "Experimental_Normal2025July" },
                    new { Date = "10Sep2025", Wayback = "20250910074132", Mode = "Experimental_Normal2025September" },
                    new { Date = "06Oct2025", Wayback = "20251006072943", Mode = "Experimental_Normal2025October" }
                };

                for (int i = 0; i < experimentalVersions.Length; i++)
                {
                    var version = experimentalVersions[i];
                    var button = CreateExperimentalButton(version.Date, version.Mode, version.Wayback);
                    ExperimentalVersionsPanel.Children.Add(button);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing mode buttons: {ex.Message}");
            }
        }

        private Button CreateVersionButton(string display, string tag, bool recommended)
        {
            try
            {
                var button = new Button
                {
                    Width = 190,
                    Height = 92,
                    Tag = tag,
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(44, 44, 46)),
                    BorderBrush = recommended ? 
                        new SolidColorBrush(Color.FromRgb(10, 132, 255)) : 
                        new SolidColorBrush(Color.FromRgb(72, 72, 74))
                };
                
                button.Margin = new Thickness(8, 8, 8, 8);
                button.BorderThickness = new Thickness(1.5, 1.5, 1.5, 1.5);

                var border = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(14, 14, 14, 14)
                };
                
                border.CornerRadius = new CornerRadius(8);

                var stackPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                if (recommended)
                {
                    var badge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    
                    badge.CornerRadius = new CornerRadius(3);
                    badge.Padding = new Thickness(7, 3, 7, 3);
                    badge.Margin = new Thickness(0, 0, 0, 7);
                    
                    var badgeText = new TextBlock
                    {
                        Text = "RECOMMENDED",
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    
                    badge.Child = badgeText;
                    stackPanel.Children.Add(badge);
                }

                var textBlock = new TextBlock
                {
                    Text = display,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanel.Children.Add(textBlock);
                border.Child = stackPanel;
                button.Content = border;

                // Hover effect
                button.MouseEnter += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(58, 58, 60));
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(10, 132, 255));
                };

                button.MouseLeave += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(44, 44, 46));
                    button.BorderBrush = recommended ? 
                        new SolidColorBrush(Color.FromRgb(10, 132, 255)) : 
                        new SolidColorBrush(Color.FromRgb(72, 72, 74));
                };

                button.Click += VersionButton_Click;

                return button;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating version button: {ex.Message}");
                throw;
            }
        }

        private Button CreateExperimentalButton(string dateDisplay, string tag, string waybackDate)
        {
            try
            {
                // Same design as stable buttons
                var button = new Button
                {
                    Width = 130,
                    Height= 84,
                    Tag = new { Mode = tag, Wayback = waybackDate },
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(44, 44, 46)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 74))
                };
                
                button.Margin = new Thickness(6, 6, 6, 6);
                button.BorderThickness = new Thickness(1.5, 1.5, 1.5, 1.5);

                var border = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(10, 10, 10, 10)
                };
                
                border.CornerRadius = new CornerRadius(8);

                var stackPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // BETA badge (like RECOMMENDED but orange)
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 159, 10)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                badge.CornerRadius = new CornerRadius(3);
                badge.Padding = new Thickness(6, 3, 6, 3);
                badge.Margin = new Thickness(0, 0, 0, 5);
                
                var badgeText = new TextBlock
                {
                    Text = "BETA",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                
                badge.Child = badgeText;
                stackPanel.Children.Add(badge);

                var dateText = new TextBlock
                {
                    Text = dateDisplay,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanel.Children.Add(dateText);
                border.Child = stackPanel;
                button.Content = border;

                // Same hover as stable buttons
                button.MouseEnter += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(58, 58, 60));
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 159, 10));
                };

                button.MouseLeave += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(44, 44, 46));
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 74));
                };

                button.Click += ExperimentalVersionButton_Click;

                return button;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating experimental button: {ex.Message}");
                throw;
            }
        }

        private void ModeSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowModeSelectionOverlay();
        }

        private void ShowModeSelectionOverlay()
        {
            ModeSelectionOverlay.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ModeSelectionOverlay.Visibility = Visibility.Collapsed;
        }

        private void VersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _selectedMode = tag;
                
                string displayText;
                if (tag == "Normal2023June")
                    displayText = "Normal 2023 June (Recommended)";
                else if (tag == "Normal2025October")
                    displayText = "Normal 2025 October";
                else if (tag == "Normal2022dec")
                    displayText = "Normal 2022 Dec";
                else if (tag == "Lite2022dec")
                    displayText = "Lite 2022 Dec";
                else
                    displayText = tag;

                _selectedModeDisplay = displayText;
                ModeSelectionButton.Content = displayText;
                
                ModeSelectionOverlay.Visibility = Visibility.Collapsed;
                UpdateModeSpecificInfo();
            }
        }

        private void ExperimentalVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                dynamic tagData = button.Tag;
                string mode = tagData.Mode;
                string wayback = tagData.Wayback;
                
                _selectedMode = $"Experimental_{wayback}";
                
                var border = button.Content as Border;
                var stackPanel = border?.Child as StackPanel;
                var dateText = stackPanel?.Children[1] as TextBlock;
                string displayDate = dateText?.Text ?? mode;
                
                _selectedModeDisplay = $"Experimental {displayDate}";
                ModeSelectionButton.Content = _selectedModeDisplay;
                
                ModeSelectionOverlay.Visibility = Visibility.Collapsed;
                UpdateModeSpecificInfo();
            }
        }

        private void InitializeBasicUI()
        {
            try
            {
                CheckAdminPrivileges();
                SetDefaultValues();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeBasicUI: {ex.Message}");
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    _steamService = new SteamDebloatService();
                    _steamService.ProgressChanged += OnProgressChanged;
                    _steamService.SteamDetectionChanged += OnSteamDetectionChanged;

                    _uninstallService = new UninstallService();
                    _uninstallService.ProgressChanged += OnProgressChanged;
                });

                await LoadSystemInfoSafe();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeServicesAsync: {ex.Message}");
            }
        }

        private void SetDefaultValues()
        {
            try
            {
                OSVersionText.Text = "Loading...";
                OSArchText.Text = "Loading...";
                SteamPathText.Text = "Searching...";
                SteamConfigText.Text = "Searching...";

                SteamDirectoryTextBox.Text = "";
            }
            catch { }
        }

        private void OnSteamDetectionChanged(bool steamInstalled)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                await LoadSystemInfoSafe();
            });
        }

        private void CheckAdminPrivileges()
        {
            try
            {
                bool isAdmin = false;
                string statusMessage = "";

                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                statusMessage = isAdmin ? "Administrator" : "Standard user (will request elevation)";

                UpdateAdminStatusElements(isAdmin, statusMessage);
            }
            catch
            {
                UpdateAdminStatusElements(false, "Unable to check privileges");
            }
        }

        private void UpdateAdminStatusElements(bool isAdmin, string customMessage = null)
        {
            try
            {
                string statusText = customMessage ?? (isAdmin ? "Administrator" : "Standard user");
                var textColor = isAdmin ? Brushes.Green : Brushes.Orange;

                AdminStatusText.Text = statusText;
                AdminStatusText.Foreground = textColor;
            }
            catch { }
        }

        private async Task LoadSystemInfoSafe()
        {
            try
            {
                if (_steamService == null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateSystemInfoElements("Service not available",
                                                 "Loading...",
                                                 "Loading...",
                                                 "Service not available");
                    });
                    return;
                }

                var systemInfo = await _steamService.GetSystemInfoAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    string configStatus = systemInfo.ConfigExists ?
                    "Optimization config found" : "No optimization config";

                    UpdateSystemInfoElements(systemInfo.SteamPath,
                                             systemInfo.OSVersion,
                                             systemInfo.Architecture,
                                             configStatus);

                    UpdateSteamPathDisplay(systemInfo);
                    UpdateModeSpecificInfo();
                });
            }
            catch
            {
                await Dispatcher.InvokeAsync(() =>
                {
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
                OSVersionText.Text = osVersion;
                OSArchText.Text = architecture;
                SteamPathText.Text = steamPath;
                SteamConfigText.Text = configStatus;
            }
            catch { }
        }

        private void UpdateSteamPathDisplay(SystemInfo systemInfo = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(_customSteamPath))
                {
                    SteamPathText.Text = $"Custom: {_customSteamPath}";
                    SteamDirectoryTextBox.Text = _customSteamPath;
                    return;
                }

                if (systemInfo?.SteamFound == true)
                {
                    SteamPathText.Text = systemInfo.SteamPath;
                    SteamDirectoryTextBox.Text = systemInfo.SteamPath;
                }
                else
                {
                    SteamPathText.Text = "Steam not found - Please browse manually";
                    SteamDirectoryTextBox.Text = "";
                }
            }
            catch { }
        }

        private void BrowseSteamDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select Steam installation directory",
                    ShowNewFolderButton = false
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedPath = dialog.SelectedPath;

                    if (IsValidSteamDirectory(selectedPath))
                    {
                        _customSteamPath = selectedPath;
                        SteamDirectoryTextBox.Text = selectedPath;
                        SteamPathText.Text = $"Custom: {selectedPath}";

                        bool configExists = File.Exists(Path.Combine(selectedPath, "steam.cfg"));
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
                    }
                    else
                    {
                        MessageBox.Show(
                            "The selected directory does not appear to be a valid Steam installation.\n\n" +
                            "Please make sure you select the folder containing the Steam executable.",
                            "Invalid Steam Directory",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error browsing for Steam directory: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (!string.IsNullOrEmpty(_customSteamPath) && IsValidSteamDirectory(_customSteamPath))
                    return _customSteamPath;

                string textboxPath = SteamDirectoryTextBox.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(textboxPath) && IsValidSteamDirectory(textboxPath))
                    return textboxPath;

                if (_steamService != null)
                    return _steamService.GetSteamPath();

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void UpdateModeSpecificInfo()
        {
            try
            {
                UpdateSteam.IsEnabled = true;
                UpdateSteam.Content = "Update Steam automatically";
                UpdateSteam.IsChecked = true;
            }
            catch { }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                string steamPath = GetValidSteamPath();
                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    MessageBox.Show(
                        "Steam installation not found or invalid.\n\n" +
                        "Please browse for your Steam directory manually or install Steam first.\n\n" +
                        "Make sure to select the folder containing the Steam executable.",
                        "Steam Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (_steamService?.IsRunningAsAdmin() != true)
                {
                    var result = MessageBox.Show(
                        "This application requires administrator privileges to function correctly.\n\n" +
                        "Do you want to restart it as administrator?",
                        "Administrator Privileges",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        RestartAsAdmin();
                    }
                    return;
                }

                await StartOptimization(steamPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting optimization: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                // Show modern confirmation dialog
                bool confirmed = await ShowConfirmationOverlay(
                    "Confirm Restore",
                    "Are you sure you want to restore Steam to its original state?\n\n" +
                    "This will remove optimization configurations and restore Steam to the latest version.\n" +
                    "Your games and personal settings will be preserved.");

                if (!confirmed)
                    return;

                if (_uninstallService?.IsRunningAsAdmin() != true)
                {
                    var adminResult = MessageBox.Show(
                        "Administrator privileges are required for restoration.\n\n" +
                        "Do you want to restart with elevated privileges?",
                        "Administrator Privileges",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (adminResult == MessageBoxResult.Yes)
                    {
                        RestartAsAdmin();
                    }
                    return;
                }

                await StartUninstall();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting restoration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartOptimization(string steamPath)
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                await Dispatcher.InvokeAsync(() =>
                {
                    ShowProgressOverlay(true);
                    StartButton.IsEnabled = false;
                    UninstallButton.IsEnabled = false;
                });

                var config = GetOptimizationConfig(steamPath);

                var result = await _steamService.OptimizeSteamAsync(config, _cancellationTokenSource.Token);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        ShowCompletionOverlay($"Optimization completed successfully in {result.Duration:mm\\:ss}");
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
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Operation cancelled by user.", "Cancelled",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowProgressOverlay(false);
                    StartButton.IsEnabled = true;
                    UninstallButton.IsEnabled = true;
                });

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                await LoadSystemInfoSafe();
            }
        }

        private async Task StartUninstall()
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                await Dispatcher.InvokeAsync(() =>
                {
                    ShowProgressOverlay(true);
                    StartButton.IsEnabled = false;
                    UninstallButton.IsEnabled = false;
                });

                var result = await _uninstallService.UninstallAsync(_cancellationTokenSource.Token);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        ShowCompletionOverlay("Restoration completed successfully.");
                    }
                    else
                    {
                        MessageBox.Show($"Error during restoration:\n{result.ErrorMessage}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Operation cancelled by user.", "Cancelled",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowProgressOverlay(false);
                    StartButton.IsEnabled = true;
                    UninstallButton.IsEnabled = true;
                });

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                await LoadSystemInfoSafe();
            }
        }

        private OptimizationConfig GetOptimizationConfig(string steamPath)
        {
            try
            {
                return new OptimizationConfig
                {
                    Mode = _selectedMode,
                    RemoveFromStartup = RemoveFromStartup.IsChecked == true,
                    UpdateSteam = UpdateSteam.IsChecked == true && UpdateSteam.IsEnabled == true,
                    SteamPath = steamPath
                };
            }
            catch
            {
                return new OptimizationConfig
                {
                    Mode = "Normal2023June",
                    SteamPath = steamPath
                };
            }
        }

        private void ShowProgressOverlay(bool show)
        {
            try
            {
                ProgressOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void ShowCompletionOverlay(string message)
        {
            try
            {
                CompletionMessageText.Text = message;
                CompletionOverlay.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void CompletionOkButton_Click(object sender, RoutedEventArgs e)
        {
            CompletionOverlay.Visibility = Visibility.Collapsed;
        }

        private Task<bool> ShowConfirmationOverlay(string title, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                ConfirmationTitleText.Text = title;
                ConfirmationMessageText.Text = message;
                ConfirmationOverlay.Visibility = Visibility.Visible;

                RoutedEventHandler yesHandler = null;
                RoutedEventHandler noHandler = null;

                yesHandler = (s, e) =>
                {
                    ConfirmationYesButton.Click -= yesHandler;
                    ConfirmationNoButton.Click -= noHandler;
                    ConfirmationOverlay.Visibility = Visibility.Collapsed;
                    tcs.TrySetResult(true);
                };

                noHandler = (s, e) =>
                {
                    ConfirmationYesButton.Click -= yesHandler;
                    ConfirmationNoButton.Click -= noHandler;
                    ConfirmationOverlay.Visibility = Visibility.Collapsed;
                    tcs.TrySetResult(false);
                };

                ConfirmationYesButton.Click += yesHandler;
                ConfirmationNoButton.Click += noHandler;
            }
            catch
            {
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch { }
        }

        private void OnProgressChanged(string status)
        {
            try
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ProgressText.Text = status;
                });
            }
            catch { }
        }

        private void RestartAsAdmin()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var fileName = currentProcess.MainModule?.FileName ?? "";

                if (string.IsNullOrEmpty(fileName))
                {
                    MessageBox.Show("Could not determine application path for restart.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Could not restart with elevated privileges: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                if (_steamService != null)
                {
                    _steamService.ProgressChanged -= OnProgressChanged;
                    _steamService.SteamDetectionChanged -= OnSteamDetectionChanged;
                    _steamService.Dispose();
                    _steamService = null;
                }

                if (_uninstallService != null)
                {
                    _uninstallService.ProgressChanged -= OnProgressChanged;
                    _uninstallService.Dispose();
                    _uninstallService = null;
                }
            }
            catch { }

            base.OnClosed(e);
        }
    }
}
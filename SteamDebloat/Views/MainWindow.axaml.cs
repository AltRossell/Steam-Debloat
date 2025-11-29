using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private Timer _validationTimer;

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Initialize();
            this.Opened += MainWindow_Opened;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void MainWindow_Opened(object sender, EventArgs e)
        {
            await Task.Delay(300);
            
            string steamPath = FindInitialSteamPath();
            bool hasMillennium = false;
            
            if (!string.IsNullOrEmpty(steamPath))
            {
                hasMillennium = CheckForMillennium(steamPath);
            }

            bool userAccepted = false;

            // Show Millennium warning if millennium.dll is detected
            if (hasMillennium)
            {
                var millenniumWindow = new MillenniumWarningWindow();
                var downgradeWindow = new DowngradeWarningWindow();
                
                downgradeWindow.ShowBackButton(true);
                
                millenniumWindow.NavigateNext += async (s, args) =>
                {
                    var result = await downgradeWindow.ShowDialog<bool?>(this);
                    
                    if (result == true)
                    {
                        userAccepted = true;
                        millenniumWindow.Close();
                    }
                    else
                    {
                        await millenniumWindow.ShowDialog(this);
                    }
                };
                
                downgradeWindow.NavigateBack += async (s, args) =>
                {
                    downgradeWindow.Hide();
                    await millenniumWindow.ShowDialog(this);
                };
                
                bool? millenniumResult = await millenniumWindow.ShowDialog<bool?>(this);
                
                if (millenniumResult != true && !userAccepted)
                {
                    Close();
                    return;
                }
            }
            else
            {
                var downgradeWindow = new DowngradeWarningWindow();
                downgradeWindow.ShowBackButton(false);
                
                bool? result = await downgradeWindow.ShowDialog<bool?>(this);
                
                if (result != true)
                {
                    Close();
                    return;
                }
            }
            
            InitializeBasicUI();
            InitializeModeComboBox();
            await InitializeServicesAsync();
            StartValidationTimer();
        }

        private string FindInitialSteamPath()
        {
            try
            {
                var commonPaths = new[]
                {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Steam",
                    @"D:\Steam",
                    @"E:\Steam"
                };

                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                        return path;
                }

                try
                {
                    string[] registryPaths = {
                        @"SOFTWARE\WOW6432Node\Valve\Steam",
                        @"SOFTWARE\Valve\Steam"
                    };

                    foreach (var regPath in registryPaths)
                    {
                        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath))
                        {
                            if (key != null)
                            {
                                var installPathObj = key.GetValue("InstallPath");
                                if (installPathObj != null)
                                {
                                    string installPath = installPathObj.ToString();
                                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                                        return installPath;
                                }
                            }
                        }
                    }
                }
                catch { }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool CheckForMillennium(string steamPath)
        {
            try
            {
                string millenniumDll = Path.Combine(steamPath, "ext", "millennium.dll");
                string millenniumDll2 = Path.Combine(steamPath, "millennium.dll");
                
                return File.Exists(millenniumDll) || File.Exists(millenniumDll2);
            }
            catch
            {
                return false;
            }
        }

        private void InitializeBasicUI()
        {
            try
            {
                var osVersionText = this.FindControl<TextBlock>("OSVersionText");
                var osArchText = this.FindControl<TextBlock>("OSArchText");
                var adminStatusText = this.FindControl<TextBlock>("AdminStatusText");

                if (osVersionText != null) osVersionText.Text = "Loading...";
                if (osArchText != null) osArchText.Text = "Loading...";
                if (adminStatusText != null) adminStatusText.Text = "Checking...";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in InitializeBasicUI: " + ex.Message);
            }
        }

        private void InitializeModeComboBox()
        {
            try
            {
                var modeComboBox = this.FindControl<ComboBox>("ModeComboBox");
                if (modeComboBox == null) return;

                modeComboBox.Items.Clear();

                // Stable Versions Header
                var stableHeader = new ComboBoxItem
                {
                    Content = "━━━ Stable Versions ━━━",
                    IsEnabled = false,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237))
                };
                modeComboBox.Items.Add(stableHeader);

                // Stable versions
                modeComboBox.Items.Add(new ComboBoxItem { Content = "Normal 2023 June (Recommended)", Tag = "Normal2023June" });
                modeComboBox.Items.Add(new ComboBoxItem { Content = "Normal 2025 November", Tag = "Normal2025November" });
                modeComboBox.Items.Add(new ComboBoxItem { Content = "Normal 2022 Dec", Tag = "Normal2022dec" });
                modeComboBox.Items.Add(new ComboBoxItem { Content = "Lite 2022 Dec", Tag = "Lite2022dec" });

                // Experimental Versions Header
                var experimentalHeader = new ComboBoxItem
                {
                    Content = "━━━ Experimental Versions ━━━",
                    IsEnabled = false,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0))
                };
                modeComboBox.Items.Add(experimentalHeader);

                // Experimental versions with Wayback Machine timestamps
                var experimentalVersions = new[]
                {
                    new { Display = "Experimental - 29 Apr 2023", Wayback = "20230429120402" },
                    new { Display = "Experimental - 31 May 2023", Wayback = "20230531113527" },
                    new { Display = "Experimental - 11 Jul 2023", Wayback = "20230711162631" },
                    new { Display = "Experimental - 01 Aug 2023", Wayback = "20230801221717" },
                    new { Display = "Experimental - 30 Sep 2023", Wayback = "20230930002005" },
                    new { Display = "Experimental - 31 Oct 2023", Wayback = "20231031200154" },
                    new { Display = "Experimental - 30 Nov 2023", Wayback = "20231130095245" },
                    new { Display = "Experimental - 12 Dec 2023", Wayback = "20231212190321" },
                    new { Display = "Experimental - 13 Jan 2024", Wayback = "20240113112425" },
                    new { Display = "Experimental - 29 Feb 2024", Wayback = "20240229082406" },
                    new { Display = "Experimental - 08 Mar 2024", Wayback = "20240308104109" },
                    new { Display = "Experimental - 21 May 2024", Wayback = "20240521073345" },
                    new { Display = "Experimental - 21 Jun 2024", Wayback = "20240621083816" },
                    new { Display = "Experimental - 17 Jul 2024", Wayback = "20240717082107" },
                    new { Display = "Experimental - 18 Sep 2024", Wayback = "20240918104445" },
                    new { Display = "Experimental - 13 Nov 2024", Wayback = "20241113093224" },
                    new { Display = "Experimental - 04 Dec 2024", Wayback = "20241204072114" },
                    new { Display = "Experimental - 29 Jan 2025", Wayback = "20250129125321" },
                    new { Display = "Experimental - 11 Mar 2025", Wayback = "20250311093241" },
                    new { Display = "Experimental - 29 Apr 2025", Wayback = "20250429101123" },
                    new { Display = "Experimental - 21 May 2025", Wayback = "20250521085614" },
                    new { Display = "Experimental - 01 Jul 2025", Wayback = "20250701090002" },
                    new { Display = "Experimental - 10 Sep 2025", Wayback = "20250910074132" },
                    new { Display = "Experimental - 06 Oct 2025", Wayback = "20251006072943" },
                    new { Display = "Experimental - 18 Nov 2025", Wayback = "20251118083007" }
                };

                foreach (var version in experimentalVersions)
                {
                    modeComboBox.Items.Add(new ComboBoxItem { Content = version.Display, Tag = "Experimental_" + version.Wayback });
                }

                modeComboBox.SelectedIndex = 1; // Select "Normal 2023 June (Recommended)"
                modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing mode combo box: " + ex.Message);
            }
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var modeComboBox = sender as ComboBox;
            if (modeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _selectedMode = tag;
                // UpdateModeSpecificInfo(); // Not critical for now
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _steamService = new SteamDebloatService();
                    _steamService.SteamDetectionChanged += OnSteamDetectionChanged;
                    _uninstallService = new UninstallService();
                });

                await LoadSystemInfoSafe();
                CheckAdminPrivileges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in InitializeServicesAsync: " + ex.Message);
            }
        }

        private void OnSteamDetectionChanged(bool steamInstalled)
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await LoadSystemInfoSafe();
            });
        }

        private async Task LoadSystemInfoSafe()
        {
            try
            {
                var osVersionText = this.FindControl<TextBlock>("OSVersionText");
                var osArchText = this.FindControl<TextBlock>("OSArchText");
                var steamDirectoryTextBox = this.FindControl<TextBox>("SteamDirectoryTextBox");

                if (_steamService == null)
                {
                    if (osVersionText != null) osVersionText.Text = "Service unavailable";
                    return;
                }

                var systemInfo = await _steamService.GetSystemInfoAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (osVersionText != null) osVersionText.Text = systemInfo.OSVersion;
                    if (osArchText != null) osArchText.Text = systemInfo.Architecture;

                    if (steamDirectoryTextBox != null && string.IsNullOrEmpty(steamDirectoryTextBox.Text) && systemInfo.SteamFound)
                    {
                        steamDirectoryTextBox.Text = systemInfo.SteamPath;
                    }
                });
            }
            catch
            {
                // Ignore errors
            }
        }

        private void CheckAdminPrivileges()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                var adminStatusText = this.FindControl<TextBlock>("AdminStatusText");
                if (adminStatusText != null)
                {
                    adminStatusText.Text = isAdmin ? "Admin" : "User";
                    adminStatusText.Foreground = isAdmin ? Brushes.LightGreen : Brushes.Orange;
                }
            }
            catch { }
        }

        private void StartValidationTimer()
        {
            _validationTimer = new Timer(ValidateSteamPath, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        private void ValidateSteamPath(object state)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                int processCount = GetSteamProcessCount();
                var steamProcessPanel = this.FindControl<StackPanel>("SteamProcessPanel");
                var steamProcessCountText = this.FindControl<TextBlock>("SteamProcessCountText");

                if (processCount > 0)
                {
                    if (steamProcessPanel != null) steamProcessPanel.IsVisible = true;
                    if (steamProcessCountText != null) steamProcessCountText.Text = $"{processCount} Process{(processCount == 1 ? "" : "es")}";
                }
                else
                {
                    if (steamProcessPanel != null) steamProcessPanel.IsVisible = false;
                }

                var steamDirectoryTextBox = this.FindControl<TextBox>("SteamDirectoryTextBox");
                var path = steamDirectoryTextBox?.Text?.Trim();
                
                var steamStatusText = this.FindControl<TextBlock>("SteamStatusText");
                var steamStatusIcon = this.FindControl<FluentAvalonia.UI.Controls.SymbolIcon>("SteamStatusIcon");
                var installSteamButton = this.FindControl<Button>("InstallSteamButton");

                if (string.IsNullOrEmpty(path))
                {
                    if (steamStatusText != null) steamStatusText.Text = "No path specified";
                    if (installSteamButton != null) installSteamButton.IsVisible = true;
                    return;
                }

                if (IsValidSteamDirectory(path))
                {
                    if (steamStatusText != null)
                    {
                        steamStatusText.Text = "Valid Steam installation detected";
                        steamStatusText.Foreground = Brushes.LightGreen;
                    }
                    if (steamStatusIcon != null)
                    {
                        steamStatusIcon.Symbol = Symbol.Checkmark;
                        steamStatusIcon.Foreground = Brushes.LightGreen;
                    }
                    _customSteamPath = path;
                    if (installSteamButton != null) installSteamButton.IsVisible = false;
                }
                else
                {
                    if (steamStatusText != null)
                    {
                        steamStatusText.Text = "Invalid path - steam.exe not found";
                        steamStatusText.Foreground = Brushes.Red;
                    }
                    if (steamStatusIcon != null)
                    {
                        steamStatusIcon.Symbol = Symbol.Dismiss;
                        steamStatusIcon.Foreground = Brushes.Red;
                    }
                    _customSteamPath = string.Empty;
                    if (installSteamButton != null) installSteamButton.IsVisible = true;
                }
            });
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

        private int GetSteamProcessCount()
        {
            int count = 0;
            string[] processNames = { "steam", "steamwebhelper", "steamservice" };
            foreach (var name in processNames)
            {
                try
                {
                    count += Process.GetProcessesByName(name).Length;
                }
                catch { }
            }
            return count;
        }

        private async void BrowseSteamDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var storageProvider = this.StorageProvider;
            var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Steam Directory",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                var path = result[0].Path.LocalPath;
                var steamDirectoryTextBox = this.FindControl<TextBox>("SteamDirectoryTextBox");
                if (steamDirectoryTextBox != null)
                {
                    steamDirectoryTextBox.Text = path;
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                int processCount = GetSteamProcessCount();
                if (processCount > 0)
                {
                    await ShowMessage("Steam Processes Detected", 
                        $"Cannot start optimization because {processCount} Steam process{(processCount == 1 ? "" : "es")} are running.\n\nPlease close Steam completely before proceeding.");
                    return;
                }

                string steamPath = GetValidSteamPath();
                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    await ShowMessage("Steam Not Found", 
                        "Steam installation not found or invalid.\n\nPlease enter a valid Steam directory path or use Browse to locate it.\n\nThe directory must contain steam.exe");
                    return;
                }

                if (_steamService != null && !_steamService.IsRunningAsAdmin())
                {
                    var result = await ShowConfirmation("Administrator Privileges", 
                        "This application requires administrator privileges to function correctly.\n\nDo you want to restart it as administrator?");

                    if (result)
                    {
                        RestartAsAdmin();
                    }
                    return;
                }

                await StartOptimization(steamPath);
            }
            catch (Exception ex)
            {
                await ShowMessage("Error", "Error starting optimization: " + ex.Message);
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                if (_uninstallService != null && !_uninstallService.IsRunningAsAdmin())
                {
                    var result = await ShowConfirmation("Administrator Privileges", 
                        "Administrator privileges are required for restoration.\n\nDo you want to restart with elevated privileges?");

                    if (result)
                    {
                        RestartAsAdmin();
                    }
                    return;
                }

                await StartUninstall();
            }
            catch (Exception ex)
            {
                await ShowMessage("Error", "Error starting restoration: " + ex.Message);
            }
        }

        private async Task StartOptimization(string steamPath)
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var progressWindow = ProgressWindow.CreateOptimizationWindow();
                
                progressWindow.CancelRequested += (s, e) =>
                {
                    _cancellationTokenSource?.Cancel();
                };

                Action<string> progressHandler = (message) =>
                {
                    progressWindow.UpdateProgress(message);
                };
                
                if (_steamService != null)
                {
                    _steamService.ProgressChanged += progressHandler;
                }

                var startButton = this.FindControl<Button>("StartButton");
                var uninstallButton = this.FindControl<Button>("UninstallButton");
                if (startButton != null) startButton.IsEnabled = false;
                if (uninstallButton != null) uninstallButton.IsEnabled = false;

                var config = GetOptimizationConfig(steamPath);
                
                // Show the window non-modally first or modally? 
                // WPF used ShowDialog (modal). Avalonia ShowDialog is async.
                // We need to run the task in background while dialog is open.
                
                var optimizationTask = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _steamService.OptimizeSteamAsync(config, _cancellationTokenSource.Token);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (result.Success)
                            {
                                progressWindow.SetCompleted(true, $"Optimization completed successfully in {result.Duration:mm\\:ss}");
                            }
                            else
                            {
                                progressWindow.SetError("Optimization failed: " + result.ErrorMessage);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            progressWindow.SetError("Operation cancelled by user");
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            progressWindow.SetError("Unexpected error: " + ex.Message);
                        });
                    }
                    finally
                    {
                        _isProcessing = false;
                        
                        if (_steamService != null)
                        {
                            _steamService.ProgressChanged -= progressHandler;
                        }
                        
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (startButton != null) startButton.IsEnabled = true;
                            if (uninstallButton != null) uninstallButton.IsEnabled = true;
                        });
                    }
                });

                await progressWindow.ShowDialog(this);

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                await LoadSystemInfoSafe();
            }
            catch (Exception ex)
            {
                _isProcessing = false;
                await ShowMessage("Error", "Error: " + ex.Message);
            }
        }

        private async Task StartUninstall()
        {
            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var progressWindow = ProgressWindow.CreateRestoreWindow();
                
                progressWindow.CancelRequested += (s, e) =>
                {
                    _cancellationTokenSource?.Cancel();
                };

                Action<string> progressHandler = (message) =>
                {
                    progressWindow.UpdateProgress(message);
                };
                
                if (_uninstallService != null)
                {
                    _uninstallService.ProgressChanged += progressHandler;
                }

                var startButton = this.FindControl<Button>("StartButton");
                var uninstallButton = this.FindControl<Button>("UninstallButton");
                if (startButton != null) startButton.IsEnabled = false;
                if (uninstallButton != null) uninstallButton.IsEnabled = false;

                var uninstallTask = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _uninstallService.UninstallAsync(_cancellationTokenSource.Token);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (result.Success)
                            {
                                progressWindow.SetCompleted(true, "Restoration completed successfully");
                            }
                            else
                            {
                                progressWindow.SetError("Restoration failed: " + result.ErrorMessage);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            progressWindow.SetError("Operation cancelled by user");
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            progressWindow.SetError("Unexpected error: " + ex.Message);
                        });
                    }
                    finally
                    {
                        _isProcessing = false;
                        
                        if (_uninstallService != null)
                        {
                            _uninstallService.ProgressChanged -= progressHandler;
                        }
                        
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (startButton != null) startButton.IsEnabled = true;
                            if (uninstallButton != null) uninstallButton.IsEnabled = true;
                        });
                    }
                });

                await progressWindow.ShowDialog(this);

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                _isProcessing = false;
                await ShowMessage("Error", "Error: " + ex.Message);
            }
        }

        private string GetValidSteamPath()
        {
            var steamDirectoryTextBox = this.FindControl<TextBox>("SteamDirectoryTextBox");
            string textboxPath = steamDirectoryTextBox?.Text?.Trim() ?? "";

            if (!string.IsNullOrEmpty(textboxPath) && IsValidSteamDirectory(textboxPath))
                return textboxPath;

            if (!string.IsNullOrEmpty(_customSteamPath) && IsValidSteamDirectory(_customSteamPath))
                return _customSteamPath;

            if (_steamService != null)
                return _steamService.GetSteamPath();

            return string.Empty;
        }

        private OptimizationConfig GetOptimizationConfig(string steamPath)
        {
            try
            {
                var removeFromStartup = this.FindControl<CheckBox>("RemoveFromStartup");
                var updateSteam = this.FindControl<CheckBox>("UpdateSteam");
                var createDesktopShortcut = this.FindControl<CheckBox>("CreateDesktopShortcut");
                var createSteamConfig = this.FindControl<CheckBox>("CreateSteamConfig");
                var clearDownloadCache = this.FindControl<CheckBox>("ClearDownloadCache");

                return new OptimizationConfig
                {
                    Mode = _selectedMode ?? "Normal2023June",
                    RemoveFromStartup = removeFromStartup?.IsChecked == true,
                    UpdateSteam = updateSteam?.IsChecked == true && updateSteam?.IsEnabled == true,
                    SteamPath = steamPath,
                    CreateDesktopShortcut = createDesktopShortcut?.IsChecked == true,
                    CreateSteamConfig = createSteamConfig?.IsChecked == true,
                    ClearDownloadCache = clearDownloadCache?.IsChecked == true
                };
            }
            catch
            {
                return new OptimizationConfig
                {
                    Mode = "Normal2023June",
                    SteamPath = steamPath,
                    CreateDesktopShortcut = true,
                    CreateSteamConfig = true,
                    ClearDownloadCache = false
                };
            }
        }

        /// <summary>
        /// Requests administrator privileges by instructing the user to restart manually.
        /// This avoids using "runas" which triggers antivirus heuristics.
        /// </summary>
        /// <remarks>
        /// User must manually restart the application as administrator.
        /// Source: https://github.com/AltRossell/Steam-Debloat
        /// </remarks>
        private async void RestartAsAdmin()
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Administrator Privileges Required",
                    Content = "This operation requires administrator privileges.\n\n" +
                             "Please follow these steps:\n" +
                             "1. Close this application\n" +
                             "2. Right-click on SteamDebloat.exe\n" +
                             "3. Select 'Run as administrator'\n\n" +
                             "Do you want to close the application now?",
                    PrimaryButtonText = "Close Application",
                    CloseButtonText = "Cancel"
                };
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Give user time to read the message
                    await Task.Delay(1000);
                    Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Dialog failed: {ex.Message}");
            }
        }

        private async Task ShowMessage(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private async Task<bool> ShowConfirmation(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void InstallSteamButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://store.steampowered.com/about/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ShowMessage("Error", "Error opening Steam download page: " + ex.Message);
                });
            }
        }

        private void SteamButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://steamcommunity.com/id/padredemessi/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Ignore errors if browser cannot be opened
                System.Diagnostics.Debug.WriteLine($"Error opening Steam profile: {ex.Message}");
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/AltRossell/Steam-Debloat",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error opening GitHub: " + ex.Message);
            }
        }

        private void ClearPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var steamDirectoryTextBox = this.FindControl<TextBox>("SteamDirectoryTextBox");
                if (steamDirectoryTextBox != null)
                {
                    steamDirectoryTextBox.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing path: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (_validationTimer != null)
                {
                    _validationTimer.Dispose();
                    _validationTimer = null;
                }

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                if (_steamService != null)
                {
                    _steamService.SteamDetectionChanged -= OnSteamDetectionChanged;
                    _steamService.Dispose();
                    _steamService = null;
                }

                if (_uninstallService != null)
                {
                    _uninstallService.Dispose();
                    _uninstallService = null;
                }
            }
            catch { }

            base.OnClosed(e);
        }
    }
}


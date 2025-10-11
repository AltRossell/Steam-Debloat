using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;

namespace SteamDebloat;

public partial class MainWindow : Window
{
    private SteamDebloatService? _steamService;
    private UninstallService? _uninstallService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isProcessing = false;
    private string _customSteamPath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBasicUI();
        PopulateModeComboBox();
        _ = InitializeServicesAsync();
    }

    private void PopulateModeComboBox()
    {
        try
        {
            ModeComboBox.Items.Clear();

            // Create common items
            var item2 = new ComboBoxItem { Content = "Normal 2023 June (Recommended)", Tag = "Normal2023June" };
            var item1 = new ComboBoxItem { Content = "Normal 2025 October", Tag = "Normal2025October" };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Only two versions
                ModeComboBox.Items.Add(item2);
                ModeComboBox.Items.Add(item1);
                ModeComboBox.SelectedIndex = 0; // Normal 2023 June as default
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: All versions
                var item3 = new ComboBoxItem { Content = "Normal 2022 Dec", Tag = "Normal2022dec" };
                var item4 = new ComboBoxItem { Content = "Lite 2022 Dec", Tag = "Lite2022dec" };

                ModeComboBox.Items.Add(item2);
                ModeComboBox.Items.Add(item1);
                ModeComboBox.Items.Add(item3);
                ModeComboBox.Items.Add(item4);
                ModeComboBox.SelectedIndex = 0; // Normal 2023 June as default
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error populating ComboBox: {ex.Message}");
        }
    }

    private void InitializeBasicUI()
    {
        try
        {
            CheckAdminPrivileges();
            SetDefaultValues();

            // Configure option visibility based on platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux: hide Windows-specific options
                CreateDesktopShortcut.IsVisible = false;
                AddToStartMenu.IsVisible = false;
                RemoveFromStartup.IsVisible = false;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows: show all options
                CreateDesktopShortcut.IsVisible = true;
                AddToStartMenu.IsVisible = true;
                RemoveFromStartup.IsVisible = true;
            }
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
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _steamService = new SteamDebloatService();
                _steamService.ProgressChanged += OnProgressChanged;
                _steamService.SteamDetectionChanged += OnSteamDetectionChanged;

                _uninstallService = new UninstallService();
                _uninstallService.ProgressChanged += OnProgressChanged;
            });

            await LoadSystemInfoSafe();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateUninstallStatus();
            });
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

            OSVersionText2.Text = "Loading...";
            OSArchText2.Text = "Loading...";
            SteamPathText2.Text = "Searching...";
            SteamConfigText2.Text = "Searching...";

            SteamDirectoryTextBox.Text = "";

            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Linux";
            PlatformText.Text = platform;
            PlatformText2.Text = platform;
        }
        catch { }
    }

    private void OnSteamDetectionChanged(bool steamInstalled)
    {
        Dispatcher.UIThread.Post(async () =>
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                statusMessage = isAdmin ? "Administrator" : "Standard user (will request elevation)";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                isAdmin = Environment.UserName == "root";

                if (!isAdmin)
                {
                    try
                    {
                        using var pkexecCheck = Process.Start(new ProcessStartInfo
                        {
                            FileName = "which",
                            Arguments = "pkexec",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        pkexecCheck?.WaitForExit();
                        bool hasPkexec = pkexecCheck?.ExitCode == 0;

                        statusMessage = hasPkexec ?
                        "Standard user (pkexec available)" :
                        "Standard user (pkexec not found - sudo required)";
                    }
                    catch
                    {
                        statusMessage = "Standard user";
                    }
                }
                else
                {
                    statusMessage = "Root";
                }
            }

            UpdateAdminStatusElements(isAdmin, statusMessage);
        }
        catch
        {
            UpdateAdminStatusElements(false, "Unable to check privileges");
        }
    }

    private void UpdateAdminStatusElements(bool isAdmin, string? customMessage = null)
    {
        try
        {
            string statusText = customMessage ?? (isAdmin ? "Administrator/Root" : "Standard user");
            var textColor = isAdmin ? Brushes.Green : Brushes.Orange;

            AdminStatusText.Text = statusText;
            AdminStatusText.Foreground = textColor;

            AdminStatusText2.Text = statusText;
            AdminStatusText2.Foreground = textColor;
        }
        catch { }
    }

    private async Task LoadSystemInfoSafe()
    {
        try
        {
            if (_steamService == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateSystemInfoElements("Service not available",
                                             "Loading...",
                                             "Loading...",
                                             "Service not available");
                });
                return;
            }

            var systemInfo = await _steamService.GetSystemInfoAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
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
            await Dispatcher.UIThread.InvokeAsync(() =>
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

            OSVersionText2.Text = osVersion;
            OSArchText2.Text = architecture;
            SteamPathText2.Text = steamPath;
            SteamConfigText2.Text = configStatus;
        }
        catch { }
    }

    private void UpdateSteamPathDisplay(SystemInfo? systemInfo = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(_customSteamPath))
            {
                SteamPathText.Text = $"Custom: {_customSteamPath}";
                SteamPathText2.Text = $"Custom: {_customSteamPath}";
                SteamDirectoryTextBox.Text = _customSteamPath;
                return;
            }

            if (systemInfo?.SteamFound == true)
            {
                SteamPathText.Text = systemInfo.SteamPath;
                SteamPathText2.Text = systemInfo.SteamPath;
                SteamDirectoryTextBox.Text = systemInfo.SteamPath;
            }
            else
            {
                SteamPathText.Text = "Steam not found - Please browse manually";
                SteamPathText2.Text = "Steam not found - Please browse manually";
                SteamDirectoryTextBox.Text = "";
            }
        }
        catch { }
    }

    private void UpdateUninstallStatus()
    {
        try
        {
            if (_uninstallService == null)
            {
                UninstallSteamConfigText.Text = "Service not available";
                return;
            }

            var status = _uninstallService.GetUninstallStatus();

            UninstallSteamConfigText.Text = status.SteamConfigStatus;
            UninstallSteamConfigText.Foreground = status.HasOptimizationConfig ?
            Brushes.Orange : Brushes.Gray;
        }
        catch
        {
            UninstallSteamConfigText.Text = "Unable to check status";
        }
    }

    private async void BrowseSteamDirectoryButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Steam installation directory",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var selectedPath = folders[0].Path.LocalPath;

                if (IsValidSteamDirectory(selectedPath))
                {
                    _customSteamPath = selectedPath;
                    SteamDirectoryTextBox.Text = selectedPath;
                    SteamPathText.Text = $"Custom: {selectedPath}";
                    SteamPathText2.Text = $"Custom: {selectedPath}";

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
                    SteamConfigText2.Text = configStatus;
                }
                else
                {
                    await ShowMessageBox("Invalid Steam Directory",
                                         "The selected directory does not appear to be a valid Steam installation.\n\n" +
                                         "Please make sure you select the folder containing the Steam executable.");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error", $"Error browsing for Steam directory: {ex.Message}");
        }
    }

    private bool IsValidSteamDirectory(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return File.Exists(Path.Combine(path, "steam.exe"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists(Path.Combine(path, "steam.sh")) ||
                File.Exists(Path.Combine(path, "ubuntu12_32/steam"));
            }

            return false;
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

    private void ModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            UpdateModeSpecificInfo();
        }
        catch { }
    }

    private void UpdateModeSpecificInfo()
    {
        try
        {
            if (ModeComboBox.SelectedItem is ComboBoxItem selected && selected.Tag != null)
            {
                UpdateSteam.IsEnabled = true;
                UpdateSteam.Content = "Update Steam automatically";
                UpdateSteam.IsChecked = true;
            }
        }
        catch { }
    }

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isProcessing) return;

        try
        {
            string steamPath = GetValidSteamPath();
            if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
            {
                await ShowMessageBox("Steam Not Found",
                                     "Steam installation not found or invalid.\n\n" +
                                     "Please browse for your Steam directory manually or install Steam first.\n\n" +
                                     "Make sure to select the folder containing the Steam executable.");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                bool hasPkexec = CheckPkexecAvailable();
                bool isRoot = Environment.UserName == "root";

                if (!hasPkexec && !isRoot)
                {
                    await ShowMessageBox("pkexec Required",
                                         "This application requires pkexec to modify Steam files.\n\n" +
                                         "Please install pkexec:\n" +
                                         "  Ubuntu/Debian: sudo apt install policykit-1\n" +
                                         "  Fedora: sudo dnf install polkit\n" +
                                         "  Arch: sudo pacman -S polkit\n\n" +
                                         "Or run the application with sudo.");
                    return;
                }

                if (!isRoot && hasPkexec)
                {
                    var result = await ShowConfirmDialog("Permission Required",
                                                         "This application will use pkexec to modify Steam files.\n\n" +
                                                         "You will be prompted for your password during the process.\n\n" +
                                                         "Continue?");

                    if (!result) return;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_steamService?.IsRunningAsAdmin() != true)
                {
                    var result = await ShowConfirmDialog("Administrator Privileges",
                                                         "This application requires administrator privileges to function correctly.\n\n" +
                                                         "Do you want to restart it as administrator?");

                    if (result)
                    {
                        RestartAsAdmin();
                    }
                    return;
                }
            }

            await StartOptimization(steamPath);
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error", $"Error starting optimization: {ex.Message}");
        }
    }

    private bool CheckPkexecAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "pkexec",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async void UninstallButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isProcessing) return;

        try
        {
            var result = await ShowConfirmDialog("Confirm Uninstallation",
                                                 "Are you sure you want to uninstall Steam Debloat?\n\n" +
                                                 "This will remove optimization configurations and restore Steam to its original state.\n" +
                                                 "Your games and personal settings will be preserved.");

            if (!result)
                return;

            if (_uninstallService?.IsRunningAsAdmin() != true)
            {
                var adminResult = await ShowConfirmDialog("Administrator Privileges",
                                                          "Administrator/root privileges are required for uninstallation.\n\n" +
                                                          "Do you want to restart with elevated privileges?");

                if (adminResult)
                {
                    RestartAsAdmin();
                }
                return;
            }

            await StartUninstall();
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error", $"Error starting uninstallation: {ex.Message}");
        }
    }

    private async Task StartOptimization(string steamPath)
    {
        try
        {
            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowProgressOverlay(true);
                StartButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;
            });

            var config = GetOptimizationConfig(steamPath);

            var result = await _steamService!.OptimizeSteamAsync(config, _cancellationTokenSource.Token);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (result.Success)
                {
                    await ShowMessageBox("Success", $"Optimization completed successfully in {result.Duration:mm\\:ss}");
                }
                else
                {
                    await ShowMessageBox("Error", $"Error during optimization:\n{result.ErrorMessage}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowMessageBox("Cancelled", "Operation cancelled by user.");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowMessageBox("Error", $"Unexpected error: {ex.Message}");
            });
        }
        finally
        {
            _isProcessing = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowProgressOverlay(false);
                StartButton.IsEnabled = true;
                UninstallButton.IsEnabled = true;
            });

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            await LoadSystemInfoSafe();
            await Dispatcher.UIThread.InvokeAsync(() => UpdateUninstallStatus());
        }
    }

    private async Task StartUninstall()
    {
        try
        {
            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowProgressOverlay(true);
                StartButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;
            });

            var result = await _uninstallService!.UninstallAsync(_cancellationTokenSource.Token);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (result.Success)
                {
                    await ShowMessageBox("Success", "Uninstallation completed successfully.");
                }
                else
                {
                    await ShowMessageBox("Error", $"Error during uninstallation:\n{result.ErrorMessage}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowMessageBox("Cancelled", "Operation cancelled by user.");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowMessageBox("Error", $"Unexpected error: {ex.Message}");
            });
        }
        finally
        {
            _isProcessing = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowProgressOverlay(false);
                StartButton.IsEnabled = true;
                UninstallButton.IsEnabled = true;
            });

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            await LoadSystemInfoSafe();
            await Dispatcher.UIThread.InvokeAsync(() => UpdateUninstallStatus());
        }
    }

    private OptimizationConfig GetOptimizationConfig(string steamPath)
    {
        try
        {
            var selectedItem = ModeComboBox.SelectedItem as ComboBoxItem;
            string mode = selectedItem?.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(mode))
            {
                // Default to Normal 2023 June for both platforms
                mode = "Normal2023June";
            }

            return new OptimizationConfig
            {
                Mode = mode,
                RemoveFromStartup = RemoveFromStartup.IsChecked == true && RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                UpdateSteam = UpdateSteam.IsChecked == true && UpdateSteam.IsEnabled == true,
                SteamPath = steamPath
            };
        }
        catch
        {
            // Default to Normal 2023 June for both platforms
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
            ProgressOverlay.IsVisible = show;
        }
        catch { }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
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
            Dispatcher.UIThread.Post(() =>
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
                _ = ShowMessageBox("Error", "Could not determine application path for restart.");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = fileName,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var methods = new[] { "pkexec", "sudo", "gksudo", "kdesudo" };
                bool started = false;

                foreach (var method in methods)
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = method,
                            Arguments = fileName
                        };
                        Process.Start(startInfo);
                        started = true;
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (!started)
                {
                    _ = ShowMessageBox("Error", "Could not find a privilege escalation tool.\nPlease run manually as root.");
                    return;
                }
            }

            Close();
        }
        catch (Exception ex)
        {
            _ = ShowMessageBox("Error", $"Could not restart with elevated privileges: {ex.Message}");
        }
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 450,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var result = false;

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var yesButton = new Button
        {
            Content = "Yes",
            Width = 80,
            Height = 35
        };
        yesButton.Click += (s, e) =>
        {
            result = true;
            dialog.Close();
        };

        var noButton = new Button
        {
            Content = "No",
            Width = 80,
            Height = 35
        };
        noButton.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task ShowMessageBox(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 450,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14
        });

        var button = new Button
        {
            Content = "OK",
            Width = 80,
            Height = 35,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        button.Click += (s, e) => dialog.Close();

        panel.Children.Add(button);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
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
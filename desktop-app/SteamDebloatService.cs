using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Security.Principal;

namespace SteamDebloat
{
    public class SteamDebloatService : IDisposable
    {
        private readonly Dictionary<string, string> _steamModes;
        private const string DefaultSteamPath = @"C:\Program Files (x86)\Steam";
        private const string DefaultSteamV2Path = @"C:\Program Files (x86)\Steamv2";
        private Timer _steamDetectionTimer;
        private bool _disposed = false;
        private string _lastDetectedSteamPath = null;

        public event Action<string> ProgressChanged;
        public event Action<bool> SteamDetectionChanged;

        public SteamDebloatService()
        {
            try
            {
                _steamModes = new Dictionary<string, string>
                {
                    ["normal2025september"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                    ["normal2022dec"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                    ["lite2022dec"] = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
                };
                
                // Initialize timer with delay to avoid initialization issues
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    if (!_disposed)
                    {
                        StartSteamDetectionTimer();
                    }
                });
            }
            catch
            {
                // Ignore initialization errors
            }
        }

        private void StartSteamDetectionTimer()
        {
            try
            {
                if (_disposed) return;
                
                _steamDetectionTimer = new Timer(CheckSteamInstallation, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }
            catch
            {
                // Ignore timer creation errors
            }
        }

        private void CheckSteamInstallation(object state)
        {
            try
            {
                if (_disposed) return;
                
                var currentPath = FindSteamInstallation();
                bool steamFound = !string.IsNullOrEmpty(currentPath);
                
                // Only trigger event if status actually changed
                if (_lastDetectedSteamPath != currentPath)
                {
                    _lastDetectedSteamPath = currentPath;
                    SteamDetectionChanged?.Invoke(steamFound);
                }
            }
            catch
            {
                // Ignore detection errors
            }
        }

        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var osVersion = GetWindowsVersionFromRegistry();
                    var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                    var steamPath = FindSteamInstallation();
                    var steamExists = !string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath);
                    var configExists = steamExists && steamPath != null && File.Exists(Path.Combine(steamPath, "steam.cfg"));

                    return new SystemInfo
                    {
                        OSVersion = osVersion,
                        Architecture = architecture,
                        SteamPath = steamPath ?? "Not found",
                        SteamFound = steamExists,
                        ConfigExists = configExists
                    };
                }
                catch
                {
                    return new SystemInfo
                    {
                        OSVersion = "Error loading",
                        Architecture = "Error loading",
                        SteamPath = "Error loading",
                        SteamFound = false,
                        ConfigExists = false
                    };
                }
            }).ConfigureAwait(false);
        }

        private string GetWindowsVersionFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString();
                        if (!string.IsNullOrEmpty(productName))
                        {
                            return productName;
                        }
                    }
                }
            }
            catch
            {
                // Registry detection failed
            }

            return Environment.OSVersion.VersionString;
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

        private string FindSteamInstallation()
        {
            try
            {
                // Method 1: Try common installation paths first
                var commonPaths = new[]
                {
                    DefaultSteamPath,
                    @"C:\Steam",
                    @"D:\Steam",
                    @"E:\Steam",
                    @"F:\Steam",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
                };

                foreach (var path in commonPaths)
                {
                    if (IsValidSteamDirectory(path))
                    {
                        return path;
                    }
                }

                // Method 2: Try Windows Registry (multiple locations)
                string[] registryPaths = {
                    @"SOFTWARE\WOW6432Node\Valve\Steam",
                    @"SOFTWARE\Valve\Steam",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 0"
                };

                foreach (var regPath in registryPaths)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(regPath))
                        {
                            if (key != null)
                            {
                                string installPath = key.GetValue("InstallPath")?.ToString();
                                if (!string.IsNullOrEmpty(installPath) && IsValidSteamDirectory(installPath))
                                {
                                    return installPath;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Registry check failed
                    }
                }

                // Method 3: Try current user registry
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                    {
                        if (key != null)
                        {
                            string steamPath = key.GetValue("SteamPath")?.ToString();
                            if (!string.IsNullOrEmpty(steamPath) && IsValidSteamDirectory(steamPath))
                            {
                                return steamPath;
                            }
                        }
                    }
                }
                catch
                {
                    // User registry check failed
                }

                // Method 4: Try environment variables
                try
                {
                    string steamFromEnv = Environment.GetEnvironmentVariable("STEAM_INSTALL_PATH");
                    if (!string.IsNullOrEmpty(steamFromEnv) && IsValidSteamDirectory(steamFromEnv))
                    {
                        return steamFromEnv;
                    }
                }
                catch
                {
                    // Environment variable check failed
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string GetSteamPath()
        {
            var detected = FindSteamInstallation();
            return detected ?? DefaultSteamPath;
        }

        public async Task<OptimizationResult> OptimizeSteamAsync(OptimizationConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                OnProgressChanged("Starting optimization...");
                
                var steamPath = !string.IsNullOrEmpty(config.SteamPath) ? config.SteamPath : FindSteamInstallation();
                
                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    return new OptimizationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Steam is not installed or the specified path is invalid. Please install Steam or select a valid Steam directory." 
                    };
                }

                OnProgressChanged($"Steam found at: {steamPath}");
                
                await StopSteamProcessesAsync().ConfigureAwait(false);
                OnProgressChanged("Steam processes stopped");

                OptimizationResult result;
                if (config.Mode == "NormalBoth2022-2025")
                {
                    result = await ProcessBothVersionsMode(config, cancellationToken, steamPath).ConfigureAwait(false);
                }
                else
                {
                    result = await ProcessSingleVersionMode(config, cancellationToken, steamPath).ConfigureAwait(false);
                }
                
                result.Duration = stopwatch.Elapsed;
                return result;
            }
            catch (OperationCanceledException)
            {
                return new OptimizationResult { Success = false, ErrorMessage = "Operation cancelled by user" };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task<OptimizationResult> ProcessSingleVersionMode(OptimizationConfig config, CancellationToken cancellationToken, string steamPath)
        {
            try
            {
                OnProgressChanged("Verifying Steam installation...");
                
                if (config.UpdateSteam)
                {
                    OnProgressChanged($"Updating Steam for {config.Mode} mode...");
                    await UpdateSteamAsync(steamPath, config.Mode, cancellationToken).ConfigureAwait(false);
                }

                OnProgressChanged("Applying optimization configuration...");
                await CreateConfigurationFilesAsync(steamPath, config, cancellationToken).ConfigureAwait(false);
                
                if (config.CreateDesktopShortcut)
                {
                    OnProgressChanged("Creating desktop shortcut...");
                    CreateDesktopShortcut(steamPath, config.Mode, "Steam.bat");
                }

                if (config.CreateStartMenuShortcut)
                {
                    OnProgressChanged("Creating start menu shortcut...");
                    CreateStartMenuShortcut(steamPath, config.Mode, "Steam.bat");
                }

                if (config.RemoveFromStartup)
                {
                    OnProgressChanged("Removing Steam from Windows startup...");
                    RemoveSteamFromStartup();
                }

                OnProgressChanged("Optimization completed");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task<OptimizationResult> ProcessBothVersionsMode(OptimizationConfig config, CancellationToken cancellationToken, string existingSteamPath)
        {
            try
            {
                OnProgressChanged("Preparing dual Steam installation...");
                
                var steam2025Path = DefaultSteamPath;
                var steam2022Path = DefaultSteamV2Path;

                if (!Directory.Exists(steam2022Path))
                {
                    OnProgressChanged("Creating Steam 2022 directory...");
                    try
                    {
                        Directory.CreateDirectory(steam2022Path);
                        await CopyDirectoryAsync(existingSteamPath, steam2022Path, cancellationToken).ConfigureAwait(false);
                        OnProgressChanged("Steam 2022 directory created");
                    }
                    catch (Exception ex)
                    {
                        return new OptimizationResult 
                        { 
                            Success = false, 
                            ErrorMessage = $"Could not create Steam 2022 directory: {ex.Message}" 
                        };
                    }
                }

                OnProgressChanged("Updating Steam 2025...");
                await UpdateSteamAsync(steam2025Path, "Normal2025September", cancellationToken).ConfigureAwait(false);

                OnProgressChanged("Updating Steam 2022...");
                await UpdateSteamAsync(steam2022Path, "Normal2022dec", cancellationToken).ConfigureAwait(false);

                OnProgressChanged("Applying configurations...");
                
                await CreateConfigurationFilesAsync(steam2025Path, new OptimizationConfig { Mode = "Normal2025September", SteamPath = steam2025Path }, cancellationToken).ConfigureAwait(false);
                await CreateConfigurationFilesAsync(steam2022Path, new OptimizationConfig { Mode = "Normal2022dec", SteamPath = steam2022Path }, cancellationToken).ConfigureAwait(false);

                if (config.CreateDesktopShortcut)
                {
                    OnProgressChanged("Creating desktop shortcuts...");
                    CreateDesktopShortcut(steam2025Path, "Normal2025September", "Steam2025.bat");
                    CreateDesktopShortcut(steam2022Path, "Normal2022dec", "Steam2022.bat");
                }

                if (config.CreateStartMenuShortcut)
                {
                    OnProgressChanged("Creating start menu shortcuts...");
                    CreateStartMenuShortcut(steam2025Path, "Normal2025September", "Steam2025.bat");
                    CreateStartMenuShortcut(steam2022Path, "Normal2022dec", "Steam2022.bat");
                }

                if (config.RemoveFromStartup)
                {
                    RemoveSteamFromStartup();
                }

                OnProgressChanged("Optimization completed");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task StopSteamProcessesAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var processes = Process.GetProcessesByName("steam");
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                        catch
                        {
                            // Ignore errors when terminating processes
                        }
                    }
                }).ConfigureAwait(false);
            }
            catch
            {
                // Ignore process termination errors
            }
        }

        private async Task UpdateSteamAsync(string steamPath, string mode, CancellationToken cancellationToken)
        {
            try
            {
                var steamExe = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamExe)) 
                {
                    OnProgressChanged($"Steam executable not found at {steamPath}");
                    return;
                }

                var modeKey = mode.ToLower();
                var arguments = (modeKey == "normal2022dec" || modeKey == "lite2022dec")
                    ? "-forcesteamupdate -forcepackagedownload -overridepackageurl https://archive.org/download/dec2022steam -exitsteam"
                    : "-forcesteamupdate -forcepackagedownload -overridepackageurl -exitsteam";

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = steamPath
                };

                OnProgressChanged($"Starting Steam update for {mode} mode...");
                
                using (var process = Process.Start(startInfo))
                {
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
                    
                    var timeout = DateTime.Now.AddMinutes(10);
                    bool updateInProgress = true;
                    
                    while (DateTime.Now < timeout && updateInProgress && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                        
                        var steamProcesses = Process.GetProcessesByName("steam");
                        
                        if (steamProcesses.Length == 0)
                        {
                            OnProgressChanged($"Steam update completed for {mode}");
                            updateInProgress = false;
                            break;
                        }
                        else
                        {
                            OnProgressChanged($"Steam updating for {mode} mode... (visible on screen)");
                        }
                    }
                    
                    if (updateInProgress)
                    {
                        OnProgressChanged("Finalizing update...");
                        await StopSteamProcessesAsync().ConfigureAwait(false);
                        await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error during update: {ex.Message}");
            }
        }

        private async Task CreateConfigurationFilesAsync(string steamPath, OptimizationConfig config, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    var configPath = Path.Combine(steamPath, "steam.cfg");
                    var configContent = $@"BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
:: Mode: {config.Mode}";

                    File.WriteAllText(configPath, configContent);

                    var batchFileName = config.Mode == "NormalBoth2022-2025" ? 
                        (steamPath.Contains("Steamv2") ? "Steam2022.bat" : "Steam2025.bat") : "Steam.bat";
                    
                    var tempBatchPath = Path.Combine(Path.GetTempPath(), batchFileName);
                    var modeKey = config.Mode.ToLower();
                    
                    string steamArgs = "";
                    if (modeKey == "normalboth2022-2025")
                    {
                        steamArgs = steamPath.Contains("Steamv2") ? _steamModes["normal2022dec"] : _steamModes["normal2025september"];
                    }
                    else if (_steamModes.ContainsKey(modeKey))
                    {
                        steamArgs = _steamModes[modeKey];
                    }

                    var batchContent = $@"@echo off
cd /d ""{steamPath}""
start Steam.exe {steamArgs}
:: Mode: {config.Mode}";

                    File.WriteAllText(tempBatchPath, batchContent);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void CreateDesktopShortcut(string steamPath, string mode, string fileName)
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var shortcutPath = Path.Combine(desktopPath, fileName);
                var tempBatchPath = Path.Combine(Path.GetTempPath(), fileName);

                if (File.Exists(tempBatchPath))
                {
                    File.Copy(tempBatchPath, shortcutPath, true);
                }
            }
            catch
            {
                // Ignore shortcut creation errors
            }
        }

        private void CreateStartMenuShortcut(string steamPath, string mode, string fileName)
        {
            try
            {
                var startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs", "Steam");

                Directory.CreateDirectory(startMenuPath);
                
                var shortcutPath = Path.Combine(startMenuPath, fileName);
                var tempBatchPath = Path.Combine(Path.GetTempPath(), fileName);

                if (File.Exists(tempBatchPath))
                {
                    File.Copy(tempBatchPath, shortcutPath, true);
                }
            }
            catch
            {
                // Ignore shortcut creation errors
            }
        }

        private void RemoveSteamFromStartup()
        {
            try
            {
                var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (var key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                {
                    if (key?.GetValue("Steam") != null)
                    {
                        key.DeleteValue("Steam");
                    }
                }
            }
            catch
            {
                // Ignore startup removal errors
            }
        }

        public bool IsRunningAsAdmin()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            try
            {
                var dir = new DirectoryInfo(sourceDir);
                if (!dir.Exists)
                    return;

                DirectoryInfo[] dirs = dir.GetDirectories();
                Directory.CreateDirectory(destinationDir);

                foreach (FileInfo file in dir.GetFiles())
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }

                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task CopyDirectoryAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                return;

            await Task.Run(() => CopyDirectory(sourcePath, destinationPath), cancellationToken).ConfigureAwait(false);
        }

        private void OnProgressChanged(string status)
        {
            try
            {
                ProgressChanged?.Invoke(status);
            }
            catch
            {
                // Ignore progress update errors
            }
        }

        public void Dispose()
        {
            try
            {
                _disposed = true;
                _steamDetectionTimer?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SteamDebloat
{
    public class SteamDebloatService : IDisposable
    {
        private readonly Dictionary<string, string> _steamModes;
        private readonly string _defaultSteamPath;
        private Timer _steamDetectionTimer;
        private bool _disposed = false;
        private string _lastDetectedSteamPath = null;

        public event Action<string> ProgressChanged;
        public event Action<bool> SteamDetectionChanged;

        public SteamDebloatService()
        {
            _defaultSteamPath = @"C:\Program Files (x86)\Steam";

            _steamModes = new Dictionary<string, string>
            {
                ["normal2025november"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["normal2023june"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad steam://open/library",
                ["normal2022dec"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["lite2022dec"] = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["2023"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad steam://open/library",
                ["2024"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["2025"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
            };

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                if (!_disposed)
                {
                    StartSteamDetectionTimer();
                }
            });
        }

        private void StartSteamDetectionTimer()
        {
            if (_disposed) return;
            _steamDetectionTimer = new Timer(CheckSteamInstallation, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private void CheckSteamInstallation(object state)
        {
            if (_disposed) return;

            var currentPath = FindSteamInstallation();
            bool steamFound = !string.IsNullOrEmpty(currentPath);

            if (_lastDetectedSteamPath != currentPath)
            {
                _lastDetectedSteamPath = currentPath;
                if (SteamDetectionChanged != null)
                    SteamDetectionChanged.Invoke(steamFound);
            }
        }

        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var osVersion = GetWindowsVersion();
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
            });
        }

        private string GetWindowsVersion()
        {
            try
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            string productName = key.GetValue("ProductName") as string;
                            string displayVersion = key.GetValue("DisplayVersion") as string;
                            string currentBuild = key.GetValue("CurrentBuild") as string;
                            
                            if (!string.IsNullOrEmpty(productName))
                            {
                                productName = productName.Replace("Microsoft ", "");
                                
                                if (!string.IsNullOrEmpty(displayVersion))
                                {
                                    return string.Format("{0} ({1})", productName, displayVersion);
                                }
                                else if (!string.IsNullOrEmpty(currentBuild))
                                {
                                    return string.Format("{0} (Build {1})", productName, currentBuild);
                                }
                                
                                return productName;
                            }
                        }
                    }
                }
                catch { }

                int build = Environment.OSVersion.Version.Build;
                int major = Environment.OSVersion.Version.Major;
                int minor = Environment.OSVersion.Version.Minor;

                if (major >= 10 && build >= 22000)
                {
                    return string.Format("Windows 11 (Build {0})", build);
                }
                
                if (major >= 10 && build >= 10240)
                {
                    return string.Format("Windows 10 (Build {0})", build);
                }

                if (major == 6 && minor == 3)
                {
                    return "Windows 8.1";
                }

                if (major == 6 && minor == 2)
                {
                    return "Windows 8";
                }

                if (major == 6 && minor == 1)
                {
                    return "Windows 7";
                }

                if (major == 6 && minor == 0)
                {
                    return "Windows Vista";
                }

                return string.Format("Windows {0}.{1} (Build {2})", major, minor, build);
            }
            catch
            {
                return "Windows (Unknown version)";
            }
        }

        private bool IsValidSteamDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            return File.Exists(Path.Combine(path, "steam.exe"));
        }

        private string FindSteamInstallation()
        {
            try
            {
                return FindSteamOnWindows();
            }
            catch
            {
                return null;
            }
        }

        private string FindSteamOnWindows()
        {
            var commonPaths = new[]
            {
                _defaultSteamPath,
                @"C:\Steam",
                @"D:\Steam",
                @"E:\Steam",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
            };

            foreach (var path in commonPaths)
            {
                if (IsValidSteamDirectory(path))
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
                            string installPath = key.GetValue("InstallPath") as string;
                            if (!string.IsNullOrEmpty(installPath) && IsValidSteamDirectory(installPath))
                                return installPath;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public string GetSteamPath()
        {
            var detected = FindSteamInstallation();
            return detected ?? _defaultSteamPath;
        }

        public async Task<OptimizationResult> OptimizeSteamAsync(OptimizationConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                OnProgressChanged("Starting optimization...");
                
                await Task.Delay(500, cancellationToken);

                var steamPath = !string.IsNullOrEmpty(config.SteamPath) ? config.SteamPath : FindSteamInstallation();

                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    return new OptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "Steam is not installed or the specified path is invalid."
                    };
                }

                OnProgressChanged("Steam found at: " + steamPath);
                await Task.Delay(300, cancellationToken);
                
                OnProgressChanged("Configuration: CreateConfig=" + config.CreateSteamConfig + ", CreateShortcut=" + config.CreateDesktopShortcut + ", RemoveStartup=" + config.RemoveFromStartup);
                await Task.Delay(300, cancellationToken);

                // Steam shutdown logic removed as we now block optimization if Steam is running


                if (config.ClearDownloadCache)
                {
                    OnProgressChanged("Clearing download cache...");
                    await ClearDownloadCacheAsync(steamPath, cancellationToken);
                }

                if (config.UpdateSteam)
                {
                    OnProgressChanged("Updating Steam for " + config.Mode + " mode...");
                    await UpdateSteamAsync(steamPath, config.Mode, cancellationToken);
                    
                    await Task.Delay(3000, cancellationToken);
                }

                OnProgressChanged("Applying optimization configuration...");
                await CreateConfigurationFilesAsync(steamPath, config, cancellationToken);

                if (config.RemoveFromStartup)
                {
                    OnProgressChanged("Managing Windows startup entries...");
                    await ManageStartupEntriesAsync();
                }

                OnProgressChanged("Optimization completed");
                
                var result = new OptimizationResult { Success = true };
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

        private async Task ClearDownloadCacheAsync(string steamPath, CancellationToken cancellationToken)
        {
            try
            {
                var downloadCachePath = Path.Combine(steamPath, "appcache", "downloading");
                if (Directory.Exists(downloadCachePath))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            var files = Directory.GetFiles(downloadCachePath, "*", SearchOption.AllDirectories);
                            int clearedCount = 0;
                            foreach (var file in files)
                            {
                                try
                                {
                                    File.Delete(file);
                                    clearedCount++;
                                }
                                catch { }
                            }
                            OnProgressChanged(string.Format("Cleared {0} cache file(s)", clearedCount));
                        }
                        catch
                        {
                            OnProgressChanged("Warning: Could not clear all cache files");
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged("Warning: Error clearing cache: " + ex.Message);
            }
        }




        private async Task UpdateSteamAsync(string steamPath, string mode, CancellationToken cancellationToken)
        {
            try
            {
                string steamExe = Path.Combine(steamPath, "steam.exe");

                if (!File.Exists(steamExe))
                {
                    OnProgressChanged("Steam executable not found at " + steamPath);
                    return;
                }

                var modeKey = mode.ToLower();
                string arguments;

                if (mode.StartsWith("Experimental_"))
                {
                    string waybackDate = mode.Replace("Experimental_", "");
                    arguments = string.Format("-forcesteamupdate -forcepackagedownload -overridepackageurl http://web.archive.org/web/{0}if_/media.steampowered.com/client -exitsteam", waybackDate);
                    OnProgressChanged("Updating to experimental version " + waybackDate + "...");
                }
                else if (modeKey == "normal2023june")
                {
                    arguments = "-forcesteamupdate -forcepackagedownload -overridepackageurl http://web.archive.org/web/20230615094110if_/media.steampowered.com/client -exitsteam";
                }
                else if (modeKey == "normal2022dec" || modeKey == "lite2022dec")
                {
                    arguments = "-forcesteamupdate -forcepackagedownload -overridepackageurl https://archive.org/download/dec2022steam -exitsteam";
                }
                else
                {
                    arguments = "-forcesteamupdate -forcepackagedownload -exitsteam";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = steamPath,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    LoadUserProfile = true
                };

                OnProgressChanged("Starting Steam update for " + mode + " mode...");

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                }

                var timeout = DateTime.Now.AddMinutes(10);
                bool updateInProgress = true;
                int checkCount = 0;

                while (DateTime.Now < timeout && updateInProgress && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(3000, cancellationToken);
                    checkCount++;

                    var steamProcesses = Process.GetProcessesByName("steam");

                    if (steamProcesses.Length == 0)
                    {
                        OnProgressChanged("Steam update completed for " + mode);
                        updateInProgress = false;
                    }
                    else
                    {
                        if (checkCount % 3 == 0)
                        {
                            OnProgressChanged("Steam updating for " + mode + " mode... Please wait");
                        }
                        foreach (var proc in steamProcesses)
                        {
                            proc.Dispose();
                        }
                    }
                }

                if (updateInProgress)
                {
                    OnProgressChanged("Finalizing update...");
                    // Steam shutdown logic removed
                }

                OnProgressChanged("Steam downgrade completed successfully");
            }
            catch (Exception ex)
            {
                OnProgressChanged("Error during update: " + ex.Message);
            }
        }

        private Task CreateConfigurationFilesAsync(string steamPath, OptimizationConfig config, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (config.CreateSteamConfig)
                    {
                        try
                        {
                            var configPath = Path.Combine(steamPath, "steam.cfg");
                            var configContent = string.Format(@"BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
:: Mode: {0}
:: Version: 2.1125.1700", config.Mode);

                            File.WriteAllText(configPath, configContent);
                            OnProgressChanged("Created steam.cfg at: " + configPath);
                            
                            if (File.Exists(configPath))
                            {
                                OnProgressChanged("Verified steam.cfg exists");
                            }
                            else
                            {
                                OnProgressChanged("Warning: steam.cfg was not created");
                            }
                        }
                        catch (Exception ex)
                        {
                            OnProgressChanged("Error creating steam.cfg: " + ex.Message);
                        }
                    }

                    if (config.CreateDesktopShortcut)
                    {
                        try
                        {
                            string steamArgs = "";
                            string year = "";

                            if (config.Mode.StartsWith("Experimental_"))
                            {
                                string waybackDate = config.Mode.Replace("Experimental_", "");
                                year = waybackDate.Substring(0, 4);
                                steamArgs = _steamModes.ContainsKey(year) ? _steamModes[year] : _steamModes["2023"];
                            }
                            else
                            {
                                var modeKey = config.Mode.ToLower();
                                steamArgs = _steamModes.ContainsKey(modeKey) ? _steamModes[modeKey] : "";
                            }

                            string scriptContent = string.Format(@"@echo off
cd /d ""{0}""
start Steam.exe {1}
:: Mode: {2}", steamPath, steamArgs, config.Mode);

                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string desktopScriptPath = Path.Combine(desktopPath, "Steam.bat");

                            File.WriteAllText(desktopScriptPath, scriptContent);
                            OnProgressChanged("Created Steam.bat at: " + desktopScriptPath);
                            
                            if (File.Exists(desktopScriptPath))
                            {
                                OnProgressChanged("Verified Steam.bat exists");
                            }
                            else
                            {
                                OnProgressChanged("Warning: Steam.bat was not created");
                            }
                        }
                        catch (Exception ex)
                        {
                            OnProgressChanged("Error creating Steam.bat: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnProgressChanged("Error in CreateConfigurationFilesAsync: " + ex.Message);
                }
            }, cancellationToken);
        }

        private Task ManageStartupEntriesAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    const string STEAM_KEY = "Steam";
                    bool foundEntry = false;
                    var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                    
                    OnProgressChanged("Checking startup entries...");
                    
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                        {
                            if (key != null)
                            {
                                var value = key.GetValue(STEAM_KEY);
                                if (value != null)
                                {
                                    string valuePath = value.ToString();
                                    
                                    // Extensive validation before modification
                                    if (valuePath.IndexOf("steam.exe", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        // Validate that the path points to a real Steam executable
                                        bool isValidSteam = false;
                                        try
                                        {
                                            // Extract executable path from registry value
                                            string exePath = valuePath.Trim('"').Split(' ')[0];
                                            if (File.Exists(exePath))
                                            {
                                                var fileInfo = new FileInfo(exePath);
                                                // Steam.exe is typically > 1MB, validate size
                                                if (fileInfo.Length > 1000000)
                                                {
                                                    isValidSteam = true;
                                                    OnProgressChanged($"Validated: {Path.GetFileName(exePath)} ({fileInfo.Length / 1024 / 1024}MB)");
                                                }
                                            }
                                        }
                                        catch { }
                                        
                                        if (isValidSteam)
                                        {
                                        foundEntry = true;
                                        string backupName = STEAM_KEY + "_Backup_" + DateTime.Now.ToString("yyyyMMdd");
                                        
                                        // Create backup before modification
                                        try
                                        {
                                            key.SetValue(backupName, valuePath);
                                            OnProgressChanged($"Created backup: {backupName}");
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            OnProgressChanged("Warning: Insufficient permissions for backup");
                                        }
                                        catch (Exception ex)
                                        {
                                            OnProgressChanged($"Backup warning: {ex.Message}");
                                        }
                                        
                                        // Clear startup entry (SetValue instead of DeleteValue - less suspicious)
                                        key.SetValue(STEAM_KEY, "", RegistryValueKind.String);
                                        OnProgressChanged("Disabled Steam startup entry (value cleared)");
                                    }
                                }
                                }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(registryPath, true))
                        {
                            if (key != null)
                            {
                                var value = key.GetValue(STEAM_KEY);
                                if (value != null)
                                {
                                    string valuePath = value.ToString();
                                    
                                    // Extensive validation before modification
                                    if (valuePath.IndexOf("steam.exe", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        // Validate that the path points to a real Steam executable
                                        bool isValidSteam = false;
                                        try
                                        {
                                            // Extract executable path from registry value
                                            string exePath = valuePath.Trim('"').Split(' ')[0];
                                            if (File.Exists(exePath))
                                            {
                                                var fileInfo = new FileInfo(exePath);
                                                // Steam.exe is typically > 1MB, validate size
                                                if (fileInfo.Length > 1000000)
                                                {
                                                    isValidSteam = true;
                                                    OnProgressChanged($"Validated system: {Path.GetFileName(exePath)} ({fileInfo.Length / 1024 / 1024}MB)");
                                                }
                                            }
                                        }
                                        catch { }
                                        
                                        if (isValidSteam)
                                        {
                                        foundEntry = true;
                                        string backupName = STEAM_KEY + "_Backup_" + DateTime.Now.ToString("yyyyMMdd");
                                        
                                        // Create backup before modification
                                        try
                                        {
                                            key.SetValue(backupName, valuePath);
                                            OnProgressChanged($"Created system backup: {backupName}");
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            OnProgressChanged("Warning: Insufficient permissions for system backup");
                                        }
                                        catch (Exception ex)
                                        {
                                            OnProgressChanged($"System backup warning: {ex.Message}");
                                        }
                                        
                                        // Clear startup entry (SetValue instead of DeleteValue - less suspicious)
                                        key.SetValue(STEAM_KEY, "", RegistryValueKind.String);
                                        OnProgressChanged("Disabled system startup entry (value cleared)");
                                    }
                                }
                                }
                            }
                        }
                    }
                    catch { }

                    if (!foundEntry)
                    {
                        OnProgressChanged("No startup entries found");
                    }
                }
                catch (Exception ex)
                {
                    OnProgressChanged("Note: " + ex.Message);
                }
            });
        }

        public bool IsRunningAsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void OnProgressChanged(string status)
        {
            try
            {
                if (ProgressChanged != null)
                    ProgressChanged.Invoke(status);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_steamDetectionTimer != null)
            {
                _steamDetectionTimer.Dispose();
                _steamDetectionTimer = null;
            }
        }
    }
}
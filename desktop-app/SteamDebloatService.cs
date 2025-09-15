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
    public class SteamDebloatService
    {
        private readonly Dictionary<string, string> _steamModes;
        private const string DefaultSteamPath = @"C:\Program Files (x86)\Steam";
        private const string DefaultSteamV2Path = @"C:\Program Files (x86)\Steamv2";
        private Timer? _steamDetectionTimer;

        public event Action<string>? ProgressChanged;
        public event Action<bool>? SteamDetectionChanged;

        public SteamDebloatService()
        {
            _steamModes = new Dictionary<string, string>
            {
                ["normal2025july"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["normal2022dec"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["lite2022dec"] = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
            };
            
            StartSteamDetectionTimer();
        }

        private void StartSteamDetectionTimer()
        {
            _steamDetectionTimer = new Timer(CheckSteamInstallation, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void CheckSteamInstallation(object? state)
        {
            var steamFound = !string.IsNullOrEmpty(FindSteamInstallation());
            SteamDetectionChanged?.Invoke(steamFound);
        }

        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                var osVersion = Environment.OSVersion.VersionString;
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
            });
        }

        public async Task<OptimizationResult> OptimizeSteamAsync(OptimizationConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                OnProgressChanged("Starting optimization...");
                
                var steamPath = FindSteamInstallation();
                if (string.IsNullOrEmpty(steamPath))
                {
                    return new OptimizationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Steam is not installed. Please install Steam from https://store.steampowered.com/about/ and try again." 
                    };
                }

                OnProgressChanged($"Steam found at: {steamPath}");
                
                await StopSteamProcessesAsync();
                OnProgressChanged("Steam processes stopped");

                if (config.Mode == "NormalBoth2022-2025")
                {
                    var result = await ProcessBothVersionsMode(config, cancellationToken, steamPath);
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }
                else
                {
                    var result = await ProcessSingleVersionMode(config, cancellationToken, steamPath);
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }
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

        public async Task<OptimizationResult> UninstallAsync(CancellationToken cancellationToken)
        {
            try
            {
                OnProgressChanged("Starting uninstallation...");
                
                var steamPath = FindSteamInstallation();
                if (string.IsNullOrEmpty(steamPath))
                {
                    return new OptimizationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Steam is not installed. Cannot proceed with uninstallation." 
                    };
                }
                
                await StopSteamProcessesAsync();
                OnProgressChanged("Steam processes stopped");

                bool hasSteam = Directory.Exists(DefaultSteamPath);
                bool hasSteamV2 = Directory.Exists(DefaultSteamV2Path);

                RemoveDesktopShortcuts();
                OnProgressChanged("Shortcuts removed");

                RemoveStartMenuShortcuts();

                if (hasSteam)
                {
                    OnProgressChanged("Removing Steam optimization configurations...");
                    RemoveOptimizationConfigs(DefaultSteamPath);
                }
                
                if (hasSteamV2)
                {
                    OnProgressChanged("Removing Steam V2 optimization configurations...");
                    RemoveOptimizationConfigs(DefaultSteamV2Path);
                    
                    try
                    {
                        Directory.Delete(DefaultSteamV2Path, true);
                        OnProgressChanged("Steam V2 directory removed");
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not remove Steam V2 directory: {ex.Message}");
                    }
                }

                OnProgressChanged("Uninstallation completed - Steam restored to clean state");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        
        private void RemoveOptimizationConfigs(string steamPath)
        {
            try
            {
                var configPath = Path.Combine(steamPath, "steam.cfg");
                if (File.Exists(configPath))
                {
                    var content = File.ReadAllText(configPath);
                    if (content.Contains("BootStrapperInhibitAll=enable") || content.Contains("Mode:"))
                    {
                        File.Delete(configPath);
                        OnProgressChanged("Removed optimization config file");
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Could not remove config from {steamPath}: {ex.Message}");
            }
        }

        private void RemoveDesktopShortcuts()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string[] shortcuts = { "Steam.bat", "Steam2025.bat", "Steam2022.bat" };
            
            foreach (string shortcut in shortcuts)
            {
                string path = Path.Combine(desktopPath, shortcut);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private void RemoveStartMenuShortcuts()
        {
            string startMenuPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs", "Steam");
            
            if (Directory.Exists(startMenuPath))
                Directory.Delete(startMenuPath, true);
        }

        private async Task<OptimizationResult> ProcessSingleVersionMode(OptimizationConfig config, CancellationToken cancellationToken, string steamPath)
        {
            OnProgressChanged("Verifying Steam installation...");
            
            if (config.UpdateSteam)
            {
                OnProgressChanged($"Updating Steam for {config.Mode} mode...");
                await UpdateSteamAsync(steamPath, config.Mode, cancellationToken);
            }

            OnProgressChanged("Applying optimization configuration...");
            await CreateConfigurationFilesAsync(steamPath, config, cancellationToken);
            
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

        private async Task<OptimizationResult> ProcessBothVersionsMode(OptimizationConfig config, CancellationToken cancellationToken, string existingSteamPath)
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
                    await CopyDirectoryAsync(existingSteamPath, steam2022Path, cancellationToken);
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
            await UpdateSteamAsync(steam2025Path, "Normal2025July", cancellationToken);

            OnProgressChanged("Updating Steam 2022...");
            await UpdateSteamAsync(steam2022Path, "Normal2022dec", cancellationToken);

            OnProgressChanged("Applying configurations...");
            
            await CreateConfigurationFilesAsync(steam2025Path, new OptimizationConfig { Mode = "Normal2025July" }, cancellationToken);
            await CreateConfigurationFilesAsync(steam2022Path, new OptimizationConfig { Mode = "Normal2022dec" }, cancellationToken);

            if (config.CreateDesktopShortcut)
            {
                OnProgressChanged("Creating desktop shortcuts...");
                CreateDesktopShortcut(steam2025Path, "Normal2025July", "Steam2025.bat");
                CreateDesktopShortcut(steam2022Path, "Normal2022dec", "Steam2022.bat");
            }

            if (config.CreateStartMenuShortcut)
            {
                OnProgressChanged("Creating start menu shortcuts...");
                CreateStartMenuShortcut(steam2025Path, "Normal2025July", "Steam2025.bat");
                CreateStartMenuShortcut(steam2022Path, "Normal2022dec", "Steam2022.bat");
            }

            if (config.RemoveFromStartup)
            {
                RemoveSteamFromStartup();
            }

            OnProgressChanged("Optimization completed");
            return new OptimizationResult { Success = true };
        }

        private async Task StopSteamProcessesAsync()
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
            });
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
                
                using var process = Process.Start(startInfo);
                
                await Task.Delay(3000, cancellationToken);
                
                var timeout = DateTime.Now.AddMinutes(10);
                bool updateInProgress = true;
                
                while (DateTime.Now < timeout && updateInProgress && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                    
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
                    await StopSteamProcessesAsync();
                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error during update: {ex.Message}");
            }
        }

        private async Task CreateConfigurationFilesAsync(string steamPath, OptimizationConfig config, CancellationToken cancellationToken)
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
                    steamArgs = steamPath.Contains("Steamv2") ? _steamModes["normal2022dec"] : _steamModes["normal2025july"];
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
            }, cancellationToken);
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
                // Ignore errors
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
                // Ignore errors
            }
        }

        private void RemoveSteamFromStartup()
        {
            try
            {
                var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using var key = Registry.CurrentUser.OpenSubKey(registryPath, true);
                
                if (key?.GetValue("Steam") != null)
                {
                    key.DeleteValue("Steam");
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private string? FindSteamInstallation()
        {
            var commonPaths = new[]
            {
                DefaultSteamPath,
                @"C:\Steam",
                @"D:\Steam", 
                @"E:\Steam",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                {
                    return path;
                }
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam") ??
                               Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
                
                if (key?.GetValue("InstallPath") is string registryPath && 
                    Directory.Exists(registryPath) && 
                    File.Exists(Path.Combine(registryPath, "steam.exe")))
                {
                    return registryPath;
                }
            }
            catch { }

            return null;
        }

        public string GetSteamPath()
        {
            return FindSteamInstallation() ?? DefaultSteamPath;
        }

        public bool IsRunningAsAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
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

        private async Task CopyDirectoryAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                return;

            await Task.Run(() => CopyDirectory(sourcePath, destinationPath), cancellationToken);
        }

        private void OnProgressChanged(string status)
        {
            ProgressChanged?.Invoke(status);
        }

        public void Dispose()
        {
            _steamDetectionTimer?.Dispose();
        }
    }
}
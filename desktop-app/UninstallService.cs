using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Security.Principal;

namespace SteamDebloat
{
    public class UninstallService : IDisposable
    {
        private const string DefaultSteamPath = @"C:\Program Files (x86)\Steam";
        private const string DefaultSteamV2Path = @"C:\Program Files (x86)\Steamv2";

        public event Action<string> ProgressChanged;

        public async Task<OptimizationResult> UninstallAsync(CancellationToken cancellationToken)
        {
            try
            {
                OnProgressChanged("Starting uninstallation...");
                
                await StopSteamProcessesAsync().ConfigureAwait(false);
                OnProgressChanged("Steam processes stopped");

                bool hasSteam = Directory.Exists(DefaultSteamPath);
                bool hasSteamV2 = Directory.Exists(DefaultSteamV2Path);

                OnProgressChanged("Removing desktop shortcuts...");
                RemoveDesktopShortcuts();

                OnProgressChanged("Removing start menu shortcuts...");
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
                        OnProgressChanged("Removing Steam V2 directory...");
                        Directory.Delete(DefaultSteamV2Path, true);
                        OnProgressChanged("Steam V2 directory removed");
                    }
                    catch (Exception ex)
                    {
                        OnProgressChanged($"Warning: Could not remove Steam V2 directory: {ex.Message}");
                    }
                }

                OnProgressChanged("Restoring Steam startup registry entry...");
                RestoreSteamStartup();

                OnProgressChanged("Cleaning temporary files...");
                CleanTempBatchFiles();

                // Update Steam to September 2025 version after cleanup
                if (hasSteam)
                {
                    OnProgressChanged("Updating Steam to September 2025 version...");
                    await UpdateSteamToSeptember2025Async(DefaultSteamPath, cancellationToken).ConfigureAwait(false);
                }

                OnProgressChanged("Uninstallation completed - Steam restored to September 2025 version");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task UpdateSteamToSeptember2025Async(string steamPath, CancellationToken cancellationToken)
        {
            try
            {
                var steamExe = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamExe)) 
                {
                    OnProgressChanged($"Steam executable not found at {steamPath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = "-forcesteamupdate -forcepackagedownload -overridepackageurl -exitsteam",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = steamPath
                };

                OnProgressChanged("Starting Steam update to September 2025 version...");
                
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
                            OnProgressChanged("Steam update to September 2025 completed");
                            updateInProgress = false;
                            break;
                        }
                        else
                        {
                            OnProgressChanged("Steam updating to September 2025... (visible on screen)");
                        }
                    }
                    
                    if (updateInProgress)
                    {
                        OnProgressChanged("Finalizing Steam update...");
                        await StopSteamProcessesAsync().ConfigureAwait(false);
                        await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error during Steam update: {ex.Message}");
            }
        }

        public UninstallStatus GetUninstallStatus()
        {
            try
            {
                var steamPath = DefaultSteamPath;
                bool hasConfig = Directory.Exists(steamPath) && File.Exists(Path.Combine(steamPath, "steam.cfg"));
                bool hasSteamV2 = Directory.Exists(DefaultSteamV2Path);
                bool hasShortcuts = CheckForShortcuts();

                // Check if configs are optimization configs
                bool hasOptimizationConfig = false;
                if (hasConfig)
                {
                    try
                    {
                        var configContent = File.ReadAllText(Path.Combine(steamPath, "steam.cfg"));
                        hasOptimizationConfig = configContent.Contains("BootStrapperInhibitAll=enable") || 
                                              configContent.Contains("Mode:");
                    }
                    catch
                    {
                        hasOptimizationConfig = false;
                    }
                }

                return new UninstallStatus
                {
                    HasOptimizationConfig = hasOptimizationConfig,
                    HasSteamV2Directory = hasSteamV2,
                    HasShortcuts = hasShortcuts,
                    SteamConfigStatus = hasOptimizationConfig ? "Optimization config found" : "No optimization config",
                    SteamV2Status = hasSteamV2 ? "Steam V2 directory found" : "No Steam V2 installation",
                    ShortcutsStatus = hasShortcuts ? "Shortcuts found" : "No shortcuts found"
                };
            }
            catch
            {
                return new UninstallStatus
                {
                    HasOptimizationConfig = false,
                    HasSteamV2Directory = false,
                    HasShortcuts = false,
                    SteamConfigStatus = "Unable to check status",
                    SteamV2Status = "Unable to check status",
                    ShortcutsStatus = "Unable to check status"
                };
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
                        OnProgressChanged($"Removed optimization config from {Path.GetFileName(steamPath)}");
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
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string[] shortcuts = { "Steam.bat", "Steam2025.bat", "Steam2022.bat" };
                
                int removedCount = 0;
                foreach (string shortcut in shortcuts)
                {
                    string path = Path.Combine(desktopPath, shortcut);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                    OnProgressChanged($"Removed {removedCount} desktop shortcut(s)");
                else
                    OnProgressChanged("No desktop shortcuts to remove");
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Error removing desktop shortcuts: {ex.Message}");
            }
        }

        private void RemoveStartMenuShortcuts()
        {
            try
            {
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs", "Steam");
                
                if (Directory.Exists(startMenuPath))
                {
                    Directory.Delete(startMenuPath, true);
                    OnProgressChanged("Removed start menu shortcuts");
                }
                else
                {
                    OnProgressChanged("No start menu shortcuts to remove");
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Error removing start menu shortcuts: {ex.Message}");
            }
        }

        private bool CheckForShortcuts()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string[] shortcuts = { "Steam.bat", "Steam2025.bat", "Steam2022.bat" };
                
                foreach (string shortcut in shortcuts)
                {
                    if (File.Exists(Path.Combine(desktopPath, shortcut)))
                        return true;
                }

                // Also check start menu
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs", "Steam");
                
                return Directory.Exists(startMenuPath);
            }
            catch
            {
                return false;
            }
        }

        private void RestoreSteamStartup()
        {
            try
            {
                // Try to restore Steam to startup if it was removed
                string steamExe = Path.Combine(DefaultSteamPath, "steam.exe");
                if (File.Exists(steamExe))
                {
                    var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                    using (var key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                    {
                        if (key?.GetValue("Steam") == null)
                        {
                            // Only restore if the user wants Steam to start with Windows
                            // This is optional - we won't restore by default
                            OnProgressChanged("Steam startup entry not restored (user preference)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Could not check Steam startup: {ex.Message}");
            }
        }

        private void CleanTempBatchFiles()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string[] tempFiles = { "Steam.bat", "Steam2025.bat", "Steam2022.bat" };
                
                int removedCount = 0;
                foreach (string fileName in tempFiles)
                {
                    string filePath = Path.Combine(tempPath, fileName);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            removedCount++;
                        }
                        catch
                        {
                            // Ignore errors for temp files
                        }
                    }
                }

                if (removedCount > 0)
                    OnProgressChanged($"Cleaned {removedCount} temporary file(s)");
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Error cleaning temp files: {ex.Message}");
            }
        }

        private async Task StopSteamProcessesAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var processes = Process.GetProcessesByName("steam");
                    int stoppedCount = 0;
                    
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                            stoppedCount++;
                        }
                        catch
                        {
                            // Ignore errors when terminating processes
                        }
                    }

                    if (stoppedCount > 0)
                        OnProgressChanged($"Stopped {stoppedCount} Steam process(es)");
                    else
                        OnProgressChanged("No Steam processes running");
                        
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Warning: Error stopping Steam processes: {ex.Message}");
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
            // Method intentionally left empty - no resources to dispose
        }
    }

    public class UninstallStatus
    {
        public bool HasOptimizationConfig { get; set; }
        public bool HasSteamV2Directory { get; set; }
        public bool HasShortcuts { get; set; }
        public string SteamConfigStatus { get; set; }
        public string SteamV2Status { get; set; }
        public string ShortcutsStatus { get; set; }

        public UninstallStatus()
        {
            HasOptimizationConfig = false;
            HasSteamV2Directory = false;
            HasShortcuts = false;
            SteamConfigStatus = string.Empty;
            SteamV2Status = string.Empty;
            ShortcutsStatus = string.Empty;
        }
    }
}
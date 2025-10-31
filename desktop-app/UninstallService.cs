using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SteamDebloat
{
    public class UninstallService : IDisposable
    {
        private readonly string _defaultSteamPath;

        public event Action<string> ProgressChanged;

        public UninstallService()
        {
            _defaultSteamPath = @"C:\Program Files (x86)\Steam";
        }

        public async Task<OptimizationResult> UninstallAsync(CancellationToken cancellationToken)
        {
            try
            {
                OnProgressChanged("Starting uninstallation...");

                await StopSteamProcessesAsync();
                OnProgressChanged("Steam processes stopped");

                bool hasSteam = Directory.Exists(_defaultSteamPath);

                if (hasSteam)
                {
                    OnProgressChanged("Removing Steam optimization configurations...");
                    RemoveOptimizationConfigs(_defaultSteamPath);
                }

                OnProgressChanged("Restoring Steam startup registry entry...");
                RestoreSteamStartup();

                OnProgressChanged("Cleaning temporary files...");
                CleanTempBatchFiles();

                if (hasSteam)
                {
                    OnProgressChanged("Updating Steam to October 2025 version...");
                    await UpdateSteamToOctober2025Async(_defaultSteamPath, cancellationToken);
                }

                OnProgressChanged("Uninstallation completed - Steam restored to October 2025 version");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task UpdateSteamToOctober2025Async(string steamPath, CancellationToken cancellationToken)
        {
            try
            {
                string steamExe = Path.Combine(steamPath, "steam.exe");

                if (!File.Exists(steamExe))
                {
                    OnProgressChanged($"Steam executable not found at {steamPath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = "-forcesteamupdate -forcepackagedownload -exitsteam",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = steamPath
                };

                OnProgressChanged("Starting Steam update to October 2025 version...");

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        await Task.Delay(3000, cancellationToken);
                    }
                }

                var timeout = DateTime.Now.AddMinutes(10);
                bool updateInProgress = true;

                while (DateTime.Now < timeout && updateInProgress && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);

                    var steamProcesses = Process.GetProcessesByName("steam");

                    if (steamProcesses.Length == 0)
                    {
                        OnProgressChanged("Steam update to October 2025 completed");
                        updateInProgress = false;
                    }
                    else
                    {
                        OnProgressChanged("Steam updating to October 2025...");
                        foreach (var proc in steamProcesses)
                        {
                            proc.Dispose();
                        }
                    }
                }

                if (updateInProgress)
                {
                    OnProgressChanged("Finalizing Steam update...");
                    await StopSteamProcessesAsync();
                    await Task.Delay(2000, cancellationToken);
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
                var steamPath = _defaultSteamPath;
                bool hasConfig = Directory.Exists(steamPath) && File.Exists(Path.Combine(steamPath, "steam.cfg"));

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
                    SteamConfigStatus = hasOptimizationConfig ? "Optimization config found" : "No optimization config"
                };
            }
            catch
            {
                return new UninstallStatus
                {
                    HasOptimizationConfig = false,
                    SteamConfigStatus = "Unable to check status"
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

        private void RestoreSteamStartup()
        {
            try
            {
                string steamExe = Path.Combine(_defaultSteamPath, "steam.exe");
                if (File.Exists(steamExe))
                {
                    OnProgressChanged("Steam startup entry not restored (user preference)");
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
                string[] tempFiles = { "Steam.bat", "Steam2025.bat", "Steam2022.bat", "Steam2023.bat" };

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
                        catch { }
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

        private Task StopSteamProcessesAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var processNames = new[] { "steam", "steamwebhelper" };
                    int stoppedCount = 0;

                    foreach (var name in processNames)
                    {
                        var processes = Process.GetProcessesByName(name);
                        foreach (var process in processes)
                        {
                            try
                            {
                                process.Kill();
                                process.WaitForExit(5000);
                                process.Dispose();
                                stoppedCount++;
                            }
                            catch { }
                        }
                    }

                    if (stoppedCount > 0)
                        OnProgressChanged($"Stopped {stoppedCount} Steam process(es)");
                    else
                        OnProgressChanged("No Steam processes running");
                }
                catch (Exception ex)
                {
                    OnProgressChanged($"Warning: Error stopping Steam processes: {ex.Message}");
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
                ProgressChanged?.Invoke(status);
            }
            catch { }
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
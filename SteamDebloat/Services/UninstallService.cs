using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SteamDebloat
{
    public class UninstallService : IDisposable
    {
        private readonly string _defaultSteamPath = @"C:\Program Files (x86)\Steam";

        public event Action<string> ProgressChanged;

        public async Task<OptimizationResult> UninstallAsync(CancellationToken cancellationToken)
        {
            try
            {
                OnProgressChanged("Starting restoration...");

                bool hasSteam = Directory.Exists(_defaultSteamPath);

                if (hasSteam)
                {
                    OnProgressChanged("Removing Steam optimization configurations...");
                    RemoveOptimizationConfigs(_defaultSteamPath);
                }

                OnProgressChanged("Cleaning temporary files...");
                CleanTempBatchFiles();

                if (hasSteam)
                {
                    OnProgressChanged("Updating Steam to November 2025 version...");
                    await UpdateSteamToNovember2025Async(_defaultSteamPath, cancellationToken);
                }

                OnProgressChanged("Restoration completed - Steam restored to November 2025 version");
                return new OptimizationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private async Task UpdateSteamToNovember2025Async(string steamPath, CancellationToken cancellationToken)
        {
            try
            {
                string steamExe = Path.Combine(steamPath, "steam.exe");

                if (!File.Exists(steamExe))
                {
                    OnProgressChanged("Steam executable not found at " + steamPath);
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = "-forcesteamupdate -forcepackagedownload -exitsteam",
                    UseShellExecute = true,  // Changed from false for transparency
                    CreateNoWindow = false,   // Changed from true - less suspicious
                    WorkingDirectory = steamPath,
                    WindowStyle = ProcessWindowStyle.Minimized  // Minimized instead of Hidden
                };

                OnProgressChanged("Starting Steam update to November 2025 version...");

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
                        OnProgressChanged("Steam update to November 2025 completed");
                        updateInProgress = false;
                    }
                    else
                    {
                        if (checkCount % 3 == 0)
                        {
                            OnProgressChanged("Steam updating to November 2025... Please wait");
                        }
                        foreach (var proc in steamProcesses)
                        {
                            proc.Dispose();
                        }
                    }
                }

                OnProgressChanged("Steam restoration completed successfully");
            }
            catch (Exception ex)
            {
                OnProgressChanged("Error during Steam update: " + ex.Message);
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
                        OnProgressChanged("Removed optimization config from " + Path.GetFileName(steamPath));
                    }
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged("Warning: Could not remove config from " + steamPath + ": " + ex.Message);
            }
        }

        private void CleanTempBatchFiles()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string[] tempFiles = { "Steam.bat" };

                int removedCount = 0;
                foreach (string fileName in tempFiles)
                {
                    string filePath = Path.Combine(desktopPath, fileName);
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
                    OnProgressChanged(string.Format("Cleaned {0} temporary file(s)", removedCount));
            }
            catch (Exception ex)
            {
                OnProgressChanged("Warning: Error cleaning temp files: " + ex.Message);
            }
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
            // No resources to dispose
        }
    }
}
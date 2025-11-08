using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

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

            // Launch parameters by mode
            _steamModes = new Dictionary<string, string>
            {
                ["normal2025october"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["normal2023june"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad steam://open/library",
                ["normal2022dec"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                ["lite2022dec"] = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
                
                // Parameters by year for experimental versions
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
                SteamDetectionChanged?.Invoke(steamFound);
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
                if (Environment.OSVersion.Version.Major >= 10)
                {
                    if (Environment.OSVersion.Version.Build >= 22000)
                        return "Windows 11";
                    return "Windows 10";
                }
                if (Environment.OSVersion.Version.Major == 6)
                {
                    if (Environment.OSVersion.Version.Minor == 3)
                        return "Windows 8.1";
                    if (Environment.OSVersion.Version.Minor == 2)
                        return "Windows 8";
                    if (Environment.OSVersion.Version.Minor == 1)
                        return "Windows 7";
                }

                return Environment.OSVersion.VersionString;
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
                            string installPath = key.GetValue("InstallPath")?.ToString();
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

                var steamPath = !string.IsNullOrEmpty(config.SteamPath) ? config.SteamPath : FindSteamInstallation();

                if (string.IsNullOrEmpty(steamPath) || !IsValidSteamDirectory(steamPath))
                {
                    return new OptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "Steam is not installed or the specified path is invalid."
                    };
                }

                OnProgressChanged($"Steam found at: {steamPath}");

                await StopSteamProcessesAsync();
                OnProgressChanged("Steam processes stopped");

                var result = await ProcessSingleVersionMode(config, cancellationToken, steamPath);

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

                bool isOctober2025 = config.Mode.Equals("Normal2025October", StringComparison.OrdinalIgnoreCase);
                
                // ONLY downgrade if UpdateSteam is checked AND it's NOT October 2025
                if (config.UpdateSteam && !isOctober2025)
                {
                    OnProgressChanged($"Downgrading Steam to {config.Mode}...");
                    await UpdateSteamAsync(steamPath, config.Mode, cancellationToken);
                }
                else if (isOctober2025)
                {
                    OnProgressChanged("Using current Steam version (October 2025 - Latest)");
                }
                else
                {
                    OnProgressChanged("Skipping Steam downgrade (using current version)");
                }

                OnProgressChanged("Applying optimization configuration...");
                await CreateConfigurationFilesAsync(steamPath, config, cancellationToken);

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

        private Task StopSteamProcessesAsync()
        {
            return Task.Run(() =>
            {
                var processNames = new[] { "steam", "steamwebhelper" };
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
                        }
                        catch { }
                    }
                }
            });
        }

        private async Task UpdateSteamAsync(string steamPath, string mode, CancellationToken cancellationToken)
        {
            try
            {
                string steamExe = Path.Combine(steamPath, "steam.exe");

                if (!File.Exists(steamExe))
                {
                    OnProgressChanged($"Steam executable not found at {steamPath}");
                    return;
                }

                var modeKey = mode.ToLower();
                string arguments;

                // Check if it's an experimental version first
                if (mode.StartsWith("Experimental_"))
                {
                    string waybackDate = mode.Replace("Experimental_", "");
                    arguments = $"-forcesteamupdate -forcepackagedownload -overridepackageurl http://web.archive.org/web/{waybackDate}if_/media.steampowered.com/client -exitsteam";
                    OnProgressChanged($"Downgrading to experimental version {waybackDate}...");
                }
                else if (modeKey == "normal2023june")
                {
                    arguments = "-forcesteamupdate -forcepackagedownload -overridepackageurl http://web.archive.org/web/20230615094110if_/media.steampowered.com/client -exitsteam";
                    OnProgressChanged("Downgrading to Normal 2023 June...");
                }
                else if (modeKey == "normal2022dec" || modeKey == "lite2022dec")
                {
                    arguments = "-forcesteamupdate -forcepackagedownload -overridepackageurl https://archive.org/download/dec2022steam -exitsteam";
                    OnProgressChanged($"Downgrading to {mode}...");
                }
                else
                {
                    // Unknown mode - no downgrade
                    OnProgressChanged($"Warning: Unknown mode {mode}, skipping downgrade");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = steamPath
                };

                OnProgressChanged($"Starting Steam downgrade process...");

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
                        OnProgressChanged($"Steam downgrade to {mode} completed");
                        updateInProgress = false;
                    }
                    else
                    {
                        OnProgressChanged($"Downgrading Steam to {mode}...");
                        foreach (var proc in steamProcesses)
                        {
                            proc.Dispose();
                        }
                    }
                }

                if (updateInProgress)
                {
                    OnProgressChanged("Finalizing downgrade...");
                    await StopSteamProcessesAsync();
                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Error during downgrade: {ex.Message}");
            }
        }

        private Task CreateConfigurationFilesAsync(string steamPath, OptimizationConfig config, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var configPath = Path.Combine(steamPath, "steam.cfg");
                var configContent = $@"BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
:: Mode: {config.Mode}
:: Version: 1.1102.0400";

                File.WriteAllText(configPath, configContent);

                string steamArgs = "";

                // Determine parameters according to mode
                if (config.Mode.StartsWith("Experimental_"))
                {
                    string waybackDate = config.Mode.Replace("Experimental_", "");
                    string year = waybackDate.Substring(0, 4);
                    steamArgs = _steamModes.ContainsKey(year) ? _steamModes[year] : _steamModes["2023"];
                }
                else
                {
                    var modeKey = config.Mode.ToLower();
                    steamArgs = _steamModes.ContainsKey(modeKey) ? _steamModes[modeKey] : "";
                }

                string scriptContent = $@"@echo off
cd /d ""{steamPath}""
start Steam.exe {steamArgs}
:: Mode: {config.Mode}
:: Version: 1.1102.0400";

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string desktopScriptPath = Path.Combine(desktopPath, "Steam.bat");

                File.WriteAllText(desktopScriptPath, scriptContent);

                OnProgressChanged($"Launch script created on desktop: Steam.bat");
            }, cancellationToken);
        }

        private void RemoveSteamFromStartup()
        {
            try
            {
                var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath, true))
                {
                    if (key?.GetValue("Steam") != null)
                    {
                        key.DeleteValue("Steam");
                    }
                }
            }
            catch { }
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
            if (_disposed)
                return;

            _disposed = true;
            _steamDetectionTimer?.Dispose();
            _steamDetectionTimer = null;
        }
    }
}
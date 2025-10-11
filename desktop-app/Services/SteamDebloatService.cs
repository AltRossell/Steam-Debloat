using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SteamDebloat;

public class SteamDebloatService : IDisposable
{
    private readonly Dictionary<string, string> _steamModes;
    private readonly string _defaultSteamPath;
    private Timer? _steamDetectionTimer;
    private bool _disposed = false;
    private string? _lastDetectedSteamPath = null;

    public event Action<string>? ProgressChanged;
    public event Action<bool>? SteamDetectionChanged;

    public SteamDebloatService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _defaultSteamPath = @"C:\Program Files (x86)\Steam";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _defaultSteamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam");
        }
        else
        {
            _defaultSteamPath = string.Empty;
        }

        _steamModes = new Dictionary<string, string>
        {
            ["normal2025october"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
            ["normal2023june"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad steam://open/library",
            ["normal2022dec"] = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library",
            ["lite2022dec"] = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
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

    private void CheckSteamInstallation(object? state)
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
                var osVersion = GetOSVersion();
                var architecture = RuntimeInformation.OSArchitecture.ToString();
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

    private string GetOSVersion()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsVersion();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxVersion();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macOS (Not supported)";
            }

            return RuntimeInformation.OSDescription;
        }
        catch
        {
            return Environment.OSVersion.VersionString;
        }
    }

    private string GetWindowsVersion()
    {
        try
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                return "Windows 11";
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
                return "Windows 10";
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 3))
                return "Windows 8.1";
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
                return "Windows 8";
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return "Windows 7";

            return RuntimeInformation.OSDescription;
        }
        catch
        {
            return "Windows (Unknown version)";
        }
    }

    private string GetLinuxVersion()
    {
        try
        {
            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                foreach (var line in lines)
                {
                    if (line.StartsWith("PRETTY_NAME="))
                    {
                        return line.Substring(13).Trim('"');
                    }
                }
            }

            return RuntimeInformation.OSDescription;
        }
        catch
        {
            return "Linux";
        }
    }

    private bool IsValidSteamDirectory(string path)
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

    private string? FindSteamInstallation()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FindSteamOnWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return FindSteamOnLinux();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string? FindSteamOnWindows()
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

        if (OperatingSystem.IsWindows())
        {
            try
            {
                string[] registryPaths = {
                    @"SOFTWARE\WOW6432Node\Valve\Steam",
                    @"SOFTWARE\Valve\Steam"
                };

                foreach (var regPath in registryPaths)
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath);
                    if (key != null)
                    {
                        string? installPath = key.GetValue("InstallPath")?.ToString();
                        if (!string.IsNullOrEmpty(installPath) && IsValidSteamDirectory(installPath))
                            return installPath;
                    }
                }
            }
            catch { }
        }

        return null;
    }

    private string? FindSteamOnLinux()
    {
        string? realUser = Environment.GetEnvironmentVariable("SUDO_USER");
        string homeDir;

        if (!string.IsNullOrEmpty(realUser))
        {
            homeDir = $"/home/{realUser}";
        }
        else
        {
            homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        var commonPaths = new[]
        {
            Path.Combine(homeDir, ".steam/steam"),
            Path.Combine(homeDir, ".local/share/Steam"),
            "/usr/share/steam",
            "/usr/local/share/steam",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam")
        };

        foreach (var path in commonPaths)
        {
            if (IsValidSteamDirectory(path))
                return path;
        }

        try
        {
            if (Directory.Exists("/home"))
            {
                foreach (var userDir in Directory.GetDirectories("/home"))
                {
                    var steamPath = Path.Combine(userDir, ".steam/steam");
                    if (IsValidSteamDirectory(steamPath))
                        return steamPath;

                    steamPath = Path.Combine(userDir, ".local/share/Steam");
                    if (IsValidSteamDirectory(steamPath))
                        return steamPath;
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

            if (config.UpdateSteam)
            {
                OnProgressChanged($"Updating Steam for {config.Mode} mode...");
                await UpdateSteamAsync(steamPath, config.Mode, cancellationToken);
            }

            OnProgressChanged("Applying optimization configuration...");
            await CreateConfigurationFilesAsync(steamPath, config, cancellationToken);

            if (config.RemoveFromStartup && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
            string steamExe;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                steamExe = Path.Combine(steamPath, "steam.exe");
            }
            else
            {
                steamExe = Path.Combine(steamPath, "steam.sh");
            }

            if (!File.Exists(steamExe))
            {
                OnProgressChanged($"Steam executable not found at {steamPath}");
                return;
            }

            var modeKey = mode.ToLower();
            string arguments;

            if (modeKey == "normal2023june")
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
                WorkingDirectory = steamPath
            };

            OnProgressChanged($"Starting Steam update for {mode} mode...");

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await Task.Delay(3000, cancellationToken);
            }

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
                }
                else
                {
                    OnProgressChanged($"Steam updating for {mode} mode...");
                    foreach (var proc in steamProcesses)
                    {
                        proc.Dispose();
                    }
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

    private Task CreateConfigurationFilesAsync(string steamPath, OptimizationConfig config, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var configPath = Path.Combine(steamPath, "steam.cfg");
            var configContent = $@"BootStrapperInhibitAll=enable
            BootStrapperForceSelfUpdate=disable
            :: Mode: {config.Mode}
            :: Version: 2.105.0200";

            File.WriteAllText(configPath, configContent);

            var modeKey = config.Mode.ToLower();
            string steamArgs = _steamModes.ContainsKey(modeKey) ? _steamModes[modeKey] : "";

            string scriptContent;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptContent = $@"@echo off
                cd /d ""{steamPath}""
                start Steam.exe {steamArgs}
                :: Mode: {config.Mode}
                :: Version: 2.105.0200";
            }
            else
            {
                scriptContent = $@"#!/bin/bash
                cd ""{steamPath}""
                ./steam.sh {steamArgs} &
                # Mode: {config.Mode}
                # Version: 2.105.0200";
            }

            var tempDir = Path.GetTempPath();
            var scriptFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Steam.bat" : "Steam.sh";
            var tempScriptPath = Path.Combine(tempDir, scriptFileName);

            File.WriteAllText(tempScriptPath, scriptContent);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    using var chmodProcess = Process.Start("chmod", $"+x \"{tempScriptPath}\"");
                    chmodProcess?.WaitForExit();
                }
                catch { }
            }
        }, cancellationToken);
    }

    private void RemoveSteamFromStartup()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !OperatingSystem.IsWindows())
            return;

        try
        {
            var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath, true);
            if (key?.GetValue("Steam") != null)
            {
                key.DeleteValue("Steam");
            }
        }
        catch { }
    }

    public bool IsRunningAsAdmin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                return Environment.UserName == "root";
            }
            catch
            {
                return false;
            }
        }

        return false;
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

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace SteamDebloat
{
    /// <summary>
    /// Bootstrapper for Steam Debloat
    /// Checks for updates at startup and manages the application launch
    /// </summary>
    public class Bootstrapper
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SteamDebloat");

        private static readonly string UpdateCheckFile = Path.Combine(AppDataFolder, "last_update_check.txt");
        private const int UPDATE_CHECK_INTERVAL_DAYS = 7;

        public static async Task<bool> InitializeAsync()
        {
            try
            {
                // Create AppData folder if it does not exist
                Directory.CreateDirectory(AppDataFolder);

                // Check whether to perform update check
                if (ShouldCheckForUpdates())
                {
                    await CheckForUpdatesOnStartupAsync();
                    SaveLastUpdateCheckTime();
                }

                // Verify system requirements
                if (!CheckSystemRequirements())
                {
                    MessageBox.Show(
                        "Steam Debloat requires Windows 7 or higher and .NET Framework 4.8.\n\n" +
                        "Please install the required components and try again.",
                        "System Requirements Not Met",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // Check for administrator privileges
                CheckAdminPrivileges();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Bootstrapper error: {ex.Message}");
                return true; // Continue even if bootstrapper errors occur
            }
        }

        private static bool ShouldCheckForUpdates()
        {
            try
            {
                if (!File.Exists(UpdateCheckFile))
                    return true;

                var lastCheck = File.ReadAllText(UpdateCheckFile);
                if (DateTime.TryParse(lastCheck, out DateTime lastCheckDate))
                {
                    return (DateTime.Now - lastCheckDate).TotalDays >= UPDATE_CHECK_INTERVAL_DAYS;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void SaveLastUpdateCheckTime()
        {
            try
            {
                File.WriteAllText(UpdateCheckFile, DateTime.Now.ToString("O"));
            }
            catch { }
        }

        private static async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                using (var updateService = new UpdateService())
                {
                    var result = await updateService.CheckForUpdatesAsync();

                    if (result.Success && result.UpdateAvailable)
                    {
                        var dialogResult = MessageBox.Show(
                            $"A new version of Steam Debloat is available!\n\n" +
                            $"Current version: {result.CurrentVersion}\n" +
                            $"Latest version: {result.LatestVersion}\n\n" +
                            $"Would you like to download it now?",
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            updateService.OpenReleaseUrl(result.ReleaseUrl);
                            
                            var closeResult = MessageBox.Show(
                                "The download page has been opened in your browser.\n\n" +
                                "Would you like to close the application to install the update?",
                                "Install Update",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (closeResult == MessageBoxResult.Yes)
                            {
                                Application.Current.Shutdown();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                // Do not show an error to the user, continue silently
            }
        }

        private static bool CheckSystemRequirements()
        {
            try
            {
                // Verify Windows version
                var osVersion = Environment.OSVersion;
                if (osVersion.Version.Major < 6 || 
                    (osVersion.Version.Major == 6 && osVersion.Version.Minor < 1))
                {
                    return false; // Requires Windows 7 or higher
                }

                // Verify .NET Framework 4.8
                var frameworkVersion = GetNetFrameworkVersion();
                if (frameworkVersion < new Version(4, 8))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return true; // If verification fails, assume OK
            }
        }

        private static Version GetNetFrameworkVersion()
        {
            try
            {
                // Retrieve version from Registry
                using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    if (ndpKey != null)
                    {
                        int releaseKey = (int)ndpKey.GetValue("Release", 0);
                        
                        if (releaseKey >= 528040) // .NET Framework 4.8
                            return new Version(4, 8);
                        if (releaseKey >= 461808) // .NET Framework 4.7.2
                            return new Version(4, 7, 2);
                        if (releaseKey >= 460798) // .NET Framework 4.7
                            return new Version(4, 7);
                    }
                }

                return new Version(4, 0);
            }
            catch
            {
                return new Version(4, 0);
            }
        }

        private static void CheckAdminPrivileges()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                if (!isAdmin)
                {
                    Debug.WriteLine("Application started without administrator privileges");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to check admin privileges: {ex.Message}");
            }
        }

        public static void LogStartup()
        {
            try
            {
                var logFile = Path.Combine(AppDataFolder, "startup.log");
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Application started (Version {GetAppVersion()})\n";
                
                File.AppendAllText(logFile, logEntry);

                // Keep only the last 100 lines
                var lines = File.ReadAllLines(logFile);
                if (lines.Length > 100)
                {
                    var linesToKeep = new string[100];
                    Array.Copy(lines, lines.Length - 100, linesToKeep, 0, 100);
                    File.WriteAllLines(logFile, linesToKeep);
                }
            }
            catch { }
        }

        private static string GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "1.1102.0400";
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SteamDebloat
{
    public class UpdateService : IDisposable
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/AltRossell/SteamDebloat/releases/latest";
        private readonly HttpClient _httpClient;

        public event Action<string> ProgressChanged;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SteamDebloat-UpdateChecker");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                OnProgressChanged("Checking for updates...");

                var response = await _httpClient.GetAsync(GITHUB_API_URL);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new UpdateCheckResult
                        {
                            Success = false,
                            ErrorMessage = "No releases found in the repository. Please create a release on GitHub first."
                        };
                    }
                    
                    return new UpdateCheckResult
                    {
                        Success = false,
                        ErrorMessage = $"GitHub API returned {response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                string latestVersion = root.GetProperty("tag_name").GetString();
                string releaseUrl = root.GetProperty("html_url").GetString();
                string releaseNotes = root.GetProperty("body").GetString();
                string publishedAt = root.GetProperty("published_at").GetString();

                latestVersion = latestVersion?.TrimStart('v');

                var currentVersion = GetCurrentVersion();

                bool updateAvailable = IsNewerVersion(latestVersion, currentVersion);

                OnProgressChanged(updateAvailable ? "Update available!" : "You're up to date");

                return new UpdateCheckResult
                {
                    Success = true,
                    UpdateAvailable = updateAvailable,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = releaseUrl,
                    ReleaseNotes = releaseNotes,
                    PublishedDate = DateTime.Parse(publishedAt)
                };
            }
            catch (HttpRequestException ex)
            {
                OnProgressChanged("Failed to connect to update server");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                OnProgressChanged("Request timed out");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = "Request timed out. Please check your internet connection."
                };
            }
            catch (Exception ex)
            {
                OnProgressChanged("Error checking for updates");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        private string GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "1.1031.2300";
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                latestVersion = latestVersion.TrimEnd('H', 'F', 'h', 'f');
                currentVersion = currentVersion.TrimEnd('H', 'F', 'h', 'f');

                var latestParts = latestVersion.Split('.');
                var currentParts = currentVersion.Split('.');

                for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
                {
                    if (int.TryParse(latestParts[i], out int latest) && 
                        int.TryParse(currentParts[i], out int current))
                    {
                        if (latest > current) return true;
                        if (latest < current) return false;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void OpenReleaseUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                OnProgressChanged($"Failed to open browser: {ex.Message}");
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
            _httpClient?.Dispose();
        }
    }
}
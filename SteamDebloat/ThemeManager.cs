using System;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;

namespace SteamDebloat
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Path.GetTempPath(),
            "SteamDebloat",
            "theme.txt");

        public static event Action<AppTheme> ThemeChanged;

        private static AppTheme _currentTheme = AppTheme.Dark;

        public static AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(_currentTheme);
                }
            }
        }

        public static void Initialize()
        {
            try
            {
                var savedTheme = LoadThemePreference();
                ApplyTheme(savedTheme);
            }
            catch
            {
                ApplyTheme(AppTheme.Dark);
            }
        }

        public static void ToggleTheme()
        {
            var newTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(newTheme);
        }

        public static void ApplyTheme(AppTheme theme)
        {
            try
            {
                CurrentTheme = theme;

                // Use Dispatcher to ensure theme is applied on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    var app = Application.Current;
                    if (app != null)
                    {
                        // Apply the theme directly
                        var targetTheme = theme == AppTheme.Dark 
                            ? ThemeVariant.Dark 
                            : ThemeVariant.Light;
                        
                        // Only update if different to avoid unnecessary redraws
                        if (app.RequestedThemeVariant != targetTheme)
                        {
                            app.RequestedThemeVariant = targetTheme;
                        }
                    }
                }, DispatcherPriority.Background);

                SaveThemePreference(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private static AppTheme LoadThemePreference()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var content = File.ReadAllText(SettingsPath).Trim();
                    if (Enum.TryParse<AppTheme>(content, out var theme))
                    {
                        return theme;
                    }
                }
            }
            catch { }

            return AppTheme.Dark;
        }

        private static void SaveThemePreference(AppTheme theme)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SettingsPath, theme.ToString());
            }
            catch { }
        }
    }
}
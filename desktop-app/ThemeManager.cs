using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

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
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
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

                // Apply iNKORE.UI.WPF.Modern theme
                if (theme == AppTheme.Dark)
                {
                    iNKORE.UI.WPF.Modern.ThemeManager.Current.ApplicationTheme = iNKORE.UI.WPF.Modern.ApplicationTheme.Dark;
                }
                else
                {
                    iNKORE.UI.WPF.Modern.ThemeManager.Current.ApplicationTheme = iNKORE.UI.WPF.Modern.ApplicationTheme.Light;
                }

                // Update application color resources
                Application.Current.Resources["BackgroundPrimaryColor"] = GetBackgroundPrimary(theme);
                Application.Current.Resources["BackgroundSecondaryColor"] = GetBackgroundSecondary(theme);
                Application.Current.Resources["BackgroundTertiaryColor"] = GetBackgroundTertiary(theme);
                Application.Current.Resources["TextPrimaryColor"] = GetTextPrimary(theme);
                Application.Current.Resources["TextSecondaryColor"] = GetTextSecondary(theme);
                Application.Current.Resources["BorderColor"] = GetBorder(theme);
                Application.Current.Resources["SeparatorColor"] = GetSeparator(theme);

                Application.Current.Resources["BackgroundPrimaryBrush"] = new SolidColorBrush(GetBackgroundPrimary(theme));
                Application.Current.Resources["BackgroundSecondaryBrush"] = new SolidColorBrush(GetBackgroundSecondary(theme));
                Application.Current.Resources["BackgroundTertiaryBrush"] = new SolidColorBrush(GetBackgroundTertiary(theme));
                Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(GetTextPrimary(theme));
                Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(GetTextSecondary(theme));
                Application.Current.Resources["BorderBrush"] = new SolidColorBrush(GetBorder(theme));
                Application.Current.Resources["SeparatorBrush"] = new SolidColorBrush(GetSeparator(theme));

                UpdateWindowChromeColor(theme);
                SaveThemePreference(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private static void UpdateWindowChromeColor(AppTheme theme)
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != null)
                    {
                        var color = theme == AppTheme.Dark
                            ? Color.FromRgb(28, 28, 30)
                            : Color.FromRgb(242, 242, 247);

                        window.Background = new SolidColorBrush(color);
                    }
                }
            }
            catch { }
        }

        // Apple-inspired color palette
        private static Color GetBackgroundPrimary(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(28, 28, 30)    // #1C1C1E
                : Color.FromRgb(255, 255, 255); // #FFFFFF
        }

        private static Color GetBackgroundSecondary(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(44, 44, 46)     // #2C2C2E
                : Color.FromRgb(246, 246, 246); // #F6F6F6
        }

        private static Color GetBackgroundTertiary(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(58, 58, 60)     // #3A3A3C
                : Color.FromRgb(236, 236, 236); // #ECECEC
        }

        private static Color GetTextPrimary(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(255, 255, 255)  // #FFFFFF
                : Color.FromRgb(0, 0, 0);       // #000000
        }

        private static Color GetTextSecondary(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(152, 152, 157)  // #98989D
                : Color.FromRgb(100, 100, 100); // #646464
        }

        private static Color GetBorder(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(72, 72, 74)     // #48484A
                : Color.FromRgb(220, 220, 220); // #DCDCDC
        }

        private static Color GetSeparator(AppTheme theme)
        {
            return theme == AppTheme.Dark
                ? Color.FromRgb(56, 56, 58)     // #38383A
                : Color.FromRgb(230, 230, 230); // #E6E6E6
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
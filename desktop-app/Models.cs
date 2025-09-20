using System;

namespace SteamDebloat
{
    public class SystemInfo
    {
        public string OSVersion { get; set; }
        public string Architecture { get; set; }
        public string SteamPath { get; set; }
        public bool SteamFound { get; set; }
        public bool ConfigExists { get; set; }

        public SystemInfo()
        {
            OSVersion = string.Empty;
            Architecture = string.Empty;
            SteamPath = string.Empty;
            SteamFound = false;
            ConfigExists = false;
        }
    }

    public class OptimizationConfig
    {
        public string Mode { get; set; }
        public bool CreateDesktopShortcut { get; set; }
        public bool CreateStartMenuShortcut { get; set; }
        public bool RemoveFromStartup { get; set; }
        public bool UpdateSteam { get; set; }
        public string SteamPath { get; set; }

        public OptimizationConfig()
        {
            Mode = "Normal2025September";
            CreateDesktopShortcut = true;
            CreateStartMenuShortcut = false;
            RemoveFromStartup = true;
            UpdateSteam = true;
            SteamPath = string.Empty;
        }
    }

    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }

        public OptimizationResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
            Duration = TimeSpan.Zero;
        }
    }
}
using System;

namespace SteamDebloat;

public class SystemInfo
{
    public string OSVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string SteamPath { get; set; } = string.Empty;
    public bool SteamFound { get; set; }
    public bool ConfigExists { get; set; }
}

public class OptimizationConfig
{
    public string Mode { get; set; } = "Normal2023June";
    public bool RemoveFromStartup { get; set; } = true;
    public bool UpdateSteam { get; set; } = true;
    public string SteamPath { get; set; } = string.Empty;
}

public class OptimizationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class UninstallStatus
{
    public bool HasOptimizationConfig { get; set; }
    public string SteamConfigStatus { get; set; } = string.Empty;
}

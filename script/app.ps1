[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [ValidateSet("Normal2025July", "Normal2022dec", "Lite2022dec", "NormalBoth2022-2025")]
    [string]$Mode = "Normal2025July",
    [switch]$SkipIntro,
    [switch]$NoInteraction
)
$host.UI.RawUI.BackgroundColor = "Black"
$Debug = "off"

# Add Windows Forms for FolderBrowserDialog
Add-Type -AssemblyName System.Windows.Forms

$script:config = @{
    Title               = "Steam Debloat"
    GitHub              = "Github.com/AltRossell/Steam-Debloat"
    Version            = "v10.29"
    Color              = @{Info = "White"; Success = "Magenta"; Warning = "DarkYellow"; Error = "DarkRed"; Debug = "Blue" }
    ErrorPage          = "https://github.com/AltRossell/Steam-Debloat/issues"
    Urls               = @{
        "SteamSetup"       = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"
    }
    SteamInstallDir    = "C:\Program Files (x86)\Steam"
    SteamInstallDirV2  = "C:\Program Files (x86)\Steamv2"
    RetryAttempts      = 3
    RetryDelay         = 5
    LogFile            = Join-Path $env:TEMP "Steam-Debloat.log"
}

# Steam launch modes embedded directly
$STEAM_MODES = @{
    "normal2025july" = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
    "normal2022dec" = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
    "lite2022dec" = "-silent -cef-force-32bit -no-dwrite -no-cef-sandbox -nooverlay -nofriendsui -nobigpicture -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-gpu-compositing -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
    "normalboth2022-2025" = @{
        "steam2025" = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
        "steam2022" = "-no-dwrite -no-cef-sandbox -nooverlay -nobigpicture -nofriendsui -noshaders -novid -noverifyfiles -nointro -skipstreamingdrivers -norepairfiles -nohltv -nofasthtml -nocrashmonitor -no-shared-textures -disablehighdpi -cef-single-process -cef-in-process-gpu -single_core -cef-disable-d3d11 -cef-disable-sandbox -disable-winh264 -vrdisable -cef-disable-breakpad -cef-disable-gpu -cef-disable-hang-timeouts -cef-disable-seccomp-sandbox -cef-disable-extensions -cef-disable-remote-fonts -cef-enable-media-stream -cef-disable-accelerated-video-decode steam://open/library"
    }
}

function Test-AdminPrivileges {
    return ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Start-ProcessAsAdmin {
    param (
        [string]$FilePath,
        [string]$ArgumentList
    )
    Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -Verb RunAs -Wait
}

function Write-DebugLog {
    param (
        [string]$Message,
        [string]$Level = "Info",
        [switch]$IsPath
    )
    Write-Host "== " -NoNewline -ForegroundColor Yellow
    Write-Host "[$Level] " -NoNewline -ForegroundColor Yellow

    if ($IsPath -or $Message -match '^[A-Za-z]:\\|\\\\|/|%\w+%|~|\.\\|\.\.\\') {
        Write-Host "$Message" -ForegroundColor Magenta
    } else {
        Write-Host "$Message" -ForegroundColor Cyan
    }

    if ($Debug -eq "on") {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $logMessage = "== [$timestamp] [$Level] $Message"
        Add-Content -Path $script:config.LogFile -Value $logMessage
    }
}

function Show-FolderBrowserDialog {
    param (
        [string]$Description = "Please select your Steam installation folder"
    )
    
    $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
    $folderBrowser.Description = $Description
    $folderBrowser.RootFolder = [System.Environment+SpecialFolder]::MyComputer
    $folderBrowser.ShowNewFolderButton = $false
    
    $result = $folderBrowser.ShowDialog()
    
    if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
        return $folderBrowser.SelectedPath
    }
    return $null
}

function Test-SteamInstallation {
    param (
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    
    # First check the default directory
    $steamExePath = Join-Path $InstallDir "steam.exe"
    if (Test-Path $steamExePath) {
        Write-DebugLog "Steam found in default location: $InstallDir" -Level Success
        return @{ Found = $true; Path = $InstallDir }
    }
    
    Write-DebugLog "Steam not found in default location: $InstallDir" -Level Warning
    
    # If not in NoInteraction mode, ask user to locate Steam directly
    if (-not $NoInteraction) {
        Write-Host ""
        Write-DebugLog "Steam installation not found in the default location." -Level Warning
        $choice = Read-Host "Do you have Steam installed in a different location? (Y/N)"
        
        if ($choice.ToUpper() -eq 'Y') {
            Write-DebugLog "Please select your Steam installation folder in the dialog that will appear." -Level Info
            Write-Host "Looking for the folder that contains 'steam.exe'..." -ForegroundColor Yellow
            
            $selectedPath = Show-FolderBrowserDialog -Description "Please select your Steam installation folder (the folder containing steam.exe)"
            
            if ($selectedPath) {
                $steamExePath = Join-Path $selectedPath "steam.exe"
                if (Test-Path $steamExePath) {
                    Write-DebugLog "Steam verified in custom location: $selectedPath" -Level Success
                    # Update the config to use this path
                    $script:config.SteamInstallDir = $selectedPath
                    return @{ Found = $true; Path = $selectedPath }
                } else {
                    Write-DebugLog "steam.exe not found in selected folder: $selectedPath" -Level Error
                    Write-DebugLog "Please make sure you select the folder that contains steam.exe" -Level Warning
                }
            } else {
                Write-DebugLog "No folder selected." -Level Warning
            }
        }
    }
    
    return @{ Found = $false; Path = $null }
}

function Create-SteamBatch {
    param (
        [string]$Mode,
        [string]$SteamPath
    )

    $tempPath = [System.Environment]::GetEnvironmentVariable("TEMP")
    $modeKey = $Mode.ToLower()
    
    try {
        if ($modeKey -eq "normalboth2022-2025") {
            # Create batch for Steam 2025
            $batchPath2025 = Join-Path $tempPath "Steam2025.bat"
            $batchContent2025 = @"
@echo off
echo Launching Steam 2025 (Latest) with optimized parameters...
cd /d "$SteamPath"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2025"])
"@
            $batchContent2025 | Out-File -FilePath $batchPath2025 -Encoding ASCII -Force
            Write-DebugLog "Created Steam 2025 batch file: $batchPath2025" -Level Success
            
            # Create batch for Steam 2022
            $batchPath2022 = Join-Path $tempPath "Steam2022.bat"
            $batchContent2022 = @"
@echo off
echo Launching Steam 2022 (December 2022) with optimized parameters...
cd /d "$($script:config.SteamInstallDirV2)"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2022"])
"@
            $batchContent2022 | Out-File -FilePath $batchPath2022 -Encoding ASCII -Force
            Write-DebugLog "Created Steam 2022 batch file: $batchPath2022" -Level Success
            
            return @{ 
                SteamBat2025 = $batchPath2025
                SteamBat2022 = $batchPath2022
            }
        } else {
            $batchPath = Join-Path $tempPath "Steam-$Mode.bat"
            $batchContent = @"
@echo off
echo Launching Steam with optimized parameters...
cd /d "$SteamPath"
start Steam.exe $($STEAM_MODES[$modeKey])
"@
            $batchContent | Out-File -FilePath $batchPath -Encoding ASCII -Force
            Write-DebugLog "Created Steam batch file: $batchPath" -Level Success
            
            return @{ SteamBat = $batchPath }
        }
    }
    catch {
        Write-DebugLog "Failed to create batch file: $_" -Level Error
        return $null
    }
}

function Wait-ForPath {
    param(
        [string]$Path,
        [int]$TimeoutSeconds = 300
    )
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-Path $Path)) {
        if ($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
            Write-DebugLog "Timeout waiting for: $Path" -Level Error
            return $false
        }
        Start-Sleep -Seconds 1
    }
    return $true
}

function Install-SteamApplication {
    param (
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    $setupPath = Join-Path $env:TEMP "SteamSetup.exe"

    try {
        Invoke-SafeWebRequest -Uri $script:config.Urls.SteamSetup -OutFile $setupPath
        Write-DebugLog "Running Steam installer to $InstallDir..." -Level Info
        
        if ($InstallDir -eq $script:config.SteamInstallDirV2) {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-DebugLog "Waiting for installation to complete..." -Level Info
            if (-not (Wait-ForPath -Path $script:config.SteamInstallDir -TimeoutSeconds 300)) {
                Write-DebugLog "Steam installation did not complete in the expected time" -Level Error
                return $false
            }
            
            if (-not (Test-Path $InstallDir)) {
                New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
            }
            Copy-Item -Path "$($script:config.SteamInstallDir)\*" -Destination $InstallDir -Recurse -Force
        } else {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-DebugLog "Waiting for installation to complete..." -Level Info
            if (-not (Wait-ForPath -Path $InstallDir -TimeoutSeconds 300)) {
                Write-DebugLog "Steam installation did not complete in the expected time" -Level Error
                return $false
            }
        }
        
        $steamExePath = Join-Path $InstallDir "steam.exe"
        if (Test-Path $steamExePath) {
            Write-DebugLog "Steam installed successfully to $InstallDir!" -Level Success
            Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
            return $true
        }
        else {
            Write-DebugLog "Steam installation failed - steam.exe not found in $InstallDir" -Level Error
            return $false
        }
    }
    catch {
        Write-DebugLog "Failed to install Steam: $_" -Level Error
        return $false
    }
}

function Install-Steam {
    param (
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    try {
        $steamExePath = Join-Path $InstallDir "steam.exe"
        $needsInstallation = -not (Test-Path $steamExePath)
        if ($needsInstallation) {
            $installSuccess = Install-SteamApplication -InstallDir $InstallDir
            if (-not $installSuccess) {
                return $false
            }
        }
        return $true
    }
    catch {
        Write-DebugLog "An error occurred in Install-Steam: $_" -Level Error
        return $false
    }
}

function Start-SteamWithParameters {
    param (
        [string]$Mode,
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    try {
        $steamExePath = Join-Path $InstallDir "steam.exe"
        if (-not (Test-Path $steamExePath)) {
            return $false
        }
        
        $arguments = if ($Mode -in "Normal2022dec", "Lite2022dec") {
            "-forcesteamupdate -forcepackagedownload -overridepackageurl https://archive.org/download/dec2022steam -exitsteam"
        }
        else {
            "-forcesteamupdate -forcepackagedownload -overridepackageurl -exitsteam"
        }
        
        Write-DebugLog "Starting Steam from $InstallDir with arguments: $arguments" -Level Info
        Start-Process -FilePath $steamExePath -ArgumentList $arguments
        $timeout = 300
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (Get-Process -Name "steam" -ErrorAction SilentlyContinue) {
            if ($timer.Elapsed.TotalSeconds -gt $timeout) {
                Write-DebugLog "Steam update process timed out after $timeout seconds." -Level Warning
                break
            }
            Start-Sleep -Seconds 5
        }
        $timer.Stop()
        Write-DebugLog "Steam update process completed in $($timer.Elapsed.TotalSeconds) seconds." -Level Info
        return $true
    }
    catch {
        Write-DebugLog "Failed to start Steam: $_" -Level Error
        return $false
    }
}

function Invoke-SafeWebRequest {
    param (
        [string]$Uri,
        [string]$OutFile
    )
    $attempt = 0
    do {
        $attempt++
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -ge $script:config.RetryAttempts) {
                throw "Failed to download from $Uri after $($script:config.RetryAttempts) attempts: $_"
            }
            Write-DebugLog "Download attempt $attempt failed. Retrying in $($script:config.RetryDelay) seconds..." -Level Warning
            Start-Sleep -Seconds $script:config.RetryDelay
        }
    } while ($true)
}

function Stop-SteamProcesses {
    $steamProcesses = Get-Process -Name "*steam*" -ErrorAction SilentlyContinue
    foreach ($process in $steamProcesses) {
        try {
            $process.Kill()
            $process.WaitForExit(5000)
            Write-DebugLog "Stopped process: $($process.ProcessName)" -Level Info
        }
        catch {
            if ($_.Exception.Message -notlike "*The process has already exited.*") {
                Write-DebugLog "Failed to stop process $($process.ProcessName): $_" -Level Warning
            }
        }
    }
}

function Get-RequiredFiles {
    param (
        [string]$SelectedMode,
        [string]$SteamPath
    )
    
    # Create batch files using embedded modes
    $batchFiles = Create-SteamBatch -Mode $SelectedMode -SteamPath $SteamPath
    
    # Create steam.cfg
    $steamCfgPath = Join-Path $env:TEMP "steam.cfg"
    @"
BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
"@ | Out-File -FilePath $steamCfgPath -Encoding ASCII -Force

    if ($SelectedMode.ToLower() -eq "normalboth2022-2025") {
        return @{ 
            SteamBat2025 = $batchFiles.SteamBat2025
            SteamBat2022 = $batchFiles.SteamBat2022
            SteamCfg = $steamCfgPath 
        }
    } else {
        return @{ 
            SteamBat = $batchFiles.SteamBat
            SteamCfg = $steamCfgPath 
        }
    }
}

function Move-ConfigFile {
    param (
        [string]$SourcePath,
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    $destinationPath = Join-Path $InstallDir "steam.cfg"
    Copy-Item -Path $SourcePath -Destination $destinationPath -Force
    Write-DebugLog "Moved steam.cfg to $destinationPath" -Level Info
}

function Move-SteamBatToDesktop {
    param (
        [string]$SourcePath,
        [string]$FileName = "steam.bat"
    )
    $destinationPath = Join-Path ([Environment]::GetFolderPath("Desktop")) $FileName
    Copy-Item -Path $SourcePath -Destination $destinationPath -Force
    Write-DebugLog "Moved $FileName to desktop" -Level Info
}

function Move-SteamBatToStartMenu {
    param (
        [string]$SourcePath,
        [string]$FileName = "steam.bat"
    )
    try {
        $startMenuPath = [System.IO.Path]::Combine($env:APPDATA, "Microsoft", "Windows", "Start Menu", "Programs", "Steam")
        
        if (-not (Test-Path $startMenuPath)) {
            New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
            Write-DebugLog "Created Steam folder in Start Menu" -Level Info
        }
        
        $destinationPath = Join-Path $startMenuPath $FileName
        Copy-Item -Path $SourcePath -Destination $destinationPath -Force
        Write-DebugLog "Moved $FileName to Start Menu Steam folder" -Level Success
        return $true
    }
    catch {
        Write-DebugLog "Failed to move $FileName to Start Menu: $_" -Level Error
        return $false
    }
}

function Remove-TempFiles {
    Remove-Item -Path (Join-Path $env:TEMP "Steam-*.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2025.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2022.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "steam.cfg") -Force -ErrorAction SilentlyContinue
    Write-DebugLog "Removed temporary files" -Level Info
}

function Remove-SteamFromStartup {
    try {
        $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        $steamEntry = Get-ItemProperty -Path $registryPath -Name "Steam" -ErrorAction SilentlyContinue
        
        if ($steamEntry) {
            Remove-ItemProperty -Path $registryPath -Name "Steam" -Force
            Write-DebugLog "Steam removed from startup registry successfully" -Level Success
            return $true
        } else {
            Write-DebugLog "Steam entry not found in startup registry" -Level Warning
            return $false
        }
    }
    catch {
        Write-DebugLog "Failed to remove Steam from startup: $_" -Level Error
        return $false
    }
}

function Get-UserChoice {
    param (
        [string]$Prompt,
        [string]$DefaultChoice = "N"
    )
    
    if ($NoInteraction) {
        Write-DebugLog "NoInteraction mode: Using default choice '$DefaultChoice' for: $Prompt" -Level Info
        return $DefaultChoice.ToUpper()
    }
    
    do {
        $choice = Read-Host $Prompt
        if ([string]::IsNullOrEmpty($choice)) {
            $choice = $DefaultChoice
        }
        switch ($choice.ToUpper()) {
            "Y" { return "Y" }
            "N" { return "N" }
            default { 
                Write-Host "Invalid choice. Please enter Y or N." -ForegroundColor Red
                continue
            }
        }
    } while ($true)
}

function Start-OptimizationGuide {
    try {
        Write-Host ""
        Write-DebugLog "=== STEAM RAM & FPS OPTIMIZATION GUIDE ===" -Level Info
        Write-Host ""
        Write-DebugLog "This optimization will:" -Level Info
        Write-DebugLog "Disable GPU acceleration for web views" -Level Info
        Write-DebugLog "Disable smooth scroll in web views" -Level Info
        Write-DebugLog "Enable library low performance mode" -Level Info
        Write-DebugLog "Enable library low bandwidth mode" -Level Info
        Write-DebugLog "Disable library community content" -Level Info
        Write-Host ""
        
        $choice = Get-UserChoice -Prompt "Do you want to apply these RAM & FPS optimizations? (Y/N)" -DefaultChoice "N"
        if ($choice -eq "N") {
            Write-DebugLog "Optimization skipped by user." -Level Info
            return $false
        }
        
        Write-DebugLog "Starting optimization process..." -Level Info
        
        # Step 1: Ask user to login to Steam
        Write-Host ""
        Write-DebugLog "STEP 1: Please login to Steam now" -Level Warning
        Write-DebugLog "Make sure you are logged in to your Steam account before continuing." -Level Info
        
        # Always wait for user confirmation, even in NoInteraction mode
        if ($NoInteraction) {
            Write-DebugLog "NoInteraction mode: Please ensure Steam is logged in, then press Enter to continue..." -Level Warning
        }
        Read-Host "Press Enter when you have logged in to Steam"
        
        # Step 2: Close Steam processes
        Write-DebugLog "STEP 2: Closing Steam processes..." -Level Info
        try {
            taskkill /f /im steam.exe 2>$null
            taskkill /f /im steamwebhelper.exe 2>$null
            Start-Sleep -Seconds 3
            Write-DebugLog "Steam processes closed successfully" -Level Success
        }
        catch {
            Write-DebugLog "Warning: Could not close some Steam processes: $_" -Level Warning
        }
        
        # Step 3: Modify registry settings
        Write-DebugLog "STEP 3: Modifying Steam registry settings..." -Level Info
        try {
            $steamRegistryPath = "HKCU:\SOFTWARE\Valve\Steam"
            
            if (Test-Path $steamRegistryPath) {
                # Disable smooth scroll web views
                Set-ItemProperty -Path $steamRegistryPath -Name "SmoothScrollWebViews" -Value 0 -Type DWord -Force
                Write-DebugLog "Disabled SmoothScrollWebViews" -Level Success
                
                # Disable GPU acceleration for web views
                Set-ItemProperty -Path $steamRegistryPath -Name "GPUAccelWebViewsV3" -Value 0 -Type DWord -Force
                Write-DebugLog "Disabled GPUAccelWebViewsV3" -Level Success
            } else {
                Write-DebugLog "Steam registry path not found" -Level Warning
                return $false
            }
        }
        catch {
            Write-DebugLog "Failed to modify registry settings: $_" -Level Error
            return $false
        }
        
        # Step 4: Modify localconfig.vdf
        Write-DebugLog "STEP 4: Modifying Steam user configuration..." -Level Info
        try {
            $steamUserDataPath = Join-Path $script:config.SteamInstallDir "userdata"
            
            if (Test-Path $steamUserDataPath) {
                $userFolders = Get-ChildItem -Path $steamUserDataPath -Directory | Where-Object { $_.Name -match '^\d+$' }
                
                if ($userFolders.Count -eq 0) {
                    Write-DebugLog "No user folders found in Steam userdata" -Level Warning
                    return $false
                }
                
                foreach ($userFolder in $userFolders) {
                    $localConfigPath = Join-Path $userFolder.FullName "config\localconfig.vdf"
                    
                    if (Test-Path $localConfigPath) {
                        Write-DebugLog "Modifying localconfig.vdf for user: $($userFolder.Name)" -Level Info
                        
                        $content = Get-Content $localConfigPath -Raw
                        
                        # Modify library settings
                        $content = $content -replace '"LibraryDisableCommunityContent"\s*"0"', '"LibraryDisableCommunityContent"		"1"'
                        $content = $content -replace '"LibraryLowPerfMode"\s*"0"', '"LibraryLowPerfMode"		"0"'
                        $content = $content -replace '"LibraryLowBandwidthMode"\s*"0"', '"LibraryLowBandwidthMode"		"1"'
                        
                        # If settings don't exist, add them
                        if ($content -notmatch '"LibraryDisableCommunityContent"') {
                            $content = $content -replace '("UserLocalConfigStore"\s*{)', "`$1`n`t`"LibraryDisableCommunityContent`"`t`t`"1`""
                        }
                        if ($content -notmatch '"LibraryLowPerfMode"') {
                            $content = $content -replace '("UserLocalConfigStore"\s*{)', "`$1`n`t`"LibraryLowPerfMode`"`t`t`"1`""
                        }
                        if ($content -notmatch '"LibraryLowBandwidthMode"') {
                            $content = $content -replace '("UserLocalConfigStore"\s*{)', "`$1`n`t`"LibraryLowBandwidthMode`"`t`t`"1`""
                        }
                        
                        # Create backup
                        $backupPath = "$localConfigPath.backup"
                        Copy-Item $localConfigPath $backupPath -Force
                        Write-DebugLog "Created backup: $backupPath" -Level Info
                        
                        # Write modified content
                        $content | Out-File -FilePath $localConfigPath -Encoding UTF8 -Force
                        Write-DebugLog "Modified localconfig.vdf for user $($userFolder.Name)" -Level Success
                    }
                }
            } else {
                Write-DebugLog "Steam userdata path not found: $steamUserDataPath" -Level Warning
                return $false
            }
        }
        catch {
            Write-DebugLog "Failed to modify localconfig.vdf: $_" -Level Error
            return $false
        }
        
        Write-Host ""
        Write-DebugLog "Steam RAM & FPS optimization completed successfully!" -Level Success
        Write-DebugLog "The following optimizations have been applied:" -Level Success
        Write-DebugLog "- Disabled GPU acceleration for web views" -Level Success
        Write-DebugLog "- Disabled smooth scroll in web views" -Level Success
        Write-DebugLog "- Enabled library performance optimizations" -Level Success
        Write-Host ""
        
        return $true
    }
    catch {
        Write-DebugLog "An error occurred during optimization: $_" -Level Error
        return $false
    }
}

function Start-SteamDebloat {
    param (
        [string]$SelectedMode
    )
    try {
        if (-not (Test-AdminPrivileges)) {
            Write-DebugLog "Requesting administrator privileges..." -Level Warning
            $scriptPath = $MyInvocation.MyCommand.Path
            $arguments = "-File `"$scriptPath`" -Mode `"$SelectedMode`""
            foreach ($param in $PSBoundParameters.GetEnumerator()) {
                if ($param.Key -ne "Mode") {
                    $arguments += " -$($param.Key)"
                    if ($param.Value -isnot [switch]) {
                        $arguments += " `"$($param.Value)`""
                    }
                }
            }
            Start-ProcessAsAdmin -FilePath "powershell.exe" -ArgumentList $arguments
            return
        }
        
        Write-DebugLog "Starting $($script:config.Title) Optimization in $SelectedMode mode" -Level Info

        if ($SelectedMode -eq "NormalBoth2022-2025") {
            Write-DebugLog "Installing both Steam versions (2022 and 2025)..." -Level Info
            
            # Check Steam 2025 version
            $steamCheck2025 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDir
            if (-not $steamCheck2025.Found) {
                Write-DebugLog "Installing Steam 2025 version..." -Level Info
                $installSuccess2025 = Install-Steam -InstallDir $script:config.SteamInstallDir
                if (-not $installSuccess2025) {
                    Write-DebugLog "Failed to install Steam 2025 version" -Level Error
                    return
                }
            } else {
                Write-DebugLog "Steam 2025 version already installed at: $($steamCheck2025.Path)" -Level Success
                $script:config.SteamInstallDir = $steamCheck2025.Path
            }
            Start-SteamWithParameters -Mode "Normal2025July" -InstallDir $script:config.SteamInstallDir
            
            # Check Steam 2022 version
            $steamCheck2022 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDirV2
            if (-not $steamCheck2022.Found) {
                Write-DebugLog "Installing Steam 2022 version..." -Level Info
                $installSuccess2022 = Install-Steam -InstallDir $script:config.SteamInstallDirV2
                if (-not $installSuccess2022) {
                    Write-DebugLog "Failed to install Steam 2022 version" -Level Error
                    return
                }
            } else {
                Write-DebugLog "Steam 2022 version already installed" -Level Success
            }
            Start-SteamWithParameters -Mode "Normal2022dec" -InstallDir $script:config.SteamInstallDirV2
            
            Stop-SteamProcesses
            
            # Generate files using the detected Steam path
            $files = Get-RequiredFiles -SelectedMode $SelectedMode -SteamPath $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDirV2
            
            # Move batch files to desktop
            Move-SteamBatToDesktop -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
            Move-SteamBatToDesktop -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
            
            # Ask about Start Menu for both versions
            Write-Host ""
            $choice = Get-UserChoice -Prompt "Do you want to add Steam batch files to Start Menu? (Y/N)" -DefaultChoice "N"
            if ($choice -eq "Y") {
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
            } else {
                Write-DebugLog "Start Menu shortcuts skipped." -Level Info
            }
            
            Remove-TempFiles
        }
        else {
            Write-DebugLog "Checking Steam installation..." -Level Info
            
            # Enhanced Steam detection
            $steamCheck = Test-SteamInstallation
            
            if (-not $steamCheck.Found) {
                Write-DebugLog "Steam is not installed or not found." -Level Warning
                if (-not $NoInteraction) {
                    $choice = Read-Host "Would you like to install Steam? (Y/N)"
                    if ($choice.ToUpper() -ne 'Y') {
                        Write-DebugLog "Cannot proceed without Steam installation." -Level Error
                        return
                    }
                } else {
                    Write-DebugLog "NoInteraction mode: Installing Steam automatically..." -Level Info
                }
                $installSuccess = Install-Steam
                if (-not $installSuccess) {
                    Write-DebugLog "Cannot proceed without Steam installation." -Level Error
                    return
                }
            } else {
                Write-DebugLog "Using Steam installation at: $($steamCheck.Path)" -Level Success
                $script:config.SteamInstallDir = $steamCheck.Path
            }
            
            $steamResult = Start-SteamWithParameters -Mode $SelectedMode -InstallDir $script:config.SteamInstallDir
            if (-not $steamResult) {
                Write-DebugLog "Failed to start Steam with parameters" -Level Warning
            }
            
            Stop-SteamProcesses
            
            # Generate files using the detected/installed Steam path
            $files = Get-RequiredFiles -SelectedMode $SelectedMode -SteamPath $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            
            # Move batch file to desktop
            Move-SteamBatToDesktop -SourcePath $files.SteamBat -FileName "Steam.bat"
            
            # Ask about Start Menu
            Write-Host ""
            $choice = Get-UserChoice -Prompt "Do you want to add the optimized Steam batch file to Start Menu? (Y/N)" -DefaultChoice "N"
            if ($choice -eq "Y") {
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat -FileName "Steam.bat"
            } else {
                Write-DebugLog "Start Menu shortcut skipped." -Level Info
            }
            
            Remove-TempFiles
        }

        # Ask about startup removal
        Write-Host ""
        $choice = Get-UserChoice -Prompt "Do you want to remove Steam from Windows startup? (Y/N)" -DefaultChoice "N"
        if ($choice -eq "Y") {
            $removeResult = Remove-SteamFromStartup
            if ($removeResult) {
                Write-DebugLog "Steam has been removed from Windows startup." -Level Success
            }
        } else {
            Write-DebugLog "Steam startup configuration left unchanged." -Level Info
        }

        # Run optimization guide
        Start-OptimizationGuide

        Write-Host ""
        Write-DebugLog "Steam Optimization process completed successfully!" -Level Success
        Write-DebugLog "Steam has been updated and configured for optimal performance." -Level Success
        Write-DebugLog "Optimized batch file(s) have been created on your desktop." -Level Success
        Write-DebugLog "You can contribute to improve the repository at: $($script:config.GitHub)" -Level Success
        
        if (-not $NoInteraction) { 
            Read-Host "Press Enter to exit" 
        } else {
            Write-DebugLog "Process completed. Exiting automatically in NoInteraction mode." -Level Info
            Start-Sleep -Seconds 2
        }
    }
    catch {
        Write-DebugLog "An error occurred: $_" -Level Error
        Write-DebugLog "For troubleshooting, visit: $($script:config.ErrorPage)" -Level Info
    }
}

# Initialize debug logging if enabled
if ($Debug -eq "on") {
    if (Test-Path $script:config.LogFile) {
        Clear-Content $script:config.LogFile
    }
    Write-DebugLog "Debug logging enabled - Log file: $($script:config.LogFile)" -Level Info
}

# Set window title
$host.UI.RawUI.WindowTitle = "$($script:config.GitHub)"

# Show intro unless skipped
if (-not $SkipIntro) {
    Clear-Host
    Write-Host @"
 ______     ______   ______     ______     __    __           
/\  ___\   /\__  _\ /\  ___\   /\  __ \   /\ "-./  \          
\ \___  \  \/_/\ \/ \ \  __\   \ \  __ \  \ \ \-./\ \         
 \/\_____\    \ \_\  \ \_____\  \ \_\ \_\  \ \_\ \ \_\        
  \/_____/     \/_/   \/_____/   \/_/\/_/   \/_/  \/_/        
                                                              
                __    __     ______     __   __     __  __    
               /\ "-./  \   /\  ___\   /\ "-.\ \   /\ \/\ \   
               \ \ \-./\ \  \ \  __\   \ \ \-.  \  \ \ \_\ \  
                \ \_\ \ \_\  \ \_____\  \ \_\\"\_\  \ \_____\ 
                 \/_/  \/_/   \/_____/   \/_/ \/_/   \/_____/ 
                                                              
"@ -ForegroundColor Green
    Write-DebugLog "$($script:config.Version)" -Level Info
    
    if (-not $NoInteraction) {
        Write-Host ""
        Write-DebugLog "Select Steam optimization mode:" -Level Info
        Write-DebugLog "1. Normal2025July (Latest Steam version)" -Level Info
        Write-DebugLog "2. Normal2022dec (December 2022 Steam version)" -Level Info
        Write-DebugLog "3. Lite2022dec (Lite December 2022 version)" -Level Info
        Write-DebugLog "4. NormalBoth2022-2025 (Experimental - Install both versions)" -Level Info
        Write-Host ""
        
        do {
            $choice = Read-Host "Enter your choice (1-4)"
            switch ($choice) {
                "1" { $Mode = "Normal2025July"; break }
                "2" { $Mode = "Normal2022dec"; break }
                "3" { $Mode = "Lite2022dec"; break }
                "4" { $Mode = "NormalBoth2022-2025"; break }
                default { 
                    Write-Host "Invalid choice. Please enter 1, 2, 3, or 4." -ForegroundColor Red
                    continue
                }
            }
            break
        } while ($true)
        
        Write-DebugLog "Selected mode: $Mode" -Level Info
    } else {
        Write-DebugLog "NoInteraction mode: Using mode $Mode" -Level Info
    }
}

# Start the main process
Start-SteamDebloat -SelectedMode $Mode

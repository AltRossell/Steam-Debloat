[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [ValidateSet("Normal2025July", "Normal2022dec", "Lite2022dec", "NormalBoth2022-2025")]
    [string]$Mode = "Normal2025July",
    [switch]$SkipIntro,
    [switch]$NoInteraction
)

$host.UI.RawUI.BackgroundColor = "Black"

# Add Windows Forms for FolderBrowserDialog
Add-Type -AssemblyName System.Windows.Forms

$script:config = @{
    Title               = "Steam Debloat"
    GitHub              = "Github.com/AltRossell/Steam-Debloat"
    Version            = "v11.04"
    Color              = @{
        Info = "White"
        Success = "White"
        Warning = "Yellow"
        Error = "Red"
        Time = "Green"
        Category = "Magenta"
    }
    ErrorPage          = "https://github.com/AltRossell/Steam-Debloat/issues"
    Urls               = @{
        "SteamSetup"       = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"
    }
    SteamInstallDir    = "C:\Program Files (x86)\Steam"
    SteamInstallDirV2  = "C:\Program Files (x86)\Steamv2"
    RetryAttempts      = 3
    RetryDelay         = 5
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

function Get-TimeStamp {
    return Get-Date -Format "HH:mm:ss.fff"
}

function Write-Message {
    param (
        [string]$Message,
        [string]$Level = "Info",
        [string]$Category = ""
    )
    
    $color = switch ($Level) {
        "Success" { "White" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        "Loaded" { "White" }
        default { "White" }
    }

    Write-Host "[" -NoNewline -ForegroundColor Green
    Write-Host (Get-TimeStamp) -NoNewline -ForegroundColor Green
    Write-Host "] " -NoNewline -ForegroundColor Green

    if ($Category -ne "") {
        Write-Host "[$Category] " -NoNewline -ForegroundColor Magenta
    }

    Write-Host "$Message" -ForegroundColor $color
}

function Clear-Screen {
    [System.Console]::Clear()
}

function Show-SystemInfo {
    param(
        [string]$SelectedMode
    )
    
    Write-Message "------------------------------" -Category "SYSTEM"
    Write-Message "Steam-Debloat Latest" -Category "SYSTEM"
    Write-Message "OS: $([System.Environment]::OSVersion.VersionString)" -Category "SYSTEM"
    Write-Message "------------------------------" -Category "SYSTEM"
    Write-Message "Script Version: $($script:config.Version)" -Category "SYSTEM"
    $osArch = if ([System.Environment]::Is64BitOperatingSystem) { 'x64' } else { 'x86' }
    Write-Message "OS Arch: $osArch" -Category "SYSTEM"
    Write-Message "------------------------------" -Category "SYSTEM"
    
    # Check Steam paths
    $steamPath = if (Test-Path $script:config.SteamInstallDir) { $script:config.SteamInstallDir } else { "No Found" }
    $steamv2Path = if (Test-Path $script:config.SteamInstallDirV2) { $script:config.SteamInstallDirV2 } else { "No Found" }
    
    Write-Message "Steam::DataPath = $steamPath" -Category "SYSTEM"
    Write-Message "Steamv2::DataPath = $steamv2Path" -Category "SYSTEM"
    
    # Add steam.cfg detection
    $steamCfgPath = if (Test-Path $script:config.SteamInstallDir) { 
        $cfgPath = Join-Path $script:config.SteamInstallDir "steam.cfg"
        if (Test-Path $cfgPath) { $cfgPath } else { "No Found" }
    } else { "No Found" }
    Write-Message "Steam.cfg::DataPath = $steamCfgPath" -Category "SYSTEM"
    
    Write-Message "------------------------------" -Category "SYSTEM"
    Write-Message "Script Name: Steam Debloat" -Category "SYSTEM"
    Write-Message "Script Developer: AltRossell" -Category "SYSTEM"
    Write-Message "------------------------------" -Category "SYSTEM"
    
    # Remove the selected mode message as requested
    Write-Host ""
}

function Show-Menu {
    param(
        [string]$Title,
        [array]$Options,
        [int]$Line = 0
    )
    
    $selected = 0
    $startTop = $Line

    # Write the fixed title with timestamp and category
    [System.Console]::SetCursorPosition(0, $startTop)
    Write-Host "[" -NoNewline -ForegroundColor Green
    Write-Host (Get-TimeStamp) -NoNewline -ForegroundColor Green
    Write-Host "] " -NoNewline -ForegroundColor Green
    Write-Host "[MENU] " -NoNewline -ForegroundColor Magenta
    [System.Console]::ForegroundColor = "Yellow"
    [System.Console]::WriteLine($Title)
    [System.Console]::ForegroundColor = "White"

    while ($true) {
        # Draw options in fixed positions
        for ($i = 0; $i -lt $Options.Count; $i++) {
            [System.Console]::SetCursorPosition(0, $startTop + $i + 1)
            if ($i -eq $selected) {
                # Highlighted option with > symbol
                [System.Console]::ForegroundColor = "Magenta"
                [System.Console]::Write("> " + $Options[$i] + "   ")
                [System.Console]::ForegroundColor = "White"
            } else {
                [System.Console]::Write("  " + $Options[$i] + "   ")
            }
            # Clear rest of the line
            [System.Console]::Write(" " * ([System.Console]::WindowWidth - [System.Console]::CursorLeft))
        }

        $key = [System.Console]::ReadKey($true)
        switch ($key.Key) {
            "UpArrow"   { if ($selected -gt 0) { $selected-- } }
            "DownArrow" { if ($selected -lt $Options.Count - 1) { $selected++ } }
            "Enter"     {
                return $Options[$selected]
            }
        }
    }
}

function Show-YesNoMenu {
    param(
        [string]$Title,
        [int]$Line = 0
    )
    
    $selected = 0
    $options = @("Yes", "No")
    $startTop = $Line

    # Write the question with timestamp and category
    [System.Console]::SetCursorPosition(0, $startTop)
    Write-Host "[" -NoNewline -ForegroundColor Green
    Write-Host (Get-TimeStamp) -NoNewline -ForegroundColor Green
    Write-Host "] " -NoNewline -ForegroundColor Green
    Write-Host "[QUESTION] " -NoNewline -ForegroundColor Magenta
    [System.Console]::ForegroundColor = "Yellow"
    [System.Console]::WriteLine($Title)
    [System.Console]::ForegroundColor = "White"

    while ($true) {
        # Draw options in fixed positions
        for ($i = 0; $i -lt $options.Count; $i++) {
            [System.Console]::SetCursorPosition(0, $startTop + $i + 1)
            if ($i -eq $selected) {
                # Highlighted option with > symbol
                [System.Console]::ForegroundColor = "Magenta"
                [System.Console]::Write("> " + $options[$i] + "   ")
                [System.Console]::ForegroundColor = "White"
            } else {
                [System.Console]::Write("  " + $options[$i] + "   ")
            }
            # Clear rest of the line
            [System.Console]::Write(" " * ([System.Console]::WindowWidth - [System.Console]::CursorLeft))
        }

        $key = [System.Console]::ReadKey($true)
        switch ($key.Key) {
            "UpArrow"   { if ($selected -gt 0) { $selected-- } }
            "DownArrow" { if ($selected -lt $options.Count - 1) { $selected++ } }
            "Enter"     {
                return ($options[$selected] -eq "Yes")
            }
        }
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

function Test-ExistingFiles {
    Write-Message "FileSystem::Validation" -Category "VALIDATION"
    
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $startMenuPath = [System.IO.Path]::Combine($env:APPDATA, "Microsoft", "Windows", "Start Menu", "Programs", "Steam")
    
    $script:FilesStatus = @{
        DesktopSteamBat = Test-Path (Join-Path $desktopPath "Steam.bat")
        DesktopSteam2025Bat = Test-Path (Join-Path $desktopPath "Steam2025.bat")
        DesktopSteam2022Bat = Test-Path (Join-Path $desktopPath "Steam2022.bat")
        StartMenuSteamBat = Test-Path (Join-Path $startMenuPath "Steam.bat")
        StartMenuSteam2025Bat = Test-Path (Join-Path $startMenuPath "Steam2025.bat")
        StartMenuSteam2022Bat = Test-Path (Join-Path $startMenuPath "Steam2022.bat")
        SteamCfg = Test-Path (Join-Path $script:config.SteamInstallDir "steam.cfg")
        SteamCfgV2 = Test-Path (Join-Path $script:config.SteamInstallDirV2 "steam.cfg")
    }
    
    # Report existing files
    if ($script:FilesStatus.DesktopSteamBat) { Write-Message "Desktop Steam.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.DesktopSteam2025Bat) { Write-Message "Desktop Steam2025.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.DesktopSteam2022Bat) { Write-Message "Desktop Steam2022.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.StartMenuSteamBat) { Write-Message "Start Menu Steam.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.StartMenuSteam2025Bat) { Write-Message "Start Menu Steam2025.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.StartMenuSteam2022Bat) { Write-Message "Start Menu Steam2022.bat loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.SteamCfg) { Write-Message "Steam steam.cfg loaded" -Level Loaded -Category "VALIDATION" }
    if ($script:FilesStatus.SteamCfgV2) { Write-Message "Steamv2 steam.cfg loaded" -Level Loaded -Category "VALIDATION" }
    
    # Add space after validation
    Write-Host ""
}

function Test-SteamInstallation {
    param (
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    
    # Don't show detection messages if Steam is already found in system info
    $steamExePath = Join-Path $InstallDir "steam.exe"
    if (Test-Path $steamExePath) {
        return @{ Found = $true; Path = $InstallDir }
    }
    
    Write-Message "Steam::Detection" -Category "DETECTION"
    Write-Message "Steam not found in default location: $InstallDir" -Level Warning -Category "DETECTION"
    
    # If not in NoInteraction mode, ask user to locate Steam directly
    if (-not $NoInteraction) {
        Write-Host ""
        Write-Message "Steam installation not found in the default location." -Level Warning -Category "DETECTION"
        
        $hasCustomLocation = Show-YesNoMenu -Title "Do you have Steam installed in a different location?" -Line ([Console]::CursorTop + 1)
        
        if ($hasCustomLocation) {
            Write-Message "Please select your Steam installation folder in the dialog that will appear." -Category "DETECTION"
            Write-Host "Looking for the folder that contains 'steam.exe'..." -ForegroundColor Yellow
            
            $selectedPath = Show-FolderBrowserDialog -Description "Please select your Steam installation folder (the folder containing steam.exe)"
            
            if ($selectedPath) {
                $steamExePath = Join-Path $selectedPath "steam.exe"
                if (Test-Path $steamExePath) {
                    Write-Message "Steam verified in custom location: $selectedPath" -Level Success -Category "DETECTION"
                    # Update the config to use this path
                    $script:config.SteamInstallDir = $selectedPath
                    return @{ Found = $true; Path = $selectedPath }
                } else {
                    Write-Message "steam.exe not found in selected folder: $selectedPath" -Level Error -Category "DETECTION"
                    Write-Message "Please make sure you select the folder that contains steam.exe" -Level Warning -Category "DETECTION"
                }
            } else {
                Write-Message "No folder selected." -Level Warning -Category "DETECTION"
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

    Write-Message "BatchFile::Generator" -Category "GENERATOR"
    
    $tempPath = [System.Environment]::GetEnvironmentVariable("TEMP")
    $modeKey = $Mode.ToLower()
    
    try {
        if ($modeKey -eq "normalboth2022-2025") {
            # Create batch for Steam 2025
            $batchPath2025 = Join-Path $tempPath "Steam2025.bat"
            $batchContent2025 = @"
@echo off
cd /d "$SteamPath"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2025"])
"@
            $batchContent2025 | Out-File -FilePath $batchPath2025 -Encoding ASCII -Force
            Write-Message "Created Steam 2025 batch file: $batchPath2025" -Level Success -Category "GENERATOR"
            
            # Create batch for Steam 2022
            $batchPath2022 = Join-Path $tempPath "Steam2022.bat"
            $batchContent2022 = @"
@echo off
cd /d "$($script:config.SteamInstallDirV2)"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2022"])
"@
            $batchContent2022 | Out-File -FilePath $batchPath2022 -Encoding ASCII -Force
            Write-Message "Created Steam 2022 batch file: $batchPath2022" -Level Success -Category "GENERATOR"
            
            return @{ 
                SteamBat2025 = $batchPath2025
                SteamBat2022 = $batchPath2022
            }
        } else {
            $batchPath = Join-Path $tempPath "Steam-$Mode.bat"
            $batchContent = @"
@echo off
cd /d "$SteamPath"
start Steam.exe $($STEAM_MODES[$modeKey])
"@
            $batchContent | Out-File -FilePath $batchPath -Encoding ASCII -Force
            Write-Message "Created Steam batch file: $batchPath" -Level Success -Category "GENERATOR"
            
            return @{ SteamBat = $batchPath }
        }
    }
    catch {
        Write-Message "Failed to create batch file: $_" -Level Error -Category "GENERATOR"
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
            Write-Message "Timeout waiting for: $Path" -Level Error -Category "INSTALLATION"
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
    
    Write-Message "Steam::Installation" -Category "INSTALLATION"
    
    $setupPath = Join-Path $env:TEMP "SteamSetup.exe"

    try {
        Invoke-SafeWebRequest -Uri $script:config.Urls.SteamSetup -OutFile $setupPath
        Write-Message "Running Steam installer to $InstallDir..." -Category "INSTALLATION"
        
        if ($InstallDir -eq $script:config.SteamInstallDirV2) {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-Message "Waiting for installation to complete..." -Category "INSTALLATION"
            if (-not (Wait-ForPath -Path $script:config.SteamInstallDir -TimeoutSeconds 300)) {
                Write-Message "Steam installation did not complete in the expected time" -Level Error -Category "INSTALLATION"
                return $false
            }
            
            if (-not (Test-Path $InstallDir)) {
                New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
            }
            Copy-Item -Path "$($script:config.SteamInstallDir)\*" -Destination $InstallDir -Recurse -Force
        } else {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-Message "Waiting for installation to complete..." -Category "INSTALLATION"
            if (-not (Wait-ForPath -Path $InstallDir -TimeoutSeconds 300)) {
                Write-Message "Steam installation did not complete in the expected time" -Level Error -Category "INSTALLATION"
                return $false
            }
        }
        
        $steamExePath = Join-Path $InstallDir "steam.exe"
        if (Test-Path $steamExePath) {
            Write-Message "Steam installed successfully to $InstallDir!" -Level Success -Category "INSTALLATION"
            Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
            return $true
        }
        else {
            Write-Message "Steam installation failed - steam.exe not found in $InstallDir" -Level Error -Category "INSTALLATION"
            return $false
        }
    }
    catch {
        Write-Message "Failed to install Steam: $_" -Level Error -Category "INSTALLATION"
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
        Write-Message "An error occurred in Install-Steam: $_" -Level Error -Category "INSTALLATION"
        return $false
    }
}

function Start-SteamWithParameters {
    param (
        [string]$Mode,
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    
    Write-Message "Steam::UpdateProcess" -Category "UPDATE"
    
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
        
        Write-Message "Starting Steam from $InstallDir with arguments: $arguments" -Category "UPDATE"
        Start-Process -FilePath $steamExePath -ArgumentList $arguments
        $timeout = 300
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (Get-Process -Name "steam" -ErrorAction SilentlyContinue) {
            if ($timer.Elapsed.TotalSeconds -gt $timeout) {
                Write-Message "Steam update process timed out after $timeout seconds." -Level Warning -Category "UPDATE"
                break
            }
            Start-Sleep -Seconds 5
        }
        $timer.Stop()
        Write-Message "Steam update process completed in $($timer.Elapsed.TotalSeconds) seconds." -Level Success -Category "UPDATE"
        
        # Add space after update process
        Write-Host ""
        
        return $true
    }
    catch {
        Write-Message "Failed to start Steam: $_" -Level Error -Category "UPDATE"
        return $false
    }
}

function Invoke-SafeWebRequest {
    param (
        [string]$Uri,
        [string]$OutFile
    )
    
    Write-Message "Network::DownloadManager" -Category "DOWNLOAD"
    
    $attempt = 0
    do {
        $attempt++
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing -ErrorAction Stop
            Write-Message "Download completed successfully: $OutFile" -Level Success -Category "DOWNLOAD"
            return
        }
        catch {
            if ($attempt -ge $script:config.RetryAttempts) {
                throw "Failed to download from $Uri after $($script:config.RetryAttempts) attempts: $_"
            }
            Write-Message "Download attempt $attempt failed. Retrying in $($script:config.RetryDelay) seconds..." -Level Warning -Category "DOWNLOAD"
            Start-Sleep -Seconds $script:config.RetryDelay
        }
    } while ($true)
}

function Stop-SteamProcesses {
    # Removed Process::Cleanup message as requested
    
    $steamProcesses = Get-Process -Name "*steam*" -ErrorAction SilentlyContinue
    foreach ($process in $steamProcesses) {
        try {
            $process.Kill()
            $process.WaitForExit(5000)
            Write-Message "Stopped process: $($process.ProcessName)" -Level Success -Category "CLEANUP"
        }
        catch {
            if ($_.Exception.Message -notlike "*The process has already exited.*") {
                Write-Message "Failed to stop process $($process.ProcessName): $_" -Level Warning -Category "CLEANUP"
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
    
    Write-Message "Config::Generator" -Category "CONFIG"
    
    # Check if steam.cfg already exists and skip creation if it does
    $steamCfgPath = Join-Path $SteamPath "steam.cfg"
    $steamCfgTempPath = Join-Path $env:TEMP "steam.cfg"
    
    if (-not (Test-Path $steamCfgPath)) {
        # Create steam.cfg only if it doesn't exist
        @"
BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
"@ | Out-File -FilePath $steamCfgTempPath -Encoding ASCII -Force

        Write-Message "Created steam.cfg configuration file" -Level Success -Category "CONFIG"
    } else {
        Write-Message "Steam.cfg already exists, skipping creation" -Level Warning -Category "CONFIG"
        $steamCfgTempPath = $null
    }

    if ($SelectedMode.ToLower() -eq "normalboth2022-2025") {
        return @{ 
            SteamBat2025 = $batchFiles.SteamBat2025
            SteamBat2022 = $batchFiles.SteamBat2022
            SteamCfg = $steamCfgTempPath 
        }
    } else {
        return @{ 
            SteamBat = $batchFiles.SteamBat
            SteamCfg = $steamCfgTempPath 
        }
    }
}

function Move-ConfigFile {
    param (
        [string]$SourcePath,
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    
    # Only move if source exists and destination doesn't exist
    if ($SourcePath -and (Test-Path $SourcePath)) {
        $destinationPath = Join-Path $InstallDir "steam.cfg"
        if (-not (Test-Path $destinationPath)) {
            Write-Message "Config::Deployment" -Category "DEPLOY"
            Copy-Item -Path $SourcePath -Destination $destinationPath -Force
            Write-Message "Moved steam.cfg to $destinationPath" -Level Success -Category "DEPLOY"
        }
    }
}

function Move-SteamBatToDesktop {
    param (
        [string]$SourcePath,
        [string]$FileName = "steam.bat"
    )
    
    $destinationPath = Join-Path ([Environment]::GetFolderPath("Desktop")) $FileName
    if (-not (Test-Path $destinationPath)) {
        Write-Message "Desktop::Deployment" -Category "DESKTOP"
        Copy-Item -Path $SourcePath -Destination $destinationPath -Force
        Write-Message "Moved $FileName to desktop" -Level Success -Category "DESKTOP"
    }
}

function Move-SteamBatToStartMenu {
    param (
        [string]$SourcePath,
        [string]$FileName = "steam.bat"
    )
    
    $destinationPath = Join-Path ([Environment]::GetFolderPath("Desktop")) $FileName
    if (-not (Test-Path $destinationPath)) {
        Write-Message "StartMenu::Deployment" -Category "STARTMENU"
        
        try {
            $startMenuPath = [System.IO.Path]::Combine($env:APPDATA, "Microsoft", "Windows", "Start Menu", "Programs", "Steam")
            
            if (-not (Test-Path $startMenuPath)) {
                New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
                Write-Message "Created Steam folder in Start Menu" -Category "STARTMENU"
            }
            
            $destinationPath = Join-Path $startMenuPath $FileName
            Copy-Item -Path $SourcePath -Destination $destinationPath -Force
            Write-Message "Moved $FileName to Start Menu Steam folder" -Level Success -Category "STARTMENU"
            return $true
        }
        catch {
            Write-Message "Failed to move $FileName to Start Menu: $_" -Level Error -Category "STARTMENU"
            return $false
        }
    }
}

function Remove-TempFiles {
    Write-Message "Cleanup::TempFiles" -Category "CLEANUP"
    
    Remove-Item -Path (Join-Path $env:TEMP "Steam-*.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2025.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2022.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "steam.cfg") -Force -ErrorAction SilentlyContinue
    Write-Message "Removed temporary files" -Level Success -Category "CLEANUP"
}

function Remove-SteamFromStartup {
    Write-Message "Registry::StartupCleanup" -Category "REGISTRY"
    
    try {
        $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        $steamEntry = Get-ItemProperty -Path $registryPath -Name "Steam" -ErrorAction SilentlyContinue
        
        if ($steamEntry) {
            Remove-ItemProperty -Path $registryPath -Name "Steam" -Force
            Write-Message "Steam removed from startup registry successfully" -Level Success -Category "REGISTRY"
            return $true
        } else {
            Write-Message "Steam entry not found in startup registry" -Level Warning -Category "REGISTRY"
            return $false
        }
    }
    catch {
        Write-Message "Failed to remove Steam from startup: $_" -Level Error -Category "REGISTRY"
        return $false
    }
}

function Start-SteamDebloat {
    param (
        [string]$SelectedMode
    )
    try {
        if (-not (Test-AdminPrivileges)) {
            Write-Message "Requesting administrator privileges..." -Level Warning -Category "SECURITY"
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

        # Check for existing files first
        Write-Host ""
        Test-ExistingFiles

        if ($SelectedMode -eq "NormalBoth2022-2025") {
            Write-Message "Installing both Steam versions (2022 and 2025)..." -Category "MAIN"
            
            # Check Steam 2025 version
            $steamCheck2025 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDir
            if (-not $steamCheck2025.Found) {
                Write-Message "Installing Steam 2025 version..." -Category "MAIN"
                $installSuccess2025 = Install-Steam -InstallDir $script:config.SteamInstallDir
                if (-not $installSuccess2025) {
                    Write-Message "Failed to install Steam 2025 version" -Level Error -Category "MAIN"
                    return
                }
            } else {
                $script:config.SteamInstallDir = $steamCheck2025.Path
            }
            Start-SteamWithParameters -Mode "Normal2025July" -InstallDir $script:config.SteamInstallDir
            
            # Check Steam 2022 version
            $steamCheck2022 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDirV2
            if (-not $steamCheck2022.Found) {
                Write-Message "Installing Steam 2022 version..." -Category "MAIN"
                $installSuccess2022 = Install-Steam -InstallDir $script:config.SteamInstallDirV2
                if (-not $installSuccess2022) {
                    Write-Message "Failed to install Steam 2022 version" -Level Error -Category "MAIN"
                    return
                }
            }
            Start-SteamWithParameters -Mode "Normal2022dec" -InstallDir $script:config.SteamInstallDirV2
            
            Stop-SteamProcesses
            
            # Generate files using the detected Steam path
            $files = Get-RequiredFiles -SelectedMode $SelectedMode -SteamPath $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDirV2
            
            # Move batch files to desktop only if they don't exist
            if (-not $script:FilesStatus.DesktopSteam2025Bat) {
                Move-SteamBatToDesktop -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
            }
            if (-not $script:FilesStatus.DesktopSteam2022Bat) {
                Move-SteamBatToDesktop -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
            }
            
            # Ask about Start Menu only if files don't exist
            if (-not ($script:FilesStatus.StartMenuSteam2025Bat -and $script:FilesStatus.StartMenuSteam2022Bat)) {
                Write-Host ""
                $addToStartMenu = Show-YesNoMenu -Title "Do you want to add Steam batch files to Start Menu?" -Line ([Console]::CursorTop + 1)
                if ($addToStartMenu) {
                    Write-Message "Yes" -Category "QUESTION"
                    if (-not $script:FilesStatus.StartMenuSteam2025Bat) {
                        Move-SteamBatToStartMenu -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
                    }
                    if (-not $script:FilesStatus.StartMenuSteam2022Bat) {
                        Move-SteamBatToStartMenu -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
                    }
                } else {
                    Write-Message "No" -Category "QUESTION"
                    Write-Message "Start Menu shortcuts skipped." -Category "MAIN"
                }
            }
            
            Remove-TempFiles
        }
        else {
            # Enhanced Steam detection
            $steamCheck = Test-SteamInstallation
            
            if (-not $steamCheck.Found) {
                Write-Message "Steam is not installed or not found." -Level Warning -Category "MAIN"
                if (-not $NoInteraction) {
                    Write-Host ""
                    $installSteam = Show-YesNoMenu -Title "Would you like to install Steam?" -Line ([Console]::CursorTop + 1)
                    if (-not $installSteam) {
                        Write-Message "No" -Category "QUESTION"
                        Write-Message "Cannot proceed without Steam installation." -Level Error -Category "MAIN"
                        return
                    } else {
                        Write-Message "Yes" -Category "QUESTION"
                    }
                } else {
                    Write-Message "NoInteraction mode: Installing Steam automatically..." -Category "MAIN"
                }
                $installSuccess = Install-Steam
                if (-not $installSuccess) {
                    Write-Message "Cannot proceed without Steam installation." -Level Error -Category "MAIN"
                    return
                }
            } else {
                $script:config.SteamInstallDir = $steamCheck.Path
            }
            
            # Ask about starting Steam with parameters for Normal2025July mode
            if ($SelectedMode -eq "Normal2025July" -and -not $NoInteraction) {
                Write-Host ""
                $startWithParams = Show-YesNoMenu -Title "Do you want to start Steam with parameters to get the latest Steam update?" -Line ([Console]::CursorTop + 1)
                if ($startWithParams) {
                    Write-Message "Yes" -Category "QUESTION"
                    $steamResult = Start-SteamWithParameters -Mode $SelectedMode -InstallDir $script:config.SteamInstallDir
                    if (-not $steamResult) {
                        Write-Message "Failed to start Steam with parameters" -Level Warning -Category "MAIN"
                    }
                } else {
                    Write-Message "No" -Category "QUESTION"
                    Write-Message "Skipping Steam parameter startup" -Category "MAIN"
                }
            } else {
                $steamResult = Start-SteamWithParameters -Mode $SelectedMode -InstallDir $script:config.SteamInstallDir
                if (-not $steamResult) {
                    Write-Message "Failed to start Steam with parameters" -Level Warning -Category "MAIN"
                }
            }
            
            Stop-SteamProcesses
            
            # Generate files using the detected/installed Steam path
            $files = Get-RequiredFiles -SelectedMode $SelectedMode -SteamPath $script:config.SteamInstallDir
            if ($files.SteamCfg) {
                Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            }
            
            # Move batch file to desktop only if it doesn't exist
            if (-not $script:FilesStatus.DesktopSteamBat) {
                Move-SteamBatToDesktop -SourcePath $files.SteamBat -FileName "Steam.bat"
            }
            
            # Ask about Start Menu only if file doesn't exist
            if (-not $script:FilesStatus.StartMenuSteamBat) {
                Write-Host ""
                $addToStartMenu = Show-YesNoMenu -Title "Do you want to add the optimized Steam batch file to Start Menu?" -Line ([Console]::CursorTop + 1)
                if ($addToStartMenu) {
                    Write-Message "Yes" -Category "QUESTION"
                    Move-SteamBatToStartMenu -SourcePath $files.SteamBat -FileName "Steam.bat"
                } else {
                    Write-Message "No" -Category "QUESTION"
                    Write-Message "Start Menu shortcut skipped." -Category "MAIN"
                }
            }
            
            Remove-TempFiles
        }

        # Ask about startup removal - single space before this section
        Write-Host ""
        $removeFromStartup = Show-YesNoMenu -Title "Do you want to remove Steam from Windows startup?" -Line ([Console]::CursorTop + 1)
        if ($removeFromStartup) {
            Write-Message "Yes" -Category "QUESTION"
            $removeResult = Remove-SteamFromStartup
            if ($removeResult) {
                Write-Message "Steam has been removed from Windows startup." -Level Success -Category "MAIN"
            }
        } else {
            Write-Message "No" -Category "QUESTION"
            Write-Message "Steam startup configuration left unchanged." -Category "MAIN"
        }

        Write-Host ""
        Write-Message "Steam Optimization process completed successfully!" -Level Success -Category "COMPLETE"
        Write-Message "Steam has been updated and configured for optimal performance." -Level Success -Category "COMPLETE"
        Write-Message "Optimized batch file(s) have been created on your desktop." -Level Success -Category "COMPLETE"
        Write-Message "You can contribute to improve the repository at: $($script:config.GitHub)" -Level Success -Category "COMPLETE"
        
        if (-not $NoInteraction) { 
            Write-Host ""
            Write-Host "Press any key to exit..." -ForegroundColor Yellow
            [System.Console]::ReadKey($true) | Out-Null
        } else {
            Write-Message "Process completed. Exiting automatically in NoInteraction mode." -Category "MAIN"
            Start-Sleep -Seconds 2
        }
    }
    catch {
        Write-Message "An error occurred: $_" -Level Error -Category "ERROR"
        Write-Message "For troubleshooting, visit: $($script:config.ErrorPage)" -Category "ERROR"
    }
}

# Set window title
$host.UI.RawUI.WindowTitle = "$($script:config.GitHub)"

# Show intro unless skipped
if (-not $SkipIntro) {
    # Maintain black background and clear screen
    [System.Console]::BackgroundColor = "Black"
    [System.Console]::ForegroundColor = "White"
    [System.Console]::Clear()
    [System.Console]::CursorVisible = $false
    
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
    
    if (-not $NoInteraction) {
        Write-Host ""
        $modeOptions = @(
            "Normal2025July (Latest Steam version)",
            "Normal2022dec (December 2022 Steam version)", 
            "Lite2022dec (Lite December 2022 version)",
            "NormalBoth2022-2025 (Experimental - Install both versions)"
        )
        
        $selectedOption = Show-Menu -Title "Select Steam optimization mode:" -Options $modeOptions -Line ([Console]::CursorTop + 1)
        
        $Mode = switch ($selectedOption) {
            $modeOptions[0] { "Normal2025July" }
            $modeOptions[1] { "Normal2022dec" }
            $modeOptions[2] { "Lite2022dec" }
            $modeOptions[3] { "NormalBoth2022-2025" }
        }
        
        # Clear screen after selection and show system info
        Clear-Screen
        Show-SystemInfo -SelectedMode $Mode
    } else {
        Write-Message "NoInteraction mode: Using mode $Mode" -Category "INIT"
        Clear-Screen
        Show-SystemInfo -SelectedMode $Mode
    }
}

# Restore cursor visibility
[System.Console]::CursorVisible = $true

# Start the main process
Start-SteamDebloat -SelectedMode $Mode
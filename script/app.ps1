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
    Version            = "v10.30"
    Color              = @{Info = "White"; Success = "Magenta"; Warning = "DarkYellow"; Error = "DarkRed" }
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

function Show-Menu {
    param(
        [string]$Title,
        [array]$Options,
        [int]$Line = 0
    )
    
    $selected = 0
    $startTop = $Line

    # Write the fixed title
    [System.Console]::SetCursorPosition(0, $startTop)
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
    
    $options = @("Yes", "No")
    $result = Show-Menu -Title $Title -Options $options -Line $Line
    return ($result -eq "Yes")
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

function Write-Message {
    param (
        [string]$Message,
        [string]$Level = "Info",
        [switch]$IsPath
    )
    
    $color = switch ($Level) {
        "Success" { "Magenta" }
        "Warning" { "DarkYellow" }
        "Error" { "DarkRed" }
        default { "White" }
    }

    Write-Host "== " -NoNewline -ForegroundColor Yellow
    Write-Host "[$Level] " -NoNewline -ForegroundColor Yellow

    if ($IsPath -or $Message -match '^[A-Za-z]:\\|\\\\|/|%\w+%|~|\.\\|\.\.\\') {
        Write-Host "$Message" -ForegroundColor Magenta
    } else {
        Write-Host "$Message" -ForegroundColor $color
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
        Write-Message "Steam found in default location: $InstallDir" -Level Success
        return @{ Found = $true; Path = $InstallDir }
    }
    
    Write-Message "Steam not found in default location: $InstallDir" -Level Warning
    
    # If not in NoInteraction mode, ask user to locate Steam directly
    if (-not $NoInteraction) {
        Write-Host ""
        Write-Message "Steam installation not found in the default location." -Level Warning
        
        $hasCustomLocation = Show-YesNoMenu -Title "Do you have Steam installed in a different location?" -Line ([Console]::CursorTop + 1)
        
        if ($hasCustomLocation) {
            Write-Message "Please select your Steam installation folder in the dialog that will appear." -Level Info
            Write-Host "Looking for the folder that contains 'steam.exe'..." -ForegroundColor Yellow
            
            $selectedPath = Show-FolderBrowserDialog -Description "Please select your Steam installation folder (the folder containing steam.exe)"
            
            if ($selectedPath) {
                $steamExePath = Join-Path $selectedPath "steam.exe"
                if (Test-Path $steamExePath) {
                    Write-Message "Steam verified in custom location: $selectedPath" -Level Success
                    # Update the config to use this path
                    $script:config.SteamInstallDir = $selectedPath
                    return @{ Found = $true; Path = $selectedPath }
                } else {
                    Write-Message "steam.exe not found in selected folder: $selectedPath" -Level Error
                    Write-Message "Please make sure you select the folder that contains steam.exe" -Level Warning
                }
            } else {
                Write-Message "No folder selected." -Level Warning
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
cd /d "$SteamPath"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2025"])
"@
            $batchContent2025 | Out-File -FilePath $batchPath2025 -Encoding ASCII -Force
            Write-Message "Created Steam 2025 batch file: $batchPath2025" -Level Success
            
            # Create batch for Steam 2022
            $batchPath2022 = Join-Path $tempPath "Steam2022.bat"
            $batchContent2022 = @"
@echo off
cd /d "$($script:config.SteamInstallDirV2)"
start Steam.exe $($STEAM_MODES[$modeKey]["steam2022"])
"@
            $batchContent2022 | Out-File -FilePath $batchPath2022 -Encoding ASCII -Force
            Write-Message "Created Steam 2022 batch file: $batchPath2022" -Level Success
            
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
            Write-Message "Created Steam batch file: $batchPath" -Level Success
            
            return @{ SteamBat = $batchPath }
        }
    }
    catch {
        Write-Message "Failed to create batch file: $_" -Level Error
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
            Write-Message "Timeout waiting for: $Path" -Level Error
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
        Write-Message "Running Steam installer to $InstallDir..." -Level Info
        
        if ($InstallDir -eq $script:config.SteamInstallDirV2) {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-Message "Waiting for installation to complete..." -Level Info
            if (-not (Wait-ForPath -Path $script:config.SteamInstallDir -TimeoutSeconds 300)) {
                Write-Message "Steam installation did not complete in the expected time" -Level Error
                return $false
            }
            
            if (-not (Test-Path $InstallDir)) {
                New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
            }
            Copy-Item -Path "$($script:config.SteamInstallDir)\*" -Destination $InstallDir -Recurse -Force
        } else {
            Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait
            Write-Message "Waiting for installation to complete..." -Level Info
            if (-not (Wait-ForPath -Path $InstallDir -TimeoutSeconds 300)) {
                Write-Message "Steam installation did not complete in the expected time" -Level Error
                return $false
            }
        }
        
        $steamExePath = Join-Path $InstallDir "steam.exe"
        if (Test-Path $steamExePath) {
            Write-Message "Steam installed successfully to $InstallDir!" -Level Success
            Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
            return $true
        }
        else {
            Write-Message "Steam installation failed - steam.exe not found in $InstallDir" -Level Error
            return $false
        }
    }
    catch {
        Write-Message "Failed to install Steam: $_" -Level Error
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
        Write-Message "An error occurred in Install-Steam: $_" -Level Error
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
        
        Write-Message "Starting Steam from $InstallDir with arguments: $arguments" -Level Info
        Start-Process -FilePath $steamExePath -ArgumentList $arguments
        $timeout = 300
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (Get-Process -Name "steam" -ErrorAction SilentlyContinue) {
            if ($timer.Elapsed.TotalSeconds -gt $timeout) {
                Write-Message "Steam update process timed out after $timeout seconds." -Level Warning
                break
            }
            Start-Sleep -Seconds 5
        }
        $timer.Stop()
        Write-Message "Steam update process completed in $($timer.Elapsed.TotalSeconds) seconds." -Level Info
        return $true
    }
    catch {
        Write-Message "Failed to start Steam: $_" -Level Error
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
            Write-Message "Download attempt $attempt failed. Retrying in $($script:config.RetryDelay) seconds..." -Level Warning
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
            Write-Message "Stopped process: $($process.ProcessName)" -Level Info
        }
        catch {
            if ($_.Exception.Message -notlike "*The process has already exited.*") {
                Write-Message "Failed to stop process $($process.ProcessName): $_" -Level Warning
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
    Write-Message "Moved steam.cfg to $destinationPath" -Level Info
}

function Move-SteamBatToDesktop {
    param (
        [string]$SourcePath,
        [string]$FileName = "steam.bat"
    )
    $destinationPath = Join-Path ([Environment]::GetFolderPath("Desktop")) $FileName
    Copy-Item -Path $SourcePath -Destination $destinationPath -Force
    Write-Message "Moved $FileName to desktop" -Level Info
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
            Write-Message "Created Steam folder in Start Menu" -Level Info
        }
        
        $destinationPath = Join-Path $startMenuPath $FileName
        Copy-Item -Path $SourcePath -Destination $destinationPath -Force
        Write-Message "Moved $FileName to Start Menu Steam folder" -Level Success
        return $true
    }
    catch {
        Write-Message "Failed to move $FileName to Start Menu: $_" -Level Error
        return $false
    }
}

function Remove-TempFiles {
    Remove-Item -Path (Join-Path $env:TEMP "Steam-*.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2025.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2022.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "steam.cfg") -Force -ErrorAction SilentlyContinue
    Write-Message "Removed temporary files" -Level Info
}

function Remove-SteamFromStartup {
    try {
        $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        $steamEntry = Get-ItemProperty -Path $registryPath -Name "Steam" -ErrorAction SilentlyContinue
        
        if ($steamEntry) {
            Remove-ItemProperty -Path $registryPath -Name "Steam" -Force
            Write-Message "Steam removed from startup registry successfully" -Level Success
            return $true
        } else {
            Write-Message "Steam entry not found in startup registry" -Level Warning
            return $false
        }
    }
    catch {
        Write-Message "Failed to remove Steam from startup: $_" -Level Error
        return $false
    }
}

function Start-SteamDebloat {
    param (
        [string]$SelectedMode
    )
    try {
        if (-not (Test-AdminPrivileges)) {
            Write-Message "Requesting administrator privileges..." -Level Warning
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
        
        Write-Message "Starting $($script:config.Title) Optimization in $SelectedMode mode" -Level Info

        if ($SelectedMode -eq "NormalBoth2022-2025") {
            Write-Message "Installing both Steam versions (2022 and 2025)..." -Level Info
            
            # Check Steam 2025 version
            $steamCheck2025 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDir
            if (-not $steamCheck2025.Found) {
                Write-Message "Installing Steam 2025 version..." -Level Info
                $installSuccess2025 = Install-Steam -InstallDir $script:config.SteamInstallDir
                if (-not $installSuccess2025) {
                    Write-Message "Failed to install Steam 2025 version" -Level Error
                    return
                }
            } else {
                Write-Message "Steam 2025 version already installed at: $($steamCheck2025.Path)" -Level Success
                $script:config.SteamInstallDir = $steamCheck2025.Path
            }
            Start-SteamWithParameters -Mode "Normal2025July" -InstallDir $script:config.SteamInstallDir
            
            # Check Steam 2022 version
            $steamCheck2022 = Test-SteamInstallation -InstallDir $script:config.SteamInstallDirV2
            if (-not $steamCheck2022.Found) {
                Write-Message "Installing Steam 2022 version..." -Level Info
                $installSuccess2022 = Install-Steam -InstallDir $script:config.SteamInstallDirV2
                if (-not $installSuccess2022) {
                    Write-Message "Failed to install Steam 2022 version" -Level Error
                    return
                }
            } else {
                Write-Message "Steam 2022 version already installed" -Level Success
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
            $addToStartMenu = Show-YesNoMenu -Title "Do you want to add Steam batch files to Start Menu?" -Line ([Console]::CursorTop + 1)
            if ($addToStartMenu) {
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
            } else {
                Write-Message "Start Menu shortcuts skipped." -Level Info
            }
            
            Remove-TempFiles
        }
        else {
            Write-Message "Checking Steam installation..." -Level Info
            
            # Enhanced Steam detection
            $steamCheck = Test-SteamInstallation
            
            if (-not $steamCheck.Found) {
                Write-Message "Steam is not installed or not found." -Level Warning
                if (-not $NoInteraction) {
                    $installSteam = Show-YesNoMenu -Title "Would you like to install Steam?" -Line ([Console]::CursorTop + 1)
                    if (-not $installSteam) {
                        Write-Message "Cannot proceed without Steam installation." -Level Error
                        return
                    }
                } else {
                    Write-Message "NoInteraction mode: Installing Steam automatically..." -Level Info
                }
                $installSuccess = Install-Steam
                if (-not $installSuccess) {
                    Write-Message "Cannot proceed without Steam installation." -Level Error
                    return
                }
            } else {
                Write-Message "Using Steam installation at: $($steamCheck.Path)" -Level Success
                $script:config.SteamInstallDir = $steamCheck.Path
            }
            
            $steamResult = Start-SteamWithParameters -Mode $SelectedMode -InstallDir $script:config.SteamInstallDir
            if (-not $steamResult) {
                Write-Message "Failed to start Steam with parameters" -Level Warning
            }
            
            Stop-SteamProcesses
            
            # Generate files using the detected/installed Steam path
            $files = Get-RequiredFiles -SelectedMode $SelectedMode -SteamPath $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            
            # Move batch file to desktop
            Move-SteamBatToDesktop -SourcePath $files.SteamBat -FileName "Steam.bat"
            
            # Ask about Start Menu
            Write-Host ""
            $addToStartMenu = Show-YesNoMenu -Title "Do you want to add the optimized Steam batch file to Start Menu?" -Line ([Console]::CursorTop + 1)
            if ($addToStartMenu) {
                Move-SteamBatToStartMenu -SourcePath $files.SteamBat -FileName "Steam.bat"
            } else {
                Write-Message "Start Menu shortcut skipped." -Level Info
            }
            
            Remove-TempFiles
        }

        # Ask about startup removal
        Write-Host ""
        $removeFromStartup = Show-YesNoMenu -Title "Do you want to remove Steam from Windows startup?" -Line ([Console]::CursorTop + 1)
        if ($removeFromStartup) {
            $removeResult = Remove-SteamFromStartup
            if ($removeResult) {
                Write-Message "Steam has been removed from Windows startup." -Level Success
            }
        } else {
            Write-Message "Steam startup configuration left unchanged." -Level Info
        }

        Write-Host ""
        Write-Message "Steam Optimization process completed successfully!" -Level Success
        Write-Message "Steam has been updated and configured for optimal performance." -Level Success
        Write-Message "Optimized batch file(s) have been created on your desktop." -Level Success
        Write-Message "You can contribute to improve the repository at: $($script:config.GitHub)" -Level Success
        
        if (-not $NoInteraction) { 
            Write-Host ""
            Write-Host "Press any key to exit..." -ForegroundColor Yellow
            [System.Console]::ReadKey($true) | Out-Null
        } else {
            Write-Message "Process completed. Exiting automatically in NoInteraction mode." -Level Info
            Start-Sleep -Seconds 2
        }
    }
    catch {
        Write-Message "An error occurred: $_" -Level Error
        Write-Message "For troubleshooting, visit: $($script:config.ErrorPage)" -Level Info
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
    Write-Message "$($script:config.Version)" -Level Info
    
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
        
        Write-Host ""
        Write-Message "Selected mode: $Mode" -Level Info
    } else {
        Write-Message "NoInteraction mode: Using mode $Mode" -Level Info
    }
}

# Restore cursor visibility
[System.Console]::CursorVisible = $true

# Start the main process
Start-SteamDebloat -SelectedMode $Mode
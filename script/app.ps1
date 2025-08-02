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

$script:config = @{
    Title               = "Steam Debloat"
    GitHub              = "Github.com/AltRossell/Steam-Debloat"
    Version            = "v3.10"
    Color              = @{Info = "White"; Success = "Magenta"; Warning = "DarkYellow"; Error = "DarkRed"; Debug = "Blue" }
    ErrorPage          = "https://github.com/AltRossell/Steam-Debloat/issues"
    Urls               = @{
        "SteamSetup"       = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"
        "SteamScript"      = "https://raw.githubusercontent.com/AltRossell/Steam-Debloat/refs/heads/main/script/steam.ps1"
    }
    SteamInstallDir    = "C:\Program Files (x86)\Steam"
    SteamInstallDirV2  = "C:\Program Files (x86)\Steamv2"
    RetryAttempts      = 3
    RetryDelay         = 5
    LogFile            = Join-Path $env:TEMP "Steam-Debloat.log"
    SteamScriptPath    = Join-Path $env:TEMP "steam.ps1"
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

function Test-SteamInstallation {
    param (
        [string]$InstallDir = $script:config.SteamInstallDir
    )
    $steamExePath = Join-Path $InstallDir "steam.exe"
    return Test-Path $steamExePath
}

function Get-SteamScript {
    $maxAttempts = 3
    $attempt = 0
    do {
        $attempt++
        try {
            Invoke-SafeWebRequest -Uri $script:config.Urls.SteamScript -OutFile $script:config.SteamScriptPath
            if (Test-Path $script:config.SteamScriptPath) {
                $content = Get-Content $script:config.SteamScriptPath -Raw
                if ($content) { return $true }
            }
        } catch {
            Write-DebugLog "Attempt $attempt to download steam.ps1 failed: $_" -Level Warning
            if ($attempt -ge $maxAttempts) { return $false }
            Start-Sleep -Seconds 2
        }
    } while ($attempt -lt $maxAttempts)
    return $false
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
        [string]$SelectedMode
    )
    & $script:config.SteamScriptPath -SelectedMode $SelectedMode

    $steamCfgPath = Join-Path $env:TEMP "steam.cfg"
    @"
BootStrapperInhibitAll=enable
BootStrapperForceSelfUpdate=disable
"@ | Out-File -FilePath $steamCfgPath -Encoding ASCII -Force

    if ($SelectedMode.ToLower() -eq "normalboth2022-2025") {
        return @{ 
            SteamBat2025 = Join-Path $env:TEMP "Steam2025.bat"
            SteamBat2022 = Join-Path $env:TEMP "Steam2022.bat"
            SteamCfg = $steamCfgPath 
        }
    } else {
        return @{ 
            SteamBat = Join-Path $env:TEMP "Steam-$SelectedMode.bat"
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

function Remove-TempFiles {
    Remove-Item -Path (Join-Path $env:TEMP "Steam-*.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2025.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "Steam2022.bat") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "steam.cfg") -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $env:TEMP "steam.ps1") -Force -ErrorAction SilentlyContinue
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
            
            # Install Steam 2025 version
            Write-DebugLog "Installing Steam 2025 version..." -Level Info
            if (-not (Test-SteamInstallation -InstallDir $script:config.SteamInstallDir)) {
                $installSuccess2025 = Install-Steam -InstallDir $script:config.SteamInstallDir
                if (-not $installSuccess2025) {
                    Write-DebugLog "Failed to install Steam 2025 version" -Level Error
                    return
                }
            }
            Start-SteamWithParameters -Mode "Normal2025July" -InstallDir $script:config.SteamInstallDir
            
            # Install Steam 2022 version
            Write-DebugLog "Installing Steam 2022 version..." -Level Info
            if (-not (Test-SteamInstallation -InstallDir $script:config.SteamInstallDirV2)) {
                $installSuccess2022 = Install-Steam -InstallDir $script:config.SteamInstallDirV2
                if (-not $installSuccess2022) {
                    Write-DebugLog "Failed to install Steam 2022 version" -Level Error
                    return
                }
            }
            Start-SteamWithParameters -Mode "Normal2022dec" -InstallDir $script:config.SteamInstallDirV2
            
            Stop-SteamProcesses
            
            $files = Get-RequiredFiles -SelectedMode $SelectedMode
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDir
            Move-ConfigFile -SourcePath $files.SteamCfg -InstallDir $script:config.SteamInstallDirV2
            Move-SteamBatToDesktop -SourcePath $files.SteamBat2025 -FileName "Steam2025.bat"
            Move-SteamBatToDesktop -SourcePath $files.SteamBat2022 -FileName "Steam2022.bat"
            Remove-TempFiles
        }
        else {
            Write-DebugLog "Initializing Steam with optimized parameters..." -Level Info
            
            if (-not (Test-SteamInstallation)) {
                Write-DebugLog "Steam is not installed on this system." -Level Warning
                if (-not $NoInteraction) {
                    $choice = Read-Host "Would you like to install Steam? (Y/N)"
                    if ($choice.ToUpper() -ne 'Y') {
                        Write-DebugLog "Cannot proceed without Steam installation." -Level Error
                        return
                    }
                }
                $installSuccess = Install-Steam
                if (-not $installSuccess) {
                    Write-DebugLog "Cannot proceed without Steam installation." -Level Error
                    return
                }
            }
            
            $steamResult = Start-SteamWithParameters -Mode $SelectedMode
            if (-not $steamResult) {
                Write-DebugLog "Failed to start Steam with parameters" -Level Warning
            }
            
            Stop-SteamProcesses
            $files = Get-RequiredFiles -SelectedMode $SelectedMode
            Move-ConfigFile -SourcePath $files.SteamCfg
            Move-SteamBatToDesktop -SourcePath $files.SteamBat
            Remove-TempFiles
        }

        if (-not $NoInteraction) {
            Write-Host ""
            do {
                $choice = Read-Host "Do you want to remove Steam from startup? (Y/N)"
                switch ($choice.ToUpper()) {
                    "Y" { 
                        $removeResult = Remove-SteamFromStartup
                        if ($removeResult) {
                            Write-DebugLog "Steam has been removed from Windows startup." -Level Success
                        }
                        break 
                    }
                    "N" { 
                        Write-DebugLog "Steam startup configuration left unchanged." -Level Info
                        break 
                    }
                    default { 
                        Write-Host "Invalid choice. Please enter Y or N." -ForegroundColor Red
                        continue
                    }
                }
                break
            } while ($true)
        }

        Write-DebugLog "Steam Optimization process completed successfully!" -Level Success
        Write-DebugLog "Steam has been updated and configured for optimal performance." -Level Success
        Write-DebugLog "You can contribute to improve the repository at: $($script:config.GitHub)" -Level Success
        
        if (-not $NoInteraction) { Read-Host "Press Enter to exit" }
    }
    catch {
        Write-DebugLog "An error occurred: $_" -Level Error
        Write-DebugLog "For troubleshooting, visit: $($script:config.ErrorPage)" -Level Info
    }
}

if ($Debug -eq "on") {
    if (Test-Path $script:config.LogFile) {
        Clear-Content $script:config.LogFile
    }
    Write-DebugLog "Debug logging enabled - Log file: $($script:config.LogFile)" -Level Info
}

$host.UI.RawUI.WindowTitle = "$($script:config.GitHub)"
if (-not (Get-SteamScript)) {
    Write-DebugLog "Cannot proceed without steam.ps1 script." -Level Error
    exit
}

if (-not $SkipIntro -and -not $NoInteraction) {
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
}

Start-SteamDebloat -SelectedMode $Mode

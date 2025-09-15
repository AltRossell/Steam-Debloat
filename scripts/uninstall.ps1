$host.UI.RawUI.BackgroundColor = "Black"

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

function Write-Message {
    param (
        [string]$Message,
        [string]$Level = "Info"
    )
    
    $color = switch ($Level) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "Cyan" }
    }

    Write-Host "[$($Level.ToUpper())] " -NoNewline -ForegroundColor $color
    Write-Host $Message
}

$steamPath = "${env:ProgramFiles(x86)}\Steam"
$steamPathV2 = "${env:ProgramFiles(x86)}\Steamv2"
$desktopBatPath = "$env:USERPROFILE\Desktop\steam.bat"
$desktop2025BatPath = "$env:USERPROFILE\Desktop\Steam2025.bat"
$desktop2022BatPath = "$env:USERPROFILE\Desktop\Steam2022.bat"
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Steam"
$backupPath = "$env:TEMP\SteamBackup"
$backupPathV2 = "$env:TEMP\SteamBackupV2"
$steamInstaller = "$env:TEMP\SteamSetup.exe"

$hasSteam = Test-Path $steamPath
$hasSteamV2 = Test-Path $steamPathV2
$isExperimentalMode = $hasSteam -and $hasSteamV2

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
                                                              
           __  __     __   __     __     __   __     ______   ______     __         __        
          /\ \/\ \   /\ "-.\ \   /\ \   /\ "-.\ \   /\  ___\ /\__  _\   /\ \       /\ \       
          \ \ \_\ \  \ \ \-.  \  \ \ \  \ \ \-.  \  \ \___  \\/_/\ \/   \ \ \____  \ \ \____  
           \ \_____\  \ \_\\"\_\  \ \_\  \ \_\\"\_\  \/\_____\  \ \_\    \ \_____\  \ \_____\ 
            \/_____/   \/_/ \/_/   \/_/   \/_/ \/_/   \/_____/   \/_/     \/_____/   \/_____/ 
                                                                                              
"@ -ForegroundColor Red

Write-Message "Steam Debloat Uninstaller"
Write-Host ""

if ($isExperimentalMode) {
    Write-Message "Experimental mode detected - Both Steam versions found:" -Level Warning
    Write-Host "  - Steam 2025: $steamPath" -ForegroundColor White
    Write-Host "  - Steam 2022: $steamPathV2" -ForegroundColor White
} elseif ($hasSteam) {
    Write-Message "Standard Steam installation found: $steamPath"
} elseif ($hasSteamV2) {
    Write-Message "Steam v2 installation found: $steamPathV2"
} else {
    Write-Message "No Steam installations found!" -Level Error
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    [System.Console]::ReadKey($true) | Out-Null
    exit
}

Write-Host ""
$proceed = Show-YesNoMenu -Title "Do you want to proceed with uninstalling Steam Debloat?" -Line ([Console]::CursorTop + 1)
if (-not $proceed) {
    Write-Message "Uninstall cancelled by user."
    [System.Console]::CursorVisible = $true
    exit
}

function Test-SteamRunning {
    $steamProcess = Get-Process -Name "steam" -ErrorAction SilentlyContinue
    return $null -ne $steamProcess
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

function Backup-SteamData {
    param(
        [string]$SteamDir,
        [string]$BackupDir,
        [string]$Version
    )
    
    Write-Message "Creating backup for Steam $Version..."
    
    if (-not (Test-Path $BackupDir)) {
        New-Item -Path $BackupDir -ItemType Directory | Out-Null
        Write-Message "Backup directory created at: $BackupDir" -Level Success
    }

    $filesToBackup = @(
        @{Path = "steamapps"; Type = "Directory"},
        @{Path = "config"; Type = "Directory"}
    )
    
    foreach ($item in $filesToBackup) {
        $sourcePath = Join-Path $SteamDir $item.Path
        $destPath = Join-Path $BackupDir $item.Path

        if (Test-Path $sourcePath) {
            Write-Message "Moving $($item.Path) to backup..."
            $parentPath = Split-Path $destPath -Parent
            if (-not (Test-Path $parentPath)) {
                New-Item -Path $parentPath -ItemType Directory -Force | Out-Null
            }
            Move-Item -Path $sourcePath -Destination $destPath -Force
            Write-Message "Successfully moved $($item.Path) for Steam $Version" -Level Success
        } else {
            Write-Message "$($item.Path) not found in Steam $Version installation" -Level Warning
        }
    }
}

function Restore-SteamData {
    param(
        [string]$SteamDir,
        [string]$BackupDir,
        [string]$Version
    )
    
    if (Test-Path $BackupDir) {
        Write-Message "Restoring backup files for Steam $Version..."
        
        $filesToRestore = @(
            @{Path = "steamapps"; Type = "Directory"},
            @{Path = "config"; Type = "Directory"}
        )
        
        foreach ($item in $filesToRestore) {
            $sourcePath = Join-Path $BackupDir $item.Path
            $destPath = Join-Path $SteamDir $item.Path

            if (Test-Path $sourcePath) {
                $parentPath = Split-Path $destPath -Parent
                if (-not (Test-Path $parentPath)) {
                    New-Item -Path $parentPath -ItemType Directory -Force | Out-Null
                }
                
                Write-Message "Restoring $($item.Path) for Steam $Version..."
                Move-Item -Path $sourcePath -Destination $destPath -Force
                Write-Message "Successfully restored $($item.Path) for Steam $Version" -Level Success
            }
        }
        
        Remove-Item -Path $BackupDir -Recurse -Force
        Write-Message "Backup for Steam $Version removed successfully" -Level Success
    }
}

Write-Message "Removing desktop and start menu shortcuts..."

# Remove desktop shortcuts
if (Test-Path $desktopBatPath) {
    Remove-Item -Path $desktopBatPath -Force
    Write-Message "Removed desktop steam.bat" -Level Success
}
if (Test-Path $desktop2025BatPath) {
    Remove-Item -Path $desktop2025BatPath -Force
    Write-Message "Removed desktop Steam2025.bat" -Level Success
}
if (Test-Path $desktop2022BatPath) {
    Remove-Item -Path $desktop2022BatPath -Force
    Write-Message "Removed desktop Steam2022.bat" -Level Success
}

# Remove start menu shortcuts
if (Test-Path $startMenuPath) {
    Remove-Item -Path $startMenuPath -Recurse -Force
    Write-Message "Removed Start Menu Steam folder" -Level Success
}

if (Test-SteamRunning) {
    Write-Message "Closing Steam processes..."
    Stop-Process -Name "steam" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Write-Message "Steam processes closed" -Level Success
}

if ($isExperimentalMode) {
    Write-Message "Processing experimental dual installation..."
    
    if ($hasSteam) {
        Backup-SteamData -SteamDir $steamPath -BackupDir $backupPath -Version "2025"
        Write-Message "Removing Steam 2025 installation..."
        Remove-Item -Path $steamPath -Recurse -Force
        Write-Message "Steam 2025 installation removed" -Level Success
    }
    
    if ($hasSteamV2) {
        Backup-SteamData -SteamDir $steamPathV2 -BackupDir $backupPathV2 -Version "2022"
        Write-Message "Removing Steam 2022 installation..."
        Remove-Item -Path $steamPathV2 -Recurse -Force
        Write-Message "Steam 2022 installation removed" -Level Success
    }
    
} else {
    $targetPath = if ($hasSteam) { $steamPath } else { $steamPathV2 }
    $targetBackup = if ($hasSteam) { $backupPath } else { $backupPathV2 }
    $version = if ($hasSteam) { "Standard" } else { "V2" }
    
    Backup-SteamData -SteamDir $targetPath -BackupDir $targetBackup -Version $version
    Write-Message "Removing Steam installation..."
    Remove-Item -Path $targetPath -Recurse -Force
    Write-Message "Steam installation removed" -Level Success
}

Write-Message "Downloading Steam installer..."
try {
    Invoke-WebRequest -Uri "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe" -OutFile $steamInstaller
    Write-Message "Steam installer downloaded successfully" -Level Success
} catch {
    Write-Message "Error downloading Steam: $_" -Level Error
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    [System.Console]::ReadKey($true) | Out-Null
    exit
}

Write-Message "Installing clean Steam..."
Start-Process -FilePath $steamInstaller -ArgumentList "/S" -Wait

Write-Message "Waiting for installation to complete..."
if (-not (Wait-ForPath -Path $steamPath -TimeoutSeconds 300)) {
    Write-Message "Steam installation did not complete in the expected time" -Level Error
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    [System.Console]::ReadKey($true) | Out-Null
    exit
}

if ($isExperimentalMode) {
    Write-Message "For experimental mode, only restoring Steam 2025 data to main installation..."
    Restore-SteamData -SteamDir $steamPath -BackupDir $backupPath -Version "2025"
    
    Write-Host ""
    Write-Message "Steam 2022 backup still available at: $backupPathV2" -Level Warning
    $keepBackup = Show-YesNoMenu -Title "Do you want to keep Steam 2022 backup for manual restoration?" -Line ([Console]::CursorTop + 1)
    if (-not $keepBackup) {
        Remove-Item -Path $backupPathV2 -Recurse -Force -ErrorAction SilentlyContinue
        Write-Message "Steam 2022 backup removed" -Level Success
    } else {
        Write-Message "Steam 2022 backup preserved for manual restoration"
    }
} else {
    $targetBackup = if (Test-Path $backupPath) { $backupPath } else { $backupPathV2 }
    $version = if (Test-Path $backupPath) { "Standard" } else { "V2" }
    Restore-SteamData -SteamDir $steamPath -BackupDir $targetBackup -Version $version
}

Remove-Item -Path $steamInstaller -Force -ErrorAction SilentlyContinue

Write-Message "Starting clean Steam..."
Start-Process "$steamPath\steam.exe" -ArgumentList "-forcesteamupdate -forcepackagedownload -overridepackageurl -exitsteam"
Start-Sleep -Seconds 5
Start-Process "$steamPath\steam.exe"

Write-Host ""
Write-Message "Steam Debloat uninstallation completed successfully!" -Level Success
Write-Message "Steam has been restored to its original state." -Level Success
Write-Message "Your games and configurations have been preserved."
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Yellow
[System.Console]::ReadKey($true) | Out-Null
[System.Console]::CursorVisible = $true
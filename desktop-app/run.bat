@echo off
cd /d "%~dp0"
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" & goto :run_build
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat" call "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat" & goto :run_build
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat" call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat" & goto :run_build
exit /b 1
:run_build
if not exist "SteamDebloat.csproj" exit /b 1
if exist "bin\" rmdir /s /q "bin"
if exist "obj\" rmdir /s /q "obj"
MSBuild SteamDebloat.csproj /t:Restore /p:Configuration=Release /v:minimal
MSBuild SteamDebloat.csproj /t:Build /p:Configuration=Release /p:Platform=AnyCPU /v:minimal /m
pause
exit /b %ERRORLEVEL%

@echo off
set VSDEV="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
if not exist %VSDEV% exit /b 1
call %VSDEV%
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist %MSBUILD% exit /b 1
cd /d "%~dp0"
for %%f in (*.sln *.csproj) do set PROY=%%f
if not defined PROY exit /b 1
%MSBUILD% "%PROY%" /p:Configuration=Release /t:Build /p:TargetFrameworkVersion=v4.8
pause

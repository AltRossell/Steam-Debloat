@echo off

set url='https://raw.githubusercontent.com/mtytyx/Steam-Debloat/main/docs/script/app_lite_2022_dec_dev.ps1'
set tls=[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12;

%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\powershell.exe ^
-Command %tls% " & { $(try { iwr -useb %url% } catch { iwr -useb %url% })} | iex

exit /b

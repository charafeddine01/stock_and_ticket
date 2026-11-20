@echo off
:: Batch script to map solvis.local to 192.168.77.3 on Windows
:: Requires Run as Administrator
echo =======================================================
echo   SOLVIS - Map solvis.local to Ubuntu Server (192.168.77.3)
echo =======================================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Administrative privileges required!
    echo Right-click this file and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

set HOSTS=%WINDIR%\System32\drivers\etc\hosts

:: Remove any existing solvis.local entries to avoid conflicts
powershell -NoProfile -Command "$path = '%HOSTS%'; $content = (Get-Content $path) | Where-Object { $_ -notmatch 'solvis\.local' }; Set-Content -Path $path -Value $content -Force"

:: Append fresh entry
echo 192.168.77.3 solvis.local>> "%HOSTS%"

:: Flush DNS resolver cache
ipconfig /flushdns >nul

echo [SUCCESS] solvis.local is now mapped to 192.168.77.3!
echo [SUCCESS] DNS cache flushed.
echo.
echo You can now access:
echo   http://solvis.local
echo.
pause

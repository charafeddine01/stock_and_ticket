@echo off
:: ==============================================================================
:: SOLVIS Hospital System - 1-Click Root Certificate Installer (certutil)
:: Right-click and select "Run as administrator"
:: ==============================================================================
title SOLVIS Root Certificate Installer
cd /d "%~dp0"

echo ==========================================================
echo    SOLVIS - Root CA Certificate Installer (Windows)
echo ==========================================================
echo.

:: Check for Administrator permissions
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Administrator rights required.
    echo Please right-click this batch file and select "Run as administrator".
    echo.
    pause
    exit /b 1
)

set "CERT_FILE=solvis-root-ca.crt"
if not exist "%CERT_FILE%" (
    set "CERT_FILE=solvis-root-ca.cer"
)

if not exist "%CERT_FILE%" (
    echo [ERROR] Cannot find solvis-root-ca.crt or solvis-root-ca.cer in this directory!
    echo Please make sure the certificate file is in the same folder.
    echo.
    pause
    exit /b 1
)

echo Installing %CERT_FILE% into Trusted Root Certification Authorities...
certutil -addstore -f "Root" "%CERT_FILE%"

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] Certificate installed successfully!
    echo Workstation will now trust https://solvis.local with a secure green padlock.
) else (
    echo.
    echo [ERROR] Failed to install certificate via certutil.
)

echo.
echo ==========================================================
pause

@echo off
title SOLVIS Auto-Deploy to Ubuntu Server
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "deploy.ps1" %*
pause

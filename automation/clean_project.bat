@echo off
call pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0clean_api.ps1" %*
pause
